namespace System.Runtime.InteropServices
{
    public enum SYSKIND
    {
        SYS_WIN16 = 0,
        SYS_WIN32 = SYS_WIN16 + 1,
        SYS_MAC = SYS_WIN32 + 1
    }

    public enum LIBFLAGS : short
    {
        LIBFLAG_FRESTRICTED = 0x1,
        LIBFLAG_FCONTROL = 0x2,
        LIBFLAG_FHIDDEN = 0x4,
        LIBFLAG_FHASDISKIMAGE = 0x8
    }

    public struct TYPELIBATTR
    {
        public Guid guid;
        public int lcid;
        public SYSKIND syskind;
        public Int16 wMajorVerNum;
        public Int16 wMinorVerNum;
        public LIBFLAGS wLibFlags;
    }

    public interface UCOMITypeLib
    {
        int GetTypeInfoCount();
        void GetTypeInfo(int index, out UCOMITypeInfo ppTI);
        void GetTypeInfoType(int index, out TYPEKIND pTKind);
        void GetTypeInfoOfGuid(ref Guid guid, out UCOMITypeInfo ppTInfo);
        void GetLibAttr(out IntPtr ppTLibAttr);
        void GetTypeComp(out UCOMITypeComp ppTComp);
        void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        bool IsName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal);
        void FindName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] UCOMITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref Int16 pcFound);
        void ReleaseTLibAttr(IntPtr pTLibAttr);
    }
}