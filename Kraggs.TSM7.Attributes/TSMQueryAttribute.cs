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

namespace Kraggs.TSM7.Data
{
    /// <summary>
    /// Sets up the SQL Query to use with a class.
    /// Instead of cluttering the code with sql code all over,
    /// you mark a class with the sql to use.
    /// That means the sql query and its class is in the same code file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class TSMQueryAttribute : Attribute
    {
        /// <summary>
        /// A sql query to use with the class.
        /// </summary>
        public string TSMSqlQuery { get; set; }

        /// <summary>
        /// Sets up a SQL Query to use with the class.
        /// </summary>
        /// <param name="TSMSqlQuery"></param>
        public TSMQueryAttribute(string TSMSqlQuery)
        {
            this.TSMSqlQuery = TSMSqlQuery;
        }
    }
}
