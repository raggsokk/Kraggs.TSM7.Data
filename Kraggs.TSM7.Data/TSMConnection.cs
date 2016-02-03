#region License
/*
    TSM 7.1 Utility library.
    Copyright (C) 2016 Jarle Hansen
    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.
    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.
    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using Kraggs.TSM7.Utils;
using Kraggs.TSM7.Data.Schema;

namespace Kraggs.TSM7.Data
{
    public class TSMConnection
    {
        /// <summary>
        /// Reference to internal DsmAdmc Client Wrapper.
        /// </summary>
        public clsDsmAdmc DsmAdmc { get; protected set; }

        /// <summary>
        /// Returns the name of the TSM Server instance.        
        /// </summary>
        public string ServerName { get; protected set; }

        /// <summary>
        /// Returns the TSM Server Platform.
        /// </summary>
        public string ServerPlatform { get; protected set; }
        /// <summary>
        /// Returns the TSM Server Version.
        /// </summary>
        public Version ServerVersion { get; protected set; }


        internal SortedDictionary<string, clsSchemaTable> pSchema { get; set; }

        internal SortedDictionary<string, clsSelectAllCacheItem> pCacheSelect { get; set; }

        [Obsolete]
        internal SortedDictionary<string, List<clsColumnInfo>> pCache { get; set; }

        #region Static Constructors

        protected TSMConnection()
        {
            // dummy.
            
        }

        public static TSMConnection CreateConnection(clsDsmAdmc dsmadmc)
        {
            var conn = new TSMConnection() { DsmAdmc = dsmadmc };

            //dsmadmc.DebugSaveLastCommand = true;
            //dsmadmc.ThrowOnErrors = true;

            var q = "SELECT SERVER_NAME,PLATFORM,VERSION,RELEASE,LEVEL,SUBLEVEL FROM STATUS";

            var list = new List<string>();

            var retCode = dsmadmc.RunTSMCommandToList(q, list);

            if (retCode == AdmcExitCode.Ok)
            {
                var line = list.First();

                var data = CsvParser.Parse(line);

                conn.ServerName = data[0];
                conn.ServerPlatform = data[1];
                conn.ServerVersion = new Version(string.Format("{0}.{1}.{2}.{3}", data[2], data[3], data[4], data[5]));
            }
            else
                throw new ApplicationException("Failed to retrive TSM Server Info.");


            return conn;
        }

        #endregion

        #region Internal Functions

        // filters the culumns from typeinfo with schema and version info.
        // returns the combined result.
        [Obsolete]
        internal List<clsColumnInfo> GetFilteredColumnInfo(clsTypeInfo typeInfo)
        {
            // returns the
            if (pCache == null)
                pCache = new SortedDictionary<string, List<clsColumnInfo>>();

            List<clsColumnInfo> cols = null;

            if(!pCache.TryGetValue(typeInfo.TypeName, out cols))
            {
                var tabschema = GetTableSchema(typeInfo.TableName);
                cols = new List<clsColumnInfo>();

                foreach(var sc in tabschema.Columns)
                {
                    clsColumnInfo f = null;

                    if (typeInfo.ColumnHash.TryGetValue(sc.ColName, out f))
                    {
                        if (f.RequiredVersion != null)
                        {
                            if (f.RequiredVersion <= this.ServerVersion)
                                cols.Add(f);
                            else
                                cols.Add(null);
                        }
                        else
                            cols.Add(f);
                    }
                    else
                        cols.Add(null); // null means skip value.
                }

                ////var verColumns = CommonFunction.GetColumnsByVersion(typeInfo, this.ServerVersion);

                //var arr = new clsColumnInfo[tabschema.ColCount];

                //foreach(var c in tabschema.Columns)
                //{
                //    var f = typeInfo.ColumnHash[c.ColName];

                //    if(f != null)
                //    {
                //        if(f.RequiredVersion != null)
                //            if(f.RequiredVersion <= this.ServerVersion)
                //                arr[]
                //    }
                //}

                //cols = arr.ToList();
                //cols = CommonFunction.GetColumnsByVersion(typeInfo, this.ServerVersion).ToList();
                pCache.Add(typeInfo.TypeName, cols);                
            }

            return cols;
        }

        internal clsSchemaTable GetTableSchema(string tablename)
        {
            var tupper = tablename.ToUpperInvariant();

            if (pSchema == null)
                pSchema = new SortedDictionary<string, clsSchemaTable>();

            clsSchemaTable table = null;

            if(pSchema.TryGetValue(tupper, out table))
            {
                return table;
            }
            else
            {
                // create and add.
                var q = string.Format(
                    "SELECT COLNAME,COLNO,TYPENAME,LENGTH,SCALE,NULLS FROM SYSCAT.COLUMNS WHERE TABNAME='{0}' ORDER BY COLNO ASC", tupper);

                var list = new List<string>();

                var retCode = DsmAdmc.RunTSMCommandToList(q, list);

                if (retCode == AdmcExitCode.Ok)
                {
                    var parsedlist = new List<List<string>>();

                    table = new clsSchemaTable() { TabName = tupper, Columns = new List<clsSchemaColumn>() };
                    table.ColCount = CsvParser.Parse(list, parsedlist);
                                        
                    foreach(var l in parsedlist)
                    {
                        var a = l.ToArray();
                        var c = new clsSchemaColumn();

                        c.ColName = a[0];
                        c.ColNo = int.Parse(a[1]);
                        c.TypeName = a[2];
                        c.Length = int.Parse(a[3]);
                        c.Scale = int.Parse(a[4]);
                        c.Nulls = a[5] == "TRUE";

                        table.Columns.Add(c);
                    }

                    pSchema.Add(tupper, table); // uppercase lookup
                    //pSchema.Add(tablename, table); // what used by user to.
                    return table;
                }
                else
                    throw new ApplicationException(string.Format(
                        "Failed to retrive Table Schema for table '{0}'", tupper));
            }
        }

        /// <summary>
        /// Based on mytype, creates a select all, combining tsm schema and reflection data.
        /// The result is returned in out SqlQuery and out Cols
        /// </summary>
        /// <param name="myType"></param>
        /// <param name="SqlQuery">A select statement compatible with TSM Server version and with Type reflected names.</param>
        /// <param name="cols">List of columns corresponding to SqlQuery generated.</param>
        /// <param name="ExplicitQuery">false returns a "Select</param>
        /// <returns></returns>
        internal bool CreateSelectAll(clsTypeInfo myType, out string SqlQuery, out List<clsColumnInfo> cols, bool ExplicitQuery = true)
        {
            //TODO: Change ExplicitQuery to MaxLength instead?

            if (pCacheSelect == null)
                pCacheSelect = new SortedDictionary<string, clsSelectAllCacheItem>();

            clsSelectAllCacheItem cachedSelect = null;            

            if (!pCacheSelect.TryGetValue(myType.TypeName, out cachedSelect))
            {
                // 1. first get tsm schema.
                var tabschema = GetTableSchema(myType.TableName);
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                cols = new List<clsColumnInfo>();

                if (ExplicitQuery)
                {
                    foreach (var sc in tabschema.Columns)
                    {
                        clsColumnInfo f = null;

                        if (myType.ColumnHash.TryGetValue(sc.ColName, out f))
                        {
                            if (f.RequiredVersion == null || f.RequiredVersion <= this.ServerVersion)
                            {
                                sb.AppendFormat("{0},", f.ColumnName);
                                cols.Add(f);
                            }
                        }
                    }
                    // remove last ,
                    sb.Length = sb.Length - 1;
                }
                else
                {
                    throw new NotImplementedException("Havent implemented non explicit code path yet.");
                }

                sb.AppendFormat(" FROM {0}", myType.TableName);

                cachedSelect = new clsSelectAllCacheItem()
                {
                    MyType = myType,
                    SelectAll = sb.ToString(),
                    Columns = cols
                };

                pCacheSelect.Add(myType.TypeName, cachedSelect);
            }

            SqlQuery = cachedSelect.SelectAll;
            cols = cachedSelect.Columns;

            return true;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Selects all rows from table, generating a SelectAll query combining data from TSM Server Schema and from Reflection breakdown of user provided type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> SelectAll<T>(bool UseTmpFile = false) where T : new()
        {
            if (UseTmpFile)
                throw new NotImplementedException();

            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            List<clsColumnInfo> cols = null;
            string SqlSelectAll = string.Empty;

            if (CreateSelectAll(myType, out SqlSelectAll, out cols, true))
            {
                var tsmlist = new List<string>();

                var retCode = DsmAdmc.RunTSMCommandToList(SqlSelectAll, tsmlist);                

                if (retCode == AdmcExitCode.NotFound)
                    return new List<T>();
                else if (retCode == AdmcExitCode.Ok)
                {
                    List<List<string>> parsed = new List<List<string>>();

                    int parseCount = CsvParser.Parse(tsmlist, parsed);

                    //return CsvConvert.ConvertUnsafe<T>(parsed);
                    return CsvConvert.Convert<T>(parsed, myType, cols);
                }
                else
                    throw new ApplicationException(string.Format(
                        "Failed to run query '{0}'", SqlSelectAll));
            }
            else
                throw new ApplicationException(string.Format(
                    "Failed to generate Select query for '{0}'", myType.TypeName));

            throw new NotImplementedException();
        }

        public List<T> Where<T>(string UnsafeSqlWhere, bool UseTmpFile = false) where T : new()
        {
            if (UseTmpFile)
                throw new NotImplementedException();

            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            List<clsColumnInfo> cols = null;
            string SqlSelectAll = string.Empty;

            if (CreateSelectAll(myType, out SqlSelectAll, out cols, true))
            {
                var tsmlist = new List<string>();

                //Only when unsafe sql where is actually something does we call this function.
                if(!string.IsNullOrWhiteSpace(UnsafeSqlWhere))
                {
                    //TODO: check for empty where statement and ignore them aka UnsafeSqlWhere="WHERE "?

                    if (!UnsafeSqlWhere.TrimStart().StartsWith("WHERE", StringComparison.InvariantCultureIgnoreCase))
                        SqlSelectAll += " WHERE ";

                    SqlSelectAll += " ";
                    SqlSelectAll += UnsafeSqlWhere;                        
                }

                var retCode = DsmAdmc.RunTSMCommandToList(SqlSelectAll, tsmlist);

                if (retCode == AdmcExitCode.NotFound)
                    return new List<T>();
                else if (retCode == AdmcExitCode.Ok)
                {
                    List<List<string>> parsed = new List<List<string>>();

                    int parseCount = CsvParser.Parse(tsmlist, parsed);

                    //return CsvConvert.ConvertUnsafe<T>(parsed);
                    return CsvConvert.Convert<T>(parsed, myType, cols);
                }
                else
                    throw new ApplicationException(string.Format(
                        "Failed to run query '{0}'", SqlSelectAll));
            }
            else
                throw new ApplicationException(string.Format(
                    "Failed to generate Select query for '{0}'", myType.TypeName));

            throw new NotImplementedException();
        }


        //public List<T> SelectAll<T>() where T : new()
        //{
        //    var type = typeof(T);
        //    var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

        //    //var tabschema = GetTableSchema(myType.TableName);

        //    var cols = GetFilteredColumnInfo(myType);

        //    var tsmlist = new List<string>();

        //    var q = string.Format("SELECT * FROM {0}", myType.TableName);

        //    var retCode = DsmAdmc.RunTSMCommandToList(q, tsmlist);

        //    if (retCode == AdmcExitCode.NotFound)
        //        return new List<T>(); // return null instead?
        //    else if(retCode == AdmcExitCode.Ok)
        //    {
        //        List<List<string>> parsed = new List<List<string>>();

        //        int parseCount = CsvParser.Parse(tsmlist, parsed);

        //        //return CsvConvert.ConvertUnsafe<T>(parsed);
        //        return CsvConvert.Convert<T>(parsed, myType, cols);
        //    }


        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// Runs any tsm sql query and uses column AS to match data to property.
        /// DOES NOT USE TSM SCHEMA.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="UnsafeSQLWithASColumns"></param>
        /// <example>
        /// <![CDATA[        
        /// class SomeClass
        /// {
        ///     public int NodeCount { get; set; }
        ///     public bool Dummy1 {get;set;}
        ///     public double TotalMB { get; set; }
        ///     public bool Dummy2 {get;set;}
        /// }
        /// 
        /// var q = "SELECT COUNT(*) as \"NodeCount\", sum(TotalMB) as \"TotalMB\" FROM AUDITOCC"
        /// 
        /// var result = SelectAS<SomeClass>(q);
        /// 
        /// ]]>
        /// </example>
        /// <returns></returns>
        public List<T> SelectAS<T>(string UnsafeSQLWithASColumns) where T : new()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the TSM Admin Username used when connecting to the tsm server.
        /// </summary>
        [DebuggerNonUserCode()]
        public string Username
        {
            get
            {
                return this.DsmAdmc.Username;
            }
        }

        /// <summary>
        /// The version of the Client Software package.
        /// </summary>
        [DebuggerNonUserCode()]
        public Version ClientVersion
        {
            get
            {
                return this.DsmAdmc.DsmAdmcVersion;
            }
        }

        #endregion
    }
}
