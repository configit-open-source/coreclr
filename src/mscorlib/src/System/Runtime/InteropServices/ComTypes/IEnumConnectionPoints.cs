namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public interface IEnumConnectionPoints
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] IConnectionPoint[] rgelt, IntPtr pceltFetched);
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumConnectionPoints ppenum);
    }
}