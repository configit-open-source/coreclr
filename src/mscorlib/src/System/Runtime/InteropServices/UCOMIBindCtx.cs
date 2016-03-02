namespace System.Runtime.InteropServices
{
    public struct BIND_OPTS
    {
        public int cbStruct;
        public int grfFlags;
        public int grfMode;
        public int dwTickCountDeadline;
    }

    public interface UCOMIBindCtx
    {
        void RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] Object punk);
        void RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] Object punk);
        void ReleaseBoundObjects();
        void SetBindOptions([In()] ref BIND_OPTS pbindopts);
        void GetBindOptions(ref BIND_OPTS pbindopts);
        void GetRunningObjectTable(out UCOMIRunningObjectTable pprot);
        void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey, [MarshalAs(UnmanagedType.Interface)] Object punk);
        void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);
        void EnumObjectParam(out UCOMIEnumString ppenum);
        void RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] String pszKey);
    }
}