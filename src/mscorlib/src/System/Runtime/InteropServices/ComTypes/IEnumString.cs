namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public interface IEnumString
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), Out] String[] rgelt, IntPtr pceltFetched);
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumString ppenum);
    }
}