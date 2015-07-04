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

namespace Kraggs.TSM7.Data
{
    //TODO: Merge Convert & ConvertUnsafe since they are almost equal.
    internal static class CsvConvert
    {
        public static List<T> ConvertUnsafe<T>(List<List<string>> CsvLines)
        {
            var result = new List<T>();

            var type = typeof(T);
            var myType = Reflection.Instance.GetTypeInfo(type.FullName, type);

            var arrColumns = myType.Columns.ToArray();

            foreach(var listValues in CsvLines)
            {
                var o = myType.CreateObject();

                int i = 0;
                foreach(var value in listValues)
                {
                    if (i == arrColumns.Length)
                        break;

                    var column = arrColumns[i];
                    object oset = null;

                    var flagNull = value.Length == 0;
                    if (flagNull) //UBER fancy null type handler.
                    {
                        i++;
                        continue;
                    }


                    //TODO: Handle null values from TSM.
                    //TODO: Handle nullable types in T.
                    
                    switch (column.DataType)
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
                            //oset = double.Parse(value, CultureInfo.InvariantCulture); break;
                            //oset = double.Parse(value); break;
                            oset = ParseDB2Double(value); break;
                        case TSMDataType.Float:
                            //TODO: Check for "." in value to handle all cases?
                            oset = float.Parse(value, CultureInfo.InvariantCulture); break;                            
                            //oset = float.Parse(value); break;
                        case TSMDataType.Bool:
                            {
                                var toupper = value.ToUpperInvariant();
                                if (toupper == "YES" || toupper == "TRUE")
                                    oset = true;
                                else
                                    oset = false;
                            } break;
                        case TSMDataType.DateTime:
                            oset = DateTime.Parse(value); break;
                        case TSMDataType.Guid:
                            {
                                var guid = value.Replace(".", "");
                                oset = Guid.Parse(guid);
                            } break;
                        default:
                            throw new NotImplementedException();
                    }

                    column.SetValue(o, oset);
                    i++;
                }


                result.Add((T)o);
            }


            return result;
        }

        public static List<T> Convert<T>(List<List<string>> CsvLines, clsTypeInfo myType, List<clsColumnInfo> Columns)
        {
            var result = new List<T>();

            var arrColumns = Columns.ToArray();

            foreach (var listValues in CsvLines)
            {
                var o = myType.CreateObject();

                int i = 0;
                foreach (var v in listValues)
                {
                    if (i == arrColumns.Length)
                        break;
                    clsColumnInfo column = arrColumns[i++];

                    
                    var flagNull = v.Length == 0;
                    if (flagNull) // Uber Fance null handling.
                    {
                        i++;
                        continue;
                    }

                    object oset = null;

                    switch (column.DataType)
                    {
                        case TSMDataType.String:
                            oset = v; break;
                        case TSMDataType.Short:
                            oset = short.Parse(v); break;
                        case TSMDataType.Int:
                            oset = int.Parse(v); break;
                        case TSMDataType.Long:
                            oset = long.Parse(v); break;
                        case TSMDataType.Double:
                            //var E = v.IndexOf('E');
                            //if (E != -1)
                            //{
                            //    var d = double.Parse(v.Substring(0, E), CultureInfo.InvariantCulture);
                            //    var multiplier = int.Parse(v.Substring(E + 2));

                            //    if (v[E + 1] == '+')
                            //        oset = d * Math.Pow(10, multiplier);
                            //    else if (v[E + 1] == '-')
                            //        oset = d / Math.Pow(10, multiplier);
                            //    else
                            //        throw new NotImplementedException();
                            //}
                            //else
                            //    oset = double.Parse(v, CultureInfo.InvariantCulture);
                            oset = ParseDB2Double(v);
                            break;
                        case TSMDataType.Float:
                            //oset = float.Parse(v, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture )
                            oset = float.Parse(v); break;
                        case TSMDataType.Bool:
                            {
                                var toupper = v.ToUpperInvariant();
                                if (toupper == "YES" || toupper == "TRUE")
                                    oset = true;
                                else
                                    oset = false;
                            }break;
                        case TSMDataType.DateTime:
                            oset = DateTime.Parse(v); break;
                        case TSMDataType.Guid:
                            {
                                var guid = v.Replace(".", "");
                                oset = Guid.Parse(guid);
                            } break;
                        default:
                            throw new NotImplementedException();
                    }

                    column.SetValue(o, oset);
                }


                result.Add((T)o);
            }


            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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
    }
}
