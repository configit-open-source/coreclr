using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public sealed class LocalBuilder : LocalVariableInfo, _LocalBuilder
    {
        private int m_localIndex;
        private Type m_localType;
        private MethodInfo m_methodBuilder;
        private bool m_isPinned;
        private LocalBuilder()
        {
        }

        internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder): this (localIndex, localType, methodBuilder, false)
        {
        }

        internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
        {
            m_isPinned = isPinned;
            m_localIndex = localIndex;
            m_localType = localType;
            m_methodBuilder = methodBuilder;
        }

        internal int GetLocalIndex()
        {
            return m_localIndex;
        }

        internal MethodInfo GetMethodBuilder()
        {
            return m_methodBuilder;
        }

        public override bool IsPinned
        {
            get
            {
                return m_isPinned;
            }
        }

        public override Type LocalType
        {
            get
            {
                return m_localType;
            }
        }

        public override int LocalIndex
        {
            get
            {
                return m_localIndex;
            }
        }

        public void SetLocalSymInfo(String name)
        {
            SetLocalSymInfo(name, 0, 0);
        }

        public void SetLocalSymInfo(String name, int startOffset, int endOffset)
        {
            ModuleBuilder dynMod;
            SignatureHelper sigHelp;
            int sigLength;
            byte[] signature;
            byte[] mungedSig;
            int index;
            MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();
            dynMod = (ModuleBuilder)methodBuilder.Module;
            if (methodBuilder.IsTypeCreated())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
            }

            if (dynMod.GetSymWriter() == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            sigHelp = SignatureHelper.GetFieldSigHelper(dynMod);
            sigHelp.AddArgument(m_localType);
            signature = sigHelp.InternalGetSignature(out sigLength);
            mungedSig = new byte[sigLength - 1];
            Array.Copy(signature, 1, mungedSig, 0, sigLength - 1);
            index = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
            if (index == -1)
            {
                methodBuilder.m_localSymInfo.AddLocalSymInfo(name, mungedSig, m_localIndex, startOffset, endOffset);
            }
            else
            {
                methodBuilder.GetILGenerator().m_ScopeTree.AddLocalSymInfoToCurrentScope(name, mungedSig, m_localIndex, startOffset, endOffset);
            }
        }
    }
}