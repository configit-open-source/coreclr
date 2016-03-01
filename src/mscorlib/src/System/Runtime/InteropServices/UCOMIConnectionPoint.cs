namespace System.Runtime.InteropServices
{
    public interface UCOMIConnectionPoint
    {
        void GetConnectionInterface(out Guid pIID);
        void GetConnectionPointContainer(out UCOMIConnectionPointContainer ppCPC);
        void Advise([MarshalAs(UnmanagedType.Interface)] Object pUnkSink, out int pdwCookie);
        void Unadvise(int dwCookie);
        void EnumConnections(out UCOMIEnumConnections ppEnum);
    }
}