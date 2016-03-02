namespace System.Runtime.InteropServices
{
    using DWORD = System.UInt32;

    public interface UCOMIEnumMoniker
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] UCOMIMoniker[] rgelt, out int pceltFetched);
        int Skip(int celt);
        int Reset();
        void Clone(out UCOMIEnumMoniker ppenum);
    }
}