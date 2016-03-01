namespace System.Diagnostics.SymbolStore
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

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