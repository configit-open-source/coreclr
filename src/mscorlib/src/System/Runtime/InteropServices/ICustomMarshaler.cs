namespace System.Runtime.InteropServices
{
    using System;

    public interface ICustomMarshaler
    {
        Object MarshalNativeToManaged(IntPtr pNativeData);
        IntPtr MarshalManagedToNative(Object ManagedObj);
        void CleanUpNativeData(IntPtr pNativeData);
        void CleanUpManagedData(Object ManagedObj);
        int GetNativeDataSize();
    }
}