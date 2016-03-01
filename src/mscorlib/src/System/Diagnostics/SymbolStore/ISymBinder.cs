namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolBinder
    {
        ISymbolReader GetReader(int importer, String filename, String searchPath);
    }

    public interface ISymbolBinder1
    {
        ISymbolReader GetReader(IntPtr importer, String filename, String searchPath);
    }
}