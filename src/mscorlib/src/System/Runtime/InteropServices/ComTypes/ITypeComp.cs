namespace System.Runtime.InteropServices.ComTypes
{
    public enum DESCKIND
    {
        DESCKIND_NONE = 0,
        DESCKIND_FUNCDESC = DESCKIND_NONE + 1,
        DESCKIND_VARDESC = DESCKIND_FUNCDESC + 1,
        DESCKIND_TYPECOMP = DESCKIND_VARDESC + 1,
        DESCKIND_IMPLICITAPPOBJ = DESCKIND_TYPECOMP + 1,
        DESCKIND_MAX = DESCKIND_IMPLICITAPPOBJ + 1
    }

    public struct BINDPTR
    {
        public IntPtr lpfuncdesc;
        public IntPtr lpvardesc;
        public IntPtr lptcomp;
    }

    public interface ITypeComp
    {
        void Bind([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, Int16 wFlags, out ITypeInfo ppTInfo, out DESCKIND pDescKind, out BINDPTR pBindPtr);
        void BindType([MarshalAs(UnmanagedType.LPWStr)] String szName, int lHashVal, out ITypeInfo ppTInfo, out ITypeComp ppTComp);
    }
}