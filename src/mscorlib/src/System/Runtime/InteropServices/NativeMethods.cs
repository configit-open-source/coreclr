namespace System.Runtime.InteropServices
{
    internal static class NativeMethods
    {
        internal static extern void VariantClear(IntPtr variant);
        internal interface IDispatch
        {
            void GetTypeInfoCount(out uint pctinfo);
            void GetTypeInfo(uint iTInfo, int lcid, out IntPtr info);
            void GetIDsOfNames(ref Guid iid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] names, uint cNames, int lcid, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4, SizeParamIndex = 2)] int[] rgDispId);
            void Invoke(int dispIdMember, ref Guid riid, int lcid, ComTypes.INVOKEKIND wFlags, ref ComTypes.DISPPARAMS pDispParams, IntPtr pvarResult, IntPtr pExcepInfo, IntPtr puArgErr);
        }
    }
}