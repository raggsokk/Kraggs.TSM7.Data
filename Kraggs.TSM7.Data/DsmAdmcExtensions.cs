﻿#region License
/*
    TSM 7.1 Utility library.
    Copyright (C) 2015 Jarle Hansen
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

using System.IO;
using System.Diagnostics;
using System.Linq.Expressions;

using Kraggs.TSM7;
using Kraggs.TSM7.Utils;

namespace Kraggs.TSM7.Data
{
    public static class DsmAdmcExtensions
    {
        /// <summary>
        /// Runs a type specific query. Looks up the TSMQueryAttribte on a query and runs it.
        /// For now no parameters are supported.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="UseTmpFile"></param>
        /// <returns></returns>
        [DebuggerNonUserCode()]
        public static List<T> Select<T>(this clsDsmAdmc dsmadmc, bool UseTmpFile = false)
        {
            return Select<T>(dsmadmc, null, UseTmpFile);
        }

        /// <summary>
        /// Runs an user specified sql query and tries to parse it into Type T.
        /// This is Unsafe since we have to information of what you are quering.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="UnsafeSQL"></param>
        /// <param name="UseTmpFile">If result is expected to be large, use tmp file instead of keeping all in memory.</param>
        /// <returns></returns>
        public static List<T> Select<T>(this clsDsmAdmc dsmadmc, string UnsafeSQL, bool UseTmpFile = false)
        {
            if(string.IsNullOrWhiteSpace(UnsafeSQL))
            {
                // type sql query
                var t = typeof(T);
                var myType = Reflection.Instance.GetTypeInfo(t.FullName, t);

                if (string.IsNullOrWhiteSpace(myType.TSMSqlQuery))
                    throw new ArgumentNullException("Generic Type requires TSMQueryAttribute to spesify tsm query to run.");
                else
                    UnsafeSQL = myType.TSMSqlQuery;
            }
                      
            // uber simple validation.
            if (!UnsafeSQL.StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("SQL Query MUST start with SELECT", "UnsafeSQL");

            if (!UseTmpFile)
            {

                var tsmlist = new List<string>();

                var retCode = dsmadmc.RunTSMCommandToList(UnsafeSQL, tsmlist);

                if(retCode == AdmcExitCode.NotFound)
                    return new List<T>(); // return null instead?

                List<List<string>> parsed = new List<List<string>>();

                int parseCount = CsvParser.Parse(tsmlist, parsed);

                //return CsvConvert.ConvertUnsafe<T>(parsed);
                return CsvConvert.Convert<T>(parsed);
            }
            else
            {
                using(var tmp = new Misc.AutoDeleteTempFile())
                {
                    var retCode = dsmadmc.RunTSMCommandToFile(
                        UnsafeSQL, tmp.TempFile);

                    if(retCode == AdmcExitCode.NotFound)
                        return new List<T>();

                    //TODO: block parse result aka read 100 lines, convert and add to result, repeat.
                    tmp.Open();
                    var reader = new StreamReader(tmp.Stream);

                    List<List<string>> parsed = new List<List<string>>();
                    var result = new List<T>();

                    var sb = new StringBuilder();
                    var count = 0;

                    do
                    {
                        string s = string.Empty;
                        while ((s = reader.ReadLine()) != null)
                        {
                            parsed.Add(CsvParser.Parse(s, sb));
                            count++;
                            if (count == 1024)
                                break;
                        }

                        // todo: ask reader to prefil next 4096 lines of bytes while converting?
                        result.AddRange(CsvConvert.Convert<T>(parsed));
                        parsed.Clear();
                        count = 0;

                    } while (sb != null);

                    return result;

                    //var lines = File.ReadAllLines(tmp.TempFile);

                    //List<List<string>> parsed = new List<List<string>>();

                    //int parseCount = CsvParser.Parse(lines, parsed);
                    
                    //return CsvConvert.Convert<T>(parsed);
                }
            }
        }

        /// <summary>
        /// Runs a "SELECT ... FROM ... WHERE [LINQ]" query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="where"></param>
        /// <param name="UseTmpFile"></param>
        /// <returns></returns>
        public static List<T> Where<T>(this clsDsmAdmc dsmadmc, Expression<Func<T, bool>> where, bool UseTmpFile = false)
        {
            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            var sb = new StringBuilder();

            sb.Append("SELECT ");
            bool flagDummy = false;

            foreach (var c in myType.Columns)
            {
                if (flagDummy)
                {
                    sb.Append(", ");
                }
                sb.Append(c.ColumnName);
                flagDummy = true;
            }

            sb.Append(" FROM ");
            sb.Append(myType.TableName);
            sb.Append(" WHERE ");

            if (!ExpressionHelper.BuildWhereString(sb, where))
                throw new ArgumentException("Failed to parse linq expression", "where");


            return Select<T>(dsmadmc, sb.ToString(), UseTmpFile);
        }

        /// <summary>
        /// Runs a "SELECT [computedcolumns] FROM [computedtable] WHERE [UnsafeSQL]" query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="UnsafeWhereSQL"></param>
        /// <param name="UseTmpFile"></param>
        /// <returns></returns>
        public static List<T> Where<T>(this clsDsmAdmc dsmadmc, string UnsafeWhereSQL, Version TSMVersion = null, bool UseTmpFile = false)
        {
            if(string.IsNullOrWhiteSpace(UnsafeWhereSQL))
            {
                throw new ArgumentNullException("UnsafeWhereSQL");
            }

            if(!UnsafeWhereSQL.StartsWith("WHERE ", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("UnsafeWhereSQL must start with WHERE");
            }

            // type sql query
            var t = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(t.FullName, t);
            
            // retrive version filtered column names,
            var columns = dsmadmc.GetTSMColumns(myType, TSMVersion);

            var sb = new StringBuilder();
            sb.Append("SELECT ");
            //bool flagDummy = false;
            sb.Append(string.Join(", ", columns));
            sb.AppendFormat(" FROM {0} ", myType.TableName);
            sb.Append(UnsafeWhereSQL);

            return dsmadmc.Select<T>(sb.ToString(), UseTmpFile);
            //foreach (var c in columns)
            //{
            //}
        }

        /// <summary>
        /// Internal only for reuse among other functions which might need this functionality.
        /// </summary>
        /// <param name="dsmadmc"></param>
        /// <param name="myType"></param>
        /// <param name="TSMVersion"></param>
        /// <returns></returns>
        internal static IEnumerable<string> GetTSMColumns(this clsDsmAdmc dsmadmc, clsTypeInfo myType, Version TSMVersion = null)
        {
            if (TSMVersion != null && myType.RequiredVersion > TSMVersion)
            {
                // not high enoug version.
                throw new ArgumentException(string.Format("Table '{0}' is not present on TSM Server version: '{1}'", myType.TableName, TSMVersion));
            }

            var list = new List<string>();

            foreach(var c in myType.Columns)
            {
                if (c.RequiredVersion != null && TSMVersion != null)
                {
                    if (c.RequiredVersion <= TSMVersion)
                        list.Add(c.ColumnName);
                }
                else
                    list.Add(c.ColumnName); // if no version info present, assume okay.
            }

            return list;
        }

        /// <summary>
        /// Retrives in the expected order a list of the column names which can be used with type T.
        /// Optionally filters the names based on provided tsm server version.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="TSMVersion"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetTSMColumns<T>(this clsDsmAdmc dsmadmc, Version TSMVersion = null)
        {
            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            return dsmadmc.GetTSMColumns(myType, TSMVersion);

        }

        //public static List<T> Where<T>(this clsDsmAdmc dsmadmc, Expression<Func<T, bool>> filter)
        //{
        //    //TODO: Make this call Select<t>(dsmadmc,sql, usetmpfile) after generating sql query.

        //    /*
        //     * 1. generate sql.
        //     * 2. call tsm
        //     * 3. parse csv to list.
        //     */
        //    var type = typeof(T);
        //    var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

        //    var sb = new StringBuilder();

        //    sb.Append("SELECT ");
        //    bool flagDummy = false;

        //    foreach (var c in myType.Columns)
        //    {
        //        if (flagDummy)
        //        {
        //            sb.Append(", ");
        //        }
        //        sb.Append(c.ColumnName);
        //        flagDummy = true;
        //    }

        //    sb.Append(" FROM ");
        //    sb.Append(myType.TableName);
        //    sb.Append(" WHERE ");

        //    if (!ExpressionHelper.BuildWhereString(sb, filter))
        //        return null;

        //    var list = new List<string>();

        //    var retCode = dsmadmc.RunTSMCommandToList(sb.ToString(), list);

        //    List<List<string>> parsed = new List<List<string>>();

        //    int parseCount = CsvParser.Parse(list, parsed);

        //    //return CsvConvert.Convert<T>(parsed, myType, myType.Columns);
        //    return CsvConvert.Convert<T>(parsed, myType, myType.Columns);
        //}



        /// <summary>
        /// Gets the TSM Server Instance Version.
        /// TODO: Should this be here at all?
        /// </summary>
        /// <param name="dsmadmc"></param>
        /// <returns></returns>
        public static Version GetTSMVersion(this clsDsmAdmc dsmadmc)
        {
            var sql = "SELECT VERSION,RELEASE,LEVEL,SUBLEVEL FROM STATUS";

            var csvlist = new List<string>();

            var retCode = dsmadmc.RunTSMCommandToList(sql, csvlist);

            if (retCode == AdmcExitCode.Ok)
            {
                Version v;

                if (Version.TryParse(csvlist.First().Replace(",", "."), out v))
                    return v;
            }

            throw new ApplicationException("Failed to obtain TSM Instance version");
        }

    }
}
