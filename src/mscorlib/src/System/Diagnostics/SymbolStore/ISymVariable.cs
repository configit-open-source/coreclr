namespace System.Diagnostics.SymbolStore
{
    using System;

    public interface ISymbolVariable
    {
        String Name
        {
            get;
        }

        Object Attributes
        {
            get;
        }

        byte[] GetSignature();
        SymAddressKind AddressKind
        {
            get;
        }

        int AddressField1
        {
            get;
        }

        int AddressField2
        {
            get;
        }

        int AddressField3
        {
            get;
        }

        int StartOffset
        {
            get;
        }

        int EndOffset
        {
            get;
        }
    }
}