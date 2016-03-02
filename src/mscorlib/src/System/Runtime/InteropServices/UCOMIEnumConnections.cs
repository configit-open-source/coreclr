namespace System.Runtime.InteropServices
{
    public struct CONNECTDATA
    {
        public Object pUnk;
        public int dwCookie;
    }

    public interface UCOMIEnumConnections
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] CONNECTDATA[] rgelt, out int pceltFetched);
        int Skip(int celt);
        void Reset();
        void Clone(out UCOMIEnumConnections ppenum);
    }
}