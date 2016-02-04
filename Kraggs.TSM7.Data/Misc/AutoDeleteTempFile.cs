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

namespace Kraggs.TSM7.Data.Misc
{
    internal class AutoDeleteTempFile : IDisposable
    {
        //protected string pTempFile;
        public Stream Stream { get; protected set; }
        public string TempFile { get; protected set; }

        public AutoDeleteTempFile()
        {
            //Stream = new FileStream(pTempFile, FileMode.CreateNew,
            //    FileAccess.ReadWrite,)
            TempFile = Path.GetTempFileName();
        }

        public void Open(bool AutoDelete = true, bool Async = true)
        {
            FileOptions options = FileOptions.SequentialScan;
            if (AutoDelete)
                options = options | FileOptions.DeleteOnClose;
            if (Async)
                options = options | FileOptions.Asynchronous;

            Stream = new FileStream(TempFile, FileMode.Open, FileAccess.Read, FileShare.None,
                8192, options);
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            if(Stream != null)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected void Dispose(bool disposing)
        {

            try
            {
                if (File.Exists(TempFile))
                    File.Delete(TempFile);
            }
            catch(Exception)
            { }
        }

        ~AutoDeleteTempFile()
        {
            this.Dispose(false);
        }
    }
}
