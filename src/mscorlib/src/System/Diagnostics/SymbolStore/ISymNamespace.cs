namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolNamespace
    {
        String Name
        {
            get;
        }

        ISymbolNamespace[] GetNamespaces();
        ISymbolVariable[] GetVariables();
    }
}