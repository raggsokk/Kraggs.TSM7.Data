#region License
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
        public static List<T> Select<T>(this clsDsmAdmc dsmadmc, Version TSMVersion = null, bool UseTmpFile = false)
        {
            return Select<T>(dsmadmc, null, null, UseTmpFile);
            //return CommonFunction.Execute<T>(dsmadmc, )
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
        public static List<T> Select<T>(this clsDsmAdmc dsmadmc, string UnsafeSQL, Version TSMVersion = null, bool UseTmpFile = false)
        {
            //clsTypeInfo myType = null;

            if(string.IsNullOrWhiteSpace(UnsafeSQL))
            {
                // type sql query
                var t = typeof(T);
                var myType = Reflection.Instance.GetTypeInfo(t.FullName, t);

                if (string.IsNullOrWhiteSpace(myType.TSMSqlQuery))
                {
                    // default to use all columns known.
                    UnsafeSQL = CommonFunction.CreateSelectAll(myType, TSMVersion).ToString();

                    //throw new ArgumentNullException("Generic Type requires TSMQueryAttribute to spesify tsm query to run.");
                }
                else
                    UnsafeSQL = myType.TSMSqlQuery;
            }
            
            // uber simple validation.
            if (!UnsafeSQL.StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("SQL Query MUST start with SELECT", "UnsafeSQL");

            return CommonFunction.Execute<T>(dsmadmc, UnsafeSQL, UseTmpFile);

 
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

            // create select all

            var sb = CommonFunction.CreateSelectAll(myType, TSMVersion);

            sb.Append(" ");
            sb.Append(UnsafeWhereSQL);

            return CommonFunction.Execute<T>(dsmadmc, sb.ToString(), UseTmpFile);

        }

        #region TSM Info Functions.

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

            return CommonFunction.GetColumnNamesByVersion(myType, TSMVersion);          
        }

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

        #endregion

        #region TSM Linq

        /// <summary>
        /// Runs a "SELECT ... FROM ... WHERE [LINQ]" query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="where"></param>
        /// <param name="UseTmpFile"></param>
        /// <returns></returns>
        public static List<T> Where<T>(this clsDsmAdmc dsmadmc, Expression<Func<T, bool>> where, Version TSMVersion = null,  bool UseTmpFile = false)
        {
            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            var sb = CommonFunction.CreateSelectAll(myType, TSMVersion);

            sb.Append(" WHERE ");


            if (!ExpressionHelper.BuildWhereString(sb, where))
                throw new ArgumentException("Failed to parse linq expression", "where");

            return CommonFunction.Execute<T>(dsmadmc, sb.ToString(), UseTmpFile);

        }


        #endregion
    }
}
