using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Kraggs.TSM7.Data.Schema
{
    [DebuggerDisplay("{ColName}")]
    public class clsSchemaColumn
    {
        public string ColName { get; set; }

        public int ColNo { get; set; }

        public string TypeName { get; set; }

        public int Length { get; set; }

        public int Scale { get; set; }

        public bool Nulls { get; set; }

    }
}
