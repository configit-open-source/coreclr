namespace System.Runtime.InteropServices.ComTypes
{
    public struct CONNECTDATA
    {
        public Object pUnk;
        public int dwCookie;
    }

    public interface IEnumConnections
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] CONNECTDATA[] rgelt, IntPtr pceltFetched);
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumConnections ppenum);
    }
}