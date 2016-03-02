namespace System.Runtime.InteropServices.ComTypes
{
    public interface IConnectionPointContainer
    {
        void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);
        void FindConnectionPoint([In] ref Guid riid, out IConnectionPoint ppCP);
    }
}