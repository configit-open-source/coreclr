namespace System.Diagnostics.SymbolStore
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    public interface ISymbolBinder
    {
        ISymbolReader GetReader(int importer, String filename, String searchPath);
    }

    public interface ISymbolBinder1
    {
        ISymbolReader GetReader(IntPtr importer, String filename, String searchPath);
    }
}