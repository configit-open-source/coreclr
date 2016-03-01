namespace System.Runtime.InteropServices
{
    public interface UCOMIConnectionPointContainer
    {
        void EnumConnectionPoints(out UCOMIEnumConnectionPoints ppEnum);
        void FindConnectionPoint(ref Guid riid, out UCOMIConnectionPoint ppCP);
    }
}