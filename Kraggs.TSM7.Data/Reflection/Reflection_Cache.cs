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

using System.Diagnostics;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;

namespace Kraggs.TSM7.Data
{
    partial class Reflection
    {
        internal SortedDictionary<string, clsTypeInfo> sTypeCache;

        internal void RegisterMap(Type t, Type map)
        {
            if (sTypeCache == null)
                sTypeCache = new SortedDictionary<string, clsTypeInfo>();

            var typename = t.FullName;

            clsTypeInfo ti = null;

            if (sTypeCache.TryGetValue(typename, out ti))
            {
                if (ti.MapType == map)
                    return;
            }

            var myType = CreateTypeInfo(t, map);

            if (ti == null)
                sTypeCache.Add(typename, ti);
            else
                sTypeCache[typename] = ti;
        }

        internal clsTypeInfo GetTypeInfo(string key, Type t)
        {
            if (sTypeCache == null)
                sTypeCache = new SortedDictionary<string, clsTypeInfo>();

            clsTypeInfo ti = null;

            if (!sTypeCache.TryGetValue(key, out ti))
            {
                ti = CreateTypeInfo(t);
                sTypeCache.Add(key, ti);
            }

            return ti;
        }

        internal static clsTypeInfo CreateTypeInfo(Type t, Type map = null)
        {
            var myType = new clsTypeInfo() { TypeName = t.FullName, Type = t, MapType = map };

            var tiT = t.GetTypeInfo();
            var tiMap = map == null ? tiT : map.GetTypeInfo();

            // 1. get type attribs
            var attribTable = tiMap.GetCustomAttribute<TSMTableAttribute>();
            if (attribTable != null && !string.IsNullOrWhiteSpace(attribTable.TableName))
                myType.TableName = attribTable.TableName;
            else
                myType.TableName = tiMap.Name; // tiT.Name;

            myType.CreateObject = CreateConstructorMethod(t, tiT);

            // 2. foreach(prop) get attrib.
            // create source hash.
            SortedDictionary<string, PropertyInfo> tiPropHash = null;
            if (map != null)
            {
                tiPropHash = new SortedDictionary<string, PropertyInfo>();
                foreach (var p in tiT.DeclaredProperties)
                    tiPropHash.Add(p.Name, p);
            }

            foreach (var p in tiMap.DeclaredProperties)
            {
                if (map != null)
                    if (!tiPropHash.ContainsKey(p.Name))
                        continue; // if not matching key name, skip it.

                var cT = new clsColumnInfo() { MemberName = p.Name };

                var attribColumn = p.GetCustomAttribute<TSMColumnAttribute>();
                if (attribColumn != null && !string.IsNullOrWhiteSpace(attribColumn.ColumnName))
                    cT.ColumnName = attribColumn.ColumnName;
                else
                    cT.ColumnName = cT.MemberName;

                // CSV Convert info
                cT.MemberType = p.PropertyType;
                cT.SetValue = CreateSetProperty(p);
                cT.DataType = GetTSMDataType(p.PropertyType);

                myType.Add(cT);
            }


            return myType;
        }

        internal static TSMDataType GetTSMDataType(Type t)
        {
            if (t == typeof(String))
                return TSMDataType.String;
            else if (t == typeof(short))
                return TSMDataType.Short;
            else if (t == typeof(int))
                return TSMDataType.Int;
            else if (t == typeof(long))
                return TSMDataType.Long;
            else if (t == typeof(double))
                return TSMDataType.Double;
            else if (t == typeof(float))
                return TSMDataType.Float;
            else if (t == typeof(DateTime))
                return TSMDataType.DateTime;
            else if (t == typeof(Guid))
                return TSMDataType.Guid;

            return TSMDataType.Undefined;
        }

    }
}
