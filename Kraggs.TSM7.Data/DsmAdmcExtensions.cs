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
using System.Linq.Expressions;

using Kraggs.TSM7;
using Kraggs.TSM7.Utils;

namespace Kraggs.TSM7.Data
{
    public static class DsmAdmcExtensions
    {
        public static List<T> Where<T>(this clsDsmAdmc dsmadmc, Expression<Func<T, bool>> filter)
        {
            /*
             * 1. generate sql.
             * 2. call tsm
             * 3. parse csv to list.
             */
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

            if (!ExpressionHelper.BuildWhereString(sb, filter))
                return null;

            var list = new List<string>();            

            var retCode = dsmadmc.RunTSMCommandToList(sb.ToString(), list);

            List<List<string>> parsed = new List<List<string>>();

            int parseCount = CsvParser.Parse(list, parsed);

            return CsvConvert.Convert<T>(parsed, myType, myType.Columns);
        }

    }
}
