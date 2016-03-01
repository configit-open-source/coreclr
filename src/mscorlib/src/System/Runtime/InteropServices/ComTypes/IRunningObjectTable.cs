namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public interface IRunningObjectTable
    {
        int Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] Object punkObject, IMoniker pmkObjectName);
        void Revoke(int dwRegister);
        int IsRunning(IMoniker pmkObjectName);
        int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out Object ppunkObject);
        void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);
        int GetTimeOfLastChange(IMoniker pmkObjectName, out FILETIME pfiletime);
        void EnumRunning(out IEnumMoniker ppenumMoniker);
    }
}