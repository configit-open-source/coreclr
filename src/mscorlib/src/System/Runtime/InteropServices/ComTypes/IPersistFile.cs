namespace System.Runtime.InteropServices.ComTypes
{
    public interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] String pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] String pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] String pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out String ppszFileName);
    }
}