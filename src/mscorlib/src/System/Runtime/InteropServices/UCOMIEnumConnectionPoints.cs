namespace System.Runtime.InteropServices
{
    public interface UCOMIEnumConnectionPoints
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] UCOMIConnectionPoint[] rgelt, out int pceltFetched);
        int Skip(int celt);
        int Reset();
        void Clone(out UCOMIEnumConnectionPoints ppenum);
    }
}