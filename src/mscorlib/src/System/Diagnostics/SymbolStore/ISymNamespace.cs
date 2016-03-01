namespace System.Diagnostics.SymbolStore
{
    using System;

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