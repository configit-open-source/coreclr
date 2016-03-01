namespace System.Runtime.InteropServices.ComTypes
{
    public interface IEnumVARIANT
    {
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] object[] rgVar, IntPtr pceltFetched);
        int Skip(int celt);
        int Reset();
        IEnumVARIANT Clone();
    }
}