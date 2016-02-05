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
using System.Diagnostics;

namespace Kraggs.TSM7.Data
{
    /// <summary>
    /// Contains data for a type.
    /// </summary>
    [DebuggerDisplay("{TableName}")]
    internal class clsTypeInfo
    {
        /// <summary>
        /// Name of type
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Name of tsm table to query agains.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Sets the required TSM Version where this table is present.
        /// example: summary_extended
        /// </summary>
        public Version RequiredVersion { get; set; }

        /// <summary>
        /// Reference to the Type this clsTypeInfo describes.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Reference to a Type with Attribute Mapping to use.
        /// </summary>
        public Type MapType { get; set; }

        /// <summary>
        /// Delegate function which creates an object.
        /// </summary>
        public CreateObject CreateObject { get; set; }

        public string TSMSqlQuery { get; set; }

        /// <summary>
        /// List of properties on this type.
        /// </summary>
        public List<clsColumnInfo> Columns { get; set; }
        /// <summary>
        /// Lookup table using Column Name.
        /// </summary>
        public SortedDictionary<string, clsColumnInfo> ColumnHash { get; protected set; }
        /// <summary>
        /// Lookup table using Member Name.
        /// </summary>
        public SortedDictionary<string, clsColumnInfo> MemberHash { get; protected set; }

        [DebuggerNonUserCode()]
        public clsTypeInfo()
        {
            this.Columns = new List<clsColumnInfo>();
            this.ColumnHash = new SortedDictionary<string, clsColumnInfo>();
            this.MemberHash = new SortedDictionary<string, clsColumnInfo>();
        }

        /// <summary>
        /// Adds the Column Info to all the data structures.
        /// </summary>
        /// <param name="ci"></param>
        [DebuggerNonUserCode()]
        public void Add(clsColumnInfo ci)
        {
            if(ColumnHash.ContainsKey(ci.ColumnName))
            {
                // TODO: how should we react to this?
                // we can actually report 
                throw new ArgumentException(string.Format(
                    "ColumnName '{0}' is already registered. Column Names are not case sensitive and must be unique on a given type. Offending Property/Field: '{1}'", ci.ColumnName, ci.MemberName));
            }
            else
            {
                Columns.Add(ci);
                ColumnHash.Add(ci.ColumnName, ci);
                //ColumnHash.Add(ci.ColumnName.ToUpperInvariant(), ci);
                MemberHash.Add(ci.MemberName, ci);
            }
        }
    }
}
