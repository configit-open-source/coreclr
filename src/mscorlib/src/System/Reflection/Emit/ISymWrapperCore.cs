using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Emit
{
    class SymWrapperCore
    {
        private SymWrapperCore()
        {
        }

        private unsafe class SymDocumentWriter : ISymbolDocumentWriter
        {
            internal SymDocumentWriter(PunkSafeHandle pDocumentWriterSafeHandle)
            {
                m_pDocumentWriterSafeHandle = pDocumentWriterSafeHandle;
                m_pDocWriter = (ISymUnmanagedDocumentWriter*)m_pDocumentWriterSafeHandle.DangerousGetHandle();
                m_vtable = (ISymUnmanagedDocumentWriterVTable)(Marshal.PtrToStructure(m_pDocWriter->m_unmanagedVTable, typeof (ISymUnmanagedDocumentWriterVTable)));
            }

            internal PunkSafeHandle GetUnmanaged()
            {
                return m_pDocumentWriterSafeHandle;
            }

            void ISymbolDocumentWriter.SetSource(byte[] source)
            {
                throw new NotSupportedException();
            }

            void ISymbolDocumentWriter.SetCheckSum(Guid algorithmId, byte[] checkSum)
            {
                int hr = m_vtable.SetCheckSum(m_pDocWriter, algorithmId, (uint)checkSum.Length, checkSum);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            private delegate int DSetCheckSum(ISymUnmanagedDocumentWriter*pThis, Guid algorithmId, uint checkSumSize, [In] byte[] checkSum);
            private struct ISymUnmanagedDocumentWriterVTable
            {
                internal IntPtr QueryInterface;
                internal IntPtr AddRef;
                internal IntPtr Release;
                internal IntPtr SetSource;
                internal DSetCheckSum SetCheckSum;
            }

            private struct ISymUnmanagedDocumentWriter
            {
                internal IntPtr m_unmanagedVTable;
            }

            private PunkSafeHandle m_pDocumentWriterSafeHandle;
            private ISymUnmanagedDocumentWriter*m_pDocWriter;
            private ISymUnmanagedDocumentWriterVTable m_vtable;
        }

        internal unsafe class SymWriter : ISymbolWriter
        {
            internal static ISymbolWriter CreateSymWriter()
            {
                return new SymWriter();
            }

            private SymWriter()
            {
            }

            void ISymbolWriter.Initialize(IntPtr emitter, String filename, bool fFullBuild)
            {
                int hr = m_vtable.Initialize(m_pWriter, emitter, filename, (IntPtr)0, fFullBuild);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            ISymbolDocumentWriter ISymbolWriter.DefineDocument(String url, Guid language, Guid languageVendor, Guid documentType)
            {
                PunkSafeHandle psymUnmanagedDocumentWriter = new PunkSafeHandle();
                int hr = m_vtable.DefineDocument(m_pWriter, url, ref language, ref languageVendor, ref documentType, out psymUnmanagedDocumentWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                if (psymUnmanagedDocumentWriter.IsInvalid)
                {
                    return null;
                }

                return new SymDocumentWriter(psymUnmanagedDocumentWriter);
            }

            void ISymbolWriter.SetUserEntryPoint(SymbolToken entryMethod)
            {
                int hr = m_vtable.SetUserEntryPoint(m_pWriter, entryMethod.GetToken());
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.OpenMethod(SymbolToken method)
            {
                int hr = m_vtable.OpenMethod(m_pWriter, method.GetToken());
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.CloseMethod()
            {
                int hr = m_vtable.CloseMethod(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
            {
                int spCount = 0;
                if (offsets != null)
                {
                    spCount = offsets.Length;
                }
                else if (lines != null)
                {
                    spCount = lines.Length;
                }
                else if (columns != null)
                {
                    spCount = columns.Length;
                }
                else if (endLines != null)
                {
                    spCount = endLines.Length;
                }
                else if (endColumns != null)
                {
                    spCount = endColumns.Length;
                }

                if (spCount == 0)
                {
                    return;
                }

                if ((offsets != null && offsets.Length != spCount) || (lines != null && lines.Length != spCount) || (columns != null && columns.Length != spCount) || (endLines != null && endLines.Length != spCount) || (endColumns != null && endColumns.Length != spCount))
                {
                    throw new ArgumentException();
                }

                SymDocumentWriter docwriter = (SymDocumentWriter)document;
                int hr = m_vtable.DefineSequencePoints(m_pWriter, docwriter.GetUnmanaged(), spCount, offsets, lines, columns, endLines, endColumns);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            int ISymbolWriter.OpenScope(int startOffset)
            {
                int ret;
                int hr = m_vtable.OpenScope(m_pWriter, startOffset, out ret);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                return ret;
            }

            void ISymbolWriter.CloseScope(int endOffset)
            {
                int hr = m_vtable.CloseScope(m_pWriter, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.SetScopeRange(int scopeID, int startOffset, int endOffset)
            {
                int hr = m_vtable.SetScopeRange(m_pWriter, scopeID, startOffset, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.DefineLocalVariable(String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset)
            {
                int hr = m_vtable.DefineLocalVariable(m_pWriter, name, (int)attributes, signature.Length, signature, (int)addrKind, addr1, addr2, addr3, startOffset, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.DefineParameter(String name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3)
            {
                throw new NotSupportedException();
            }

            void ISymbolWriter.DefineField(SymbolToken parent, String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3)
            {
                throw new NotSupportedException();
            }

            void ISymbolWriter.DefineGlobalVariable(String name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3)
            {
                throw new NotSupportedException();
            }

            void ISymbolWriter.Close()
            {
                int hr = m_vtable.Close(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.SetSymAttribute(SymbolToken parent, String name, byte[] data)
            {
                int hr = m_vtable.SetSymAttribute(m_pWriter, parent.GetToken(), name, data.Length, data);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.OpenNamespace(String name)
            {
                int hr = m_vtable.OpenNamespace(m_pWriter, name);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.CloseNamespace()
            {
                int hr = m_vtable.CloseNamespace(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.UsingNamespace(String name)
            {
                int hr = m_vtable.UsingNamespace(m_pWriter, name);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            void ISymbolWriter.SetMethodSourceRange(ISymbolDocumentWriter startDoc, int startLine, int startColumn, ISymbolDocumentWriter endDoc, int endLine, int endColumn)
            {
                throw new NotSupportedException();
            }

            void ISymbolWriter.SetUnderlyingWriter(IntPtr ppUnderlyingWriter)
            {
                throw new NotSupportedException();
            }

            internal void InternalSetUnderlyingWriter(IntPtr ppUnderlyingWriter)
            {
                m_pWriter = *((ISymUnmanagedWriter**)ppUnderlyingWriter);
                m_vtable = (ISymUnmanagedWriterVTable)(Marshal.PtrToStructure(m_pWriter->m_unmanagedVTable, typeof (ISymUnmanagedWriterVTable)));
            }

            private delegate int DInitialize(ISymUnmanagedWriter*pthis, IntPtr emitter, [MarshalAs(UnmanagedType.LPWStr)] String filename, IntPtr pIStream, [MarshalAs(UnmanagedType.Bool)] bool fFullBuild);
            private delegate int DDefineDocument(ISymUnmanagedWriter*pthis, [MarshalAs(UnmanagedType.LPWStr)] String url, [In] ref Guid language, [In] ref Guid languageVender, [In] ref Guid documentType, [Out] out PunkSafeHandle ppsymUnmanagedDocumentWriter);
            private delegate int DSetUserEntryPoint(ISymUnmanagedWriter*pthis, int entryMethod);
            private delegate int DOpenMethod(ISymUnmanagedWriter*pthis, int entryMethod);
            private delegate int DCloseMethod(ISymUnmanagedWriter*pthis);
            private delegate int DDefineSequencePoints(ISymUnmanagedWriter*pthis, PunkSafeHandle document, int spCount, [In] int[] offsets, [In] int[] lines, [In] int[] columns, [In] int[] endLines, [In] int[] endColumns);
            private delegate int DOpenScope(ISymUnmanagedWriter*pthis, int startOffset, [Out] out int pretval);
            private delegate int DCloseScope(ISymUnmanagedWriter*pthis, int endOffset);
            private delegate int DSetScopeRange(ISymUnmanagedWriter*pthis, int scopeID, int startOffset, int endOffset);
            private delegate int DDefineLocalVariable(ISymUnmanagedWriter*pthis, [MarshalAs(UnmanagedType.LPWStr)] String name, int attributes, int cSig, [In] byte[] signature, int addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
            private delegate int DClose(ISymUnmanagedWriter*pthis);
            private delegate int DSetSymAttribute(ISymUnmanagedWriter*pthis, int parent, [MarshalAs(UnmanagedType.LPWStr)] String name, int cData, [In] byte[] data);
            private delegate int DOpenNamespace(ISymUnmanagedWriter*pthis, [MarshalAs(UnmanagedType.LPWStr)] String name);
            private delegate int DCloseNamespace(ISymUnmanagedWriter*pthis);
            private delegate int DUsingNamespace(ISymUnmanagedWriter*pthis, [MarshalAs(UnmanagedType.LPWStr)] String name);
            private struct ISymUnmanagedWriterVTable
            {
                internal IntPtr QueryInterface;
                internal IntPtr AddRef;
                internal IntPtr Release;
                internal DDefineDocument DefineDocument;
                internal DSetUserEntryPoint SetUserEntryPoint;
                internal DOpenMethod OpenMethod;
                internal DCloseMethod CloseMethod;
                internal DOpenScope OpenScope;
                internal DCloseScope CloseScope;
                internal DSetScopeRange SetScopeRange;
                internal DDefineLocalVariable DefineLocalVariable;
                internal IntPtr DefineParameter;
                internal IntPtr DefineField;
                internal IntPtr DefineGlobalVariable;
                internal DClose Close;
                internal DSetSymAttribute SetSymAttribute;
                internal DOpenNamespace OpenNamespace;
                internal DCloseNamespace CloseNamespace;
                internal DUsingNamespace UsingNamespace;
                internal IntPtr SetMethodSourceRange;
                internal DInitialize Initialize;
                internal IntPtr GetDebugInfo;
                internal DDefineSequencePoints DefineSequencePoints;
            }

            private struct ISymUnmanagedWriter
            {
                internal IntPtr m_unmanagedVTable;
            }

            private ISymUnmanagedWriter*m_pWriter;
            private ISymUnmanagedWriterVTable m_vtable;
        }
    }

    sealed class PunkSafeHandle : SafeHandle
    {
        internal PunkSafeHandle(): base ((IntPtr)0, true)
        {
        }

        override protected bool ReleaseHandle()
        {
            m_Release(handle);
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get
            {
                return handle == ((IntPtr)0);
            }
        }

        private delegate void DRelease(IntPtr punk);
        private static DRelease m_Release;
        private static extern IntPtr nGetDReleaseTarget();
        static PunkSafeHandle()
        {
            m_Release = (DRelease)(Marshal.GetDelegateForFunctionPointer(nGetDReleaseTarget(), typeof (DRelease)));
            m_Release((IntPtr)0);
        }
    }
}