namespace System.IO.IsolatedStorage
{
    using System;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public class IsolatedStorageFileStream : FileStream
    {
        private const int s_BlockSize = 1024;
        private const String s_BackSlash = "\\";
        private FileStream m_fs;
        private IsolatedStorageFile m_isf;
        private String m_GivenPath;
        private String m_FullPath;
        private bool m_OwnedStore;
        private IsolatedStorageFileStream()
        {
        }

        public IsolatedStorageFileStream(String path, FileMode mode, IsolatedStorageFile isf): this (path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None, isf)
        {
        }

        public IsolatedStorageFileStream(String path, FileMode mode, FileAccess access, IsolatedStorageFile isf): this (path, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None, DefaultBufferSize, isf)
        {
        }

        public IsolatedStorageFileStream(String path, FileMode mode, FileAccess access, FileShare share, IsolatedStorageFile isf): this (path, mode, access, share, DefaultBufferSize, isf)
        {
        }

        public IsolatedStorageFileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, IsolatedStorageFile isf)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            if ((path.Length == 0) || path.Equals(s_BackSlash))
                throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_Path"));
            if (isf == null)
            {
                throw new ArgumentNullException("isf");
            }

            if (isf.Disposed)
                throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            switch (mode)
            {
                case FileMode.CreateNew:
                case FileMode.Create:
                case FileMode.OpenOrCreate:
                case FileMode.Truncate:
                case FileMode.Append:
                case FileMode.Open:
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_FileOpenMode"));
            }

            m_isf = isf;
            m_GivenPath = path;
            m_FullPath = m_isf.GetFullPath(m_GivenPath);
            try
            {
                m_isf.Demand(m_FullPath);
                m_fs = new FileStream(m_FullPath, mode, access, share, bufferSize, FileOptions.None, m_GivenPath, true);
            }
            catch (Exception e)
            {
                throw IsolatedStorageFile.GetIsolatedStorageException("IsolatedStorage_Operation_ISFS", e);
            }
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return m_fs.CanRead;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return m_fs.CanWrite;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return m_fs.CanSeek;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return m_fs.IsAsync;
            }
        }

        public override long Length
        {
            get
            {
                return m_fs.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_fs.Position;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                }

                Contract.EndContractBlock();
                Seek(value, SeekOrigin.Begin);
            }
        }

        public new string Name
        {
            [SecurityCritical]
            get
            {
                return m_FullPath;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    try
                    {
                        if (m_fs != null)
                            m_fs.Close();
                    }
                    finally
                    {
                        if (m_OwnedStore && m_isf != null)
                            m_isf.Close();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            m_fs.Flush();
        }

        public override void Flush(Boolean flushToDisk)
        {
            m_fs.Flush(flushToDisk);
        }

        public override IntPtr Handle
        {
            [System.Security.SecurityCritical]
            get
            {
                NotPermittedError();
                return Win32Native.INVALID_HANDLE_VALUE;
            }
        }

        public override SafeFileHandle SafeFileHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                NotPermittedError();
                return null;
            }
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            ulong oldLen = (ulong)m_fs.Length;
            ulong newLen = (ulong)value;
            ZeroInit(oldLen, newLen);
            m_fs.SetLength(value);
        }

        public override void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_fs.Lock(position, length);
        }

        public override void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_fs.Unlock(position, length);
        }

        private void ZeroInit(ulong oldLen, ulong newLen)
        {
            if (oldLen >= newLen)
                return;
            ulong rem = newLen - oldLen;
            byte[] buffer = new byte[s_BlockSize];
            long pos = m_fs.Position;
            m_fs.Seek((long)oldLen, SeekOrigin.Begin);
            if (rem <= (ulong)s_BlockSize)
            {
                m_fs.Write(buffer, 0, (int)rem);
                m_fs.Position = pos;
                return;
            }

            int allign = s_BlockSize - (int)(oldLen & ((ulong)s_BlockSize - 1));
            m_fs.Write(buffer, 0, allign);
            rem -= (ulong)allign;
            int nBlocks = (int)(rem / s_BlockSize);
            for (int i = 0; i < nBlocks; ++i)
                m_fs.Write(buffer, 0, s_BlockSize);
            m_fs.Write(buffer, 0, (int)(rem & ((ulong)s_BlockSize - 1)));
            m_fs.Position = pos;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_fs.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return m_fs.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long ret;
            ulong oldLen;
            ulong newLen;
            oldLen = (ulong)m_fs.Length;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newLen = (ulong)((offset < 0) ? 0 : offset);
                    break;
                case SeekOrigin.Current:
                    newLen = (ulong)((m_fs.Position + offset) < 0 ? 0 : (m_fs.Position + offset));
                    break;
                case SeekOrigin.End:
                    newLen = (ulong)((m_fs.Length + offset) < 0 ? 0 : (m_fs.Length + offset));
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_SeekOrigin"));
            }

            ZeroInit(oldLen, newLen);
            ret = m_fs.Seek(offset, origin);
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_fs.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            m_fs.WriteByte(value);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            return m_fs.BeginRead(buffer, offset, numBytes, userCallback, stateObject);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();
            return m_fs.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            return m_fs.BeginWrite(buffer, offset, numBytes, userCallback, stateObject);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();
            m_fs.EndWrite(asyncResult);
        }

        internal void NotPermittedError(String str)
        {
            throw new IsolatedStorageException(str);
        }

        internal void NotPermittedError()
        {
            NotPermittedError(Environment.GetResourceString("IsolatedStorage_Operation_ISFS"));
        }
    }
}