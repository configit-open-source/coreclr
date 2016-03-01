namespace System.Runtime.InteropServices
{
    using System;

    public interface UCOMIRunningObjectTable
    {
        void Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] Object punkObject, UCOMIMoniker pmkObjectName, out int pdwRegister);
        void Revoke(int dwRegister);
        void IsRunning(UCOMIMoniker pmkObjectName);
        void GetObject(UCOMIMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out Object ppunkObject);
        void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);
        void GetTimeOfLastChange(UCOMIMoniker pmkObjectName, out FILETIME pfiletime);
        void EnumRunning(out UCOMIEnumMoniker ppenumMoniker);
    }
}