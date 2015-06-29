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

namespace Kraggs.TSM7.Data
{
    internal static class CsvParser
    {
        public static List<string> Parse(string CsvLine, StringBuilder reuse = null, char CsvSeparator = ',')
        {
            if (reuse == null)
                reuse = new StringBuilder(CsvLine.Length);
            else
            {
                reuse.Length = 0;
            }

            var list = new List<string>();

            var flagQ1 = false;
            var flagQ2 = false;

            foreach (char c in CsvLine.ToCharArray())
            {
                // fast skip inside quote[s]
                if (
                    (flagQ1 && c != '\'') ||
                    (flagQ2 && c != '"'))
                {
                    reuse.Append(c);
                    continue;
                }
                else if (c == '\'')
                    flagQ1 = !flagQ1;
                else if (c == '"')
                    flagQ2 = !flagQ2;
                else if (c == CsvSeparator)
                {
                    list.Add(reuse.ToString());
                    reuse.Length = 0;
                }
                else
                    reuse.Append(c);
            }

            // add list item.
            list.Add(reuse.ToString());

            return list;
        }

        public static int Parse(List<string> CsvLines, System.Collections.IList result, char CsvSeparator = ',')
        {
            if (CsvLines == null)
                throw new ArgumentNullException("CsvLines");
            if (result == null)
                throw new ArgumentNullException("result");

            var reuse = new StringBuilder();
            var count = 0;

            foreach (var line in CsvLines)
            {
                List<string> list = null;

                list = Parse(line, reuse, CsvSeparator);

                if (list != null)
                {
                    result.Add(list);
                    count++;
                }
            }

            return count;
        }

    }
}
