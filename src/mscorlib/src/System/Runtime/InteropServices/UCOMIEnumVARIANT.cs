namespace System.Runtime.InteropServices
{
    public interface UCOMIEnumVARIANT
    {
        int Next(int celt, int rgvar, int pceltFetched);
        int Skip(int celt);
        int Reset();
        void Clone(int ppenum);
    }
}