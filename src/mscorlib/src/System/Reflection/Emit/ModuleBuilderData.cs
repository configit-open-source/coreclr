using System.Diagnostics.Contracts;
using System.IO;

namespace System.Reflection.Emit
{
    internal class ModuleBuilderData
    {
        internal ModuleBuilderData(ModuleBuilder module, String strModuleName, String strFileName, int tkFile)
        {
            m_globalTypeBuilder = new TypeBuilder(module);
            m_module = module;
            m_tkFile = tkFile;
            InitNames(strModuleName, strFileName);
        }

        private void InitNames(String strModuleName, String strFileName)
        {
            m_strModuleName = strModuleName;
            if (strFileName == null)
            {
                m_strFileName = strModuleName;
            }
            else
            {
                String strExtension = Path.GetExtension(strFileName);
                if (strExtension == null || strExtension == String.Empty)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoModuleFileExtension", strFileName));
                }

                m_strFileName = strFileName;
            }
        }

        internal virtual void ModifyModuleName(String strModuleName)
        {
            Contract.Assert(m_strModuleName == AssemblyBuilder.MANIFEST_MODULE_NAME, "Changing names for non-manifest module");
            InitNames(strModuleName, null);
        }

        internal int FileToken
        {
            get
            {
                return m_tkFile;
            }

            set
            {
                m_tkFile = value;
            }
        }

        internal String m_strModuleName;
        internal String m_strFileName;
        internal bool m_fGlobalBeenCreated;
        internal bool m_fHasGlobal;
        internal TypeBuilder m_globalTypeBuilder;
        internal ModuleBuilder m_module;
        private int m_tkFile;
        internal bool m_isSaved;
        internal ResWriterData m_embeddedRes;
        internal const String MULTI_BYTE_VALUE_CLASS = "$ArrayType$";
        internal String m_strResourceFileName;
        internal byte[] m_resourceBytes;
    }
}