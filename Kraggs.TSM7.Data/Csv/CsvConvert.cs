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

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Kraggs.TSM7.Data
{
    /// <summary>
    /// Class for converting Csv Values into objects.
    /// </summary>
    internal static class CsvConvert
    {
        // combined safe and unsafe convert method.
        /// <summary>
        /// Converts a list of list of values into a list of objects.
        /// </summary>
        /// <typeparam name="T">Type to parse values into.</typeparam>
        /// <param name="CsvLines">list of list of values</param>
        /// <param name="myType">MyType. If null picks from T.</param>
        /// <param name="Columns">A list of columns. If null gets from myType.</param>
        /// <param name="TSMVersion">The TSMVersion to treat this as.</param>
        /// <returns></returns>
        public static List<T> Convert<T>(List<List<string>> CsvLines, clsTypeInfo myType = null, List<clsColumnInfo> Columns = null, Version TSMVersion = null)
        {
            var result = new List<T>();

            if (myType == null)
            {
                var type = typeof(T);
                myType = Reflection.Instance.GetTypeInfo(type.FullName, type);
            }

            clsColumnInfo[] arrColumns;

            if (Columns == null)
                arrColumns = myType.Columns.ToArray();
            else
                arrColumns = Columns.ToArray();

            foreach (var listValues in CsvLines)
            {
                var o = myType.CreateObject();

                int i = 0;
                foreach (var value in listValues)
                {
                    if (i == arrColumns.Length)
                        break;

                    var column = arrColumns[i++];                    

                    var flagNull = value.Length == 0;
                    if (flagNull) //UBER fancy null type handler.
                    {
                        //i++;
                        continue;
                    }

                    object oset = null;

                    switch(column.DataType)
                    {
                        case TSMDataType.String:
                            oset = value; break;
                        case TSMDataType.Short:
                            oset = short.Parse(value); break;
                        case TSMDataType.Int:
                            oset = int.Parse(value); break;
                        case TSMDataType.Long:
                            oset = long.Parse(value); break;
                        case TSMDataType.Double:
                            oset = ParseDB2Double(value); break;
                        case TSMDataType.Float:
                            oset = ParseDB2Float(value); break;
                        case TSMDataType.Bool:
                            oset = ParseTSMBool(value); break;
                        case TSMDataType.DateTime:
                            oset = ParseDB2DateTime(value); break;
                        case TSMDataType.Guid:
                            oset = ParseTSMGuid(value); break;
                        default:
                            throw new NotImplementedException();

                    }
                    column.SetValue(o, oset);
                }
                result.Add((T)o);
            }

            return result;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ParseDB2Double(string value)
        {
            var E = value.IndexOf('E');
            if (E != -1)
            {
                var d = double.Parse(value.Substring(0, E), CultureInfo.InvariantCulture);
                var multiplier = int.Parse(value.Substring(E + 2));

                if (value[E + 1] == '+')
                    return d * Math.Pow(10, multiplier);
                else if (value[E + 1] == '-')
                    return d / Math.Pow(10, multiplier);
                else
                    throw new NotImplementedException();
            }
            else
                return double.Parse(value, CultureInfo.InvariantCulture);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ParseDB2Float(string value)
        {
            var E = value.IndexOf('E');
            if(E != -1)
            {
                var d = float.Parse(value.Substring(0, E), CultureInfo.InvariantCulture);
                var multiplier = int.Parse(value.Substring(E + 2));

                if (value[E + 1] == '+')
                    return d * (float)Math.Pow(10, multiplier);
                else if(value[E + 1] == '-')
                    return d / (float)Math.Pow(10, multiplier);
                else
                    throw new NotImplementedException();
            }
            else
                return float.Parse(value, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime ParseDB2DateTime(string value)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Guid ParseTSMGuid(string value)
        {
            return Guid.Parse(value.Replace(".", ""));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ParseTSMBool(string value)
        {
            var toUpper = value.ToUpperInvariant();
            if (toUpper == "YES" || toUpper == "TRUE")
                return true;
            else
                return false;
        }
    }
}
