namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolDocument
    {
        String URL
        {
            get;
        }

        Guid DocumentType
        {
            get;
        }

        Guid Language
        {
            get;
        }

        Guid LanguageVendor
        {
            get;
        }

        Guid CheckSumAlgorithmId
        {
            get;
        }

        byte[] GetCheckSum();
        int FindClosestLine(int line);
        bool HasEmbeddedSource
        {
            get;
        }

        int SourceLength
        {
            get;
        }

        byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn);
    }
}