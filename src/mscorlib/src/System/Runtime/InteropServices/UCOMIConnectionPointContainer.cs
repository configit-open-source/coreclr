namespace System.Runtime.InteropServices
{
    using System;

    public interface UCOMIConnectionPointContainer
    {
        void EnumConnectionPoints(out UCOMIEnumConnectionPoints ppEnum);
        void FindConnectionPoint(ref Guid riid, out UCOMIConnectionPoint ppCP);
    }
}