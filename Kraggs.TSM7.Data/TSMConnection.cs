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

                    pSchema.Add(tupper, table);
                    return table;
                }
                else
                    throw new ApplicationException(string.Format(
                        "Failed to retrive Table Schema for table '{0}'", tupper));
            }
        }

        #endregion

        #region Functions

        public List<T> SelectAll<T>() where T : new()
        {
            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            //var tabschema = GetTableSchema(myType.TableName);

            var cols = GetFilteredColumnInfo(myType);

            var tsmlist = new List<string>();

            var q = string.Format("SELECT * FROM {0}", myType.TableName);

            var retCode = DsmAdmc.RunTSMCommandToList(q, tsmlist);

            if (retCode == AdmcExitCode.NotFound)
                return new List<T>(); // return null instead?
            else if(retCode == AdmcExitCode.Ok)
            {
                List<List<string>> parsed = new List<List<string>>();

                int parseCount = CsvParser.Parse(tsmlist, parsed);

                //return CsvConvert.ConvertUnsafe<T>(parsed);
                return CsvConvert.Convert<T>(parsed, myType, cols);
            }


            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs any tsm sql query and uses column AS to match data to property.
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
