namespace System.Runtime.InteropServices
{
    using System;

    public interface UCOMIEnumString
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), Out] String[] rgelt, out int pceltFetched);
        int Skip(int celt);
        int Reset();
        void Clone(out UCOMIEnumString ppenum);
    }
}