using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System.IO
{
    internal sealed unsafe class PinnedBufferMemoryStream : UnmanagedMemoryStream
    {
        private byte[] _array;
        private GCHandle _pinningHandle;
        private PinnedBufferMemoryStream(): base ()
        {
        }

        internal PinnedBufferMemoryStream(byte[] array)
        {
            Contract.Assert(array != null, "Array can't be null");
            int len = array.Length;
            if (len == 0)
            {
                array = new byte[1];
                len = 0;
            }

            _array = array;
            _pinningHandle = new GCHandle(array, GCHandleType.Pinned);
            fixed (byte *ptr = _array)
                Initialize(ptr, len, len, FileAccess.Read, true);
        }

        ~PinnedBufferMemoryStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isOpen)
            {
                _pinningHandle.Free();
                _isOpen = false;
            }

            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            base.Dispose(disposing);
        }
    }
}