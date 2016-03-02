namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolScope
    {
        ISymbolMethod Method
        {
            get;
        }

        ISymbolScope Parent
        {
            get;
        }

        ISymbolScope[] GetChildren();
        int StartOffset
        {
            get;
        }

        int EndOffset
        {
            get;
        }

        ISymbolVariable[] GetLocals();
        ISymbolNamespace[] GetNamespaces();
    }
}