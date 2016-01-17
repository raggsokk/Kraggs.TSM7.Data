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
    /// <summary>
    /// Contains common functionality used by DsmAdmcExtension.
    /// Moved here for better reuse.
    /// </summary>
    internal static class CommonFunction
    {
        /// <summary>
        /// Executes a sql command, handling use of tmp file or not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dsmadmc"></param>
        /// <param name="myType"></param>
        /// <param name="UnsafeSQL"></param>
        /// <param name="UseTmpFile"></param>
        /// <returns></returns>
        internal static List<T> Execute<T>(clsDsmAdmc dsmadmc, string UnsafeSQL, bool UseTmpFile = false)
        {
            if (!UseTmpFile)
            {
                var tsmlist = new List<string>();

                var retCode = dsmadmc.RunTSMCommandToList(UnsafeSQL, tsmlist);

                if (retCode == AdmcExitCode.NotFound)
                    return new List<T>(); // return null instead?

                List<List<string>> parsed = new List<List<string>>();

                int parseCount = CsvParser.Parse(tsmlist, parsed);

                //return CsvConvert.ConvertUnsafe<T>(parsed);
                return CsvConvert.Convert<T>(parsed);
            }
            else
            {
                using (var tmp = new Misc.AutoDeleteTempFile())
                {
                    var retCode = dsmadmc.RunTSMCommandToFile(
                        UnsafeSQL, tmp.TempFile);

                    if (retCode == AdmcExitCode.NotFound)
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
                }
            }
        }

        /// <summary>
        /// Retrive a version filtered list of columns to use.
        /// </summary>
        /// <param name="myType"></param>
        /// <param name="TSMVersion"></param>
        /// <returns></returns>
        internal static IEnumerable<clsColumnInfo> GetColumnsByVersion(clsTypeInfo myType, Version TSMVersion = null)
        {
            var list = new List<clsColumnInfo>();

            if (TSMVersion != null && myType.RequiredVersion > TSMVersion)
                return list;

            foreach (var c in myType.Columns)
            {
                if (c.RequiredVersion != null && TSMVersion != null)
                {
                    if (c.RequiredVersion <= TSMVersion)
                        list.Add(c);
                }
                else
                    list.Add(c); // if no version info present, assume its okay.
            }

            return list;
        }

        /// <summary>
        /// Internal only for reuse among other functions which might need this functionality.
        /// </summary>
        /// <param name="dsmadmc"></param>
        /// <param name="myType"></param>
        /// <param name="TSMVersion"></param>
        /// <returns></returns>
        internal static IEnumerable<string> GetColumnNamesByVersion(clsTypeInfo myType, Version TSMVersion = null)
        {
            if (TSMVersion != null && myType.RequiredVersion > TSMVersion)
            {
                // not high enoug version.
                throw new ArgumentException(string.Format("Table '{0}' is not present on TSM Server version: '{1}'", myType.TableName, TSMVersion));
            }

            var list = new List<string>();

            var colums = GetColumnsByVersion(myType, TSMVersion);

            foreach (var c in colums)
                list.Add(c.ColumnName);

            return list;
        }

        //internal static StringBuilder CreateSelectAll(clsTypeInfo myType, IEnumerable<clsColumnInfo> columns, Version TSMVersion = null)

        /// <summary>
        /// Creates a select all statement with version filtered attributes.
        /// </summary>
        /// <param name="myType"></param>
        /// <param name="TSMVersion"></param>
        /// <returns></returns>
        internal static StringBuilder CreateSelectAll(clsTypeInfo myType, Version TSMVersion = null)
        {
            var sb = new StringBuilder();

            sb.Append("SELECT ");

            //var columns = GetColumnsByVersion(myType, TSMVersion);
            var columns = GetColumnNamesByVersion(myType, TSMVersion);
            
            sb.Append(string.Join(",", columns));

            sb.AppendFormat(" FROM {0} ", myType.TableName);

            return sb;
        }
    }
}
