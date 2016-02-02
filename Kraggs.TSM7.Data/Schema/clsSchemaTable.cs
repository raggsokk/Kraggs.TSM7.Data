using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Kraggs.TSM7.Data.Schema
{
    
    [DebuggerDisplay("{TabName}")]
    internal class clsSchemaTable
    {
        public string TabName { get; set; }

        public int ColCount { get; set; }

        public List<clsSchemaColumn> Columns { get; set; }
    }
}
