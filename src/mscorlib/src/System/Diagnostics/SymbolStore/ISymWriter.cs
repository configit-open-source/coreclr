namespace System.Diagnostics.SymbolStore
{
    using System;
    using System.Text;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    public interface ISymbolWriter
    {
        void Initialize(IntPtr emitter, String filename, bool fFullBuild);
        ISymbolDocumentWriter DefineDocument(String url, Guid language, Guid languageVendor, Guid documentType);
        void SetUserEntryPoint(SymbolToken entryMethod);
        void OpenMethod(SymbolToken method);
        void CloseMethod();
        void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);
        int OpenScope(int startOffset);
        void CloseScope(int endOffset);
        void SetScopeRange(int scopeID, int startOffset, int endOffset);
        void DefineLocalVariable(String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
        void DefineParameter(String name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void DefineField(SymbolToken parent, String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void DefineGlobalVariable(String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void Close();
        void SetSymAttribute(SymbolToken parent, String name, byte[] data);
        void OpenNamespace(String name);
        void CloseNamespace();
        void UsingNamespace(String fullName);
        void SetMethodSourceRange(ISymbolDocumentWriter startDoc, int startLine, int startColumn, ISymbolDocumentWriter endDoc, int endLine, int endColumn);
        void SetUnderlyingWriter(IntPtr underlyingWriter);
    }
}