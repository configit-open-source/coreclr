namespace System.Diagnostics.SymbolStore
{
    using System;

    public interface ISymbolDocumentWriter
    {
        void SetSource(byte[] source);
        void SetCheckSum(Guid algorithmId, byte[] checkSum);
    }
}