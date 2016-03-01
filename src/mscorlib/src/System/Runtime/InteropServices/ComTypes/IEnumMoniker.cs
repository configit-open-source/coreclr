namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public interface IEnumMoniker
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] IMoniker[] rgelt, IntPtr pceltFetched);
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumMoniker ppenum);
    }
}