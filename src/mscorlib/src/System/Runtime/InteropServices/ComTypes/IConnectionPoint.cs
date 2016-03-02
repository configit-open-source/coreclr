namespace System.Runtime.InteropServices.ComTypes
{
    public interface IConnectionPoint
    {
        void GetConnectionInterface(out Guid pIID);
        void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);
        void Advise([MarshalAs(UnmanagedType.Interface)] Object pUnkSink, out int pdwCookie);
        void Unadvise(int dwCookie);
        void EnumConnections(out IEnumConnections ppEnum);
    }
}