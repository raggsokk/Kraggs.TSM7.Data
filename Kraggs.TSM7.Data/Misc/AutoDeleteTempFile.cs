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
