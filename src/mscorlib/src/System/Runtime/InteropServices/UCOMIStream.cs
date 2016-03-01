namespace System.Runtime.InteropServices
{
    using System;

    public struct STATSTG
    {
        public String pwcsName;
        public int type;
        public Int64 cbSize;
        public FILETIME mtime;
        public FILETIME ctime;
        public FILETIME atime;
        public int grfMode;
        public int grfLocksSupported;
        public Guid clsid;
        public int grfStateBits;
        public int reserved;
    }

    public interface UCOMIStream
    {
        void Read([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Byte[] pv, int cb, IntPtr pcbRead);
        void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Byte[] pv, int cb, IntPtr pcbWritten);
        void Seek(Int64 dlibMove, int dwOrigin, IntPtr plibNewPosition);
        void SetSize(Int64 libNewSize);
        void CopyTo(UCOMIStream pstm, Int64 cb, IntPtr pcbRead, IntPtr pcbWritten);
        void Commit(int grfCommitFlags);
        void Revert();
        void LockRegion(Int64 libOffset, Int64 cb, int dwLockType);
        void UnlockRegion(Int64 libOffset, Int64 cb, int dwLockType);
        void Stat(out STATSTG pstatstg, int grfStatFlag);
        void Clone(out UCOMIStream ppstm);
    }
}