namespace System.Reflection.Emit
{
    using System;
    using IList = System.Collections.IList;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics;
    using CultureInfo = System.Globalization.CultureInfo;
    using IResourceWriter = System.Resources.IResourceWriter;
    using System.IO;
    using System.Runtime.Versioning;
    using System.Diagnostics.SymbolStore;
    using System.Diagnostics.Contracts;

    internal class AssemblyBuilderData
    {
        internal AssemblyBuilderData(InternalAssemblyBuilder assembly, String strAssemblyName, AssemblyBuilderAccess access, String dir)
        {
            m_assembly = assembly;
            m_strAssemblyName = strAssemblyName;
            m_access = access;
            m_moduleBuilderList = new List<ModuleBuilder>();
            m_resWriterList = new List<ResWriterData>();
            if (dir == null && access != AssemblyBuilderAccess.Run)
                m_strDir = Environment.CurrentDirectory;
            else
                m_strDir = dir;
            m_peFileKind = PEFileKinds.Dll;
        }

        internal void AddModule(ModuleBuilder dynModule)
        {
            m_moduleBuilderList.Add(dynModule);
        }

        internal void AddResWriter(ResWriterData resData)
        {
            m_resWriterList.Add(resData);
        }

        internal void AddCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (m_CABuilders == null)
            {
                m_CABuilders = new CustomAttributeBuilder[m_iInitialSize];
            }

            if (m_iCABuilder == m_CABuilders.Length)
            {
                CustomAttributeBuilder[] tempCABuilders = new CustomAttributeBuilder[m_iCABuilder * 2];
                Array.Copy(m_CABuilders, tempCABuilders, m_iCABuilder);
                m_CABuilders = tempCABuilders;
            }

            m_CABuilders[m_iCABuilder] = customBuilder;
            m_iCABuilder++;
        }

        internal void AddCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (m_CABytes == null)
            {
                m_CABytes = new byte[m_iInitialSize][];
                m_CACons = new ConstructorInfo[m_iInitialSize];
            }

            if (m_iCAs == m_CABytes.Length)
            {
                byte[][] temp = new byte[m_iCAs * 2][];
                ConstructorInfo[] tempCon = new ConstructorInfo[m_iCAs * 2];
                for (int i = 0; i < m_iCAs; i++)
                {
                    temp[i] = m_CABytes[i];
                    tempCon[i] = m_CACons[i];
                }

                m_CABytes = temp;
                m_CACons = tempCon;
            }

            byte[] attrs = new byte[binaryAttribute.Length];
            Array.Copy(binaryAttribute, attrs, binaryAttribute.Length);
            m_CABytes[m_iCAs] = attrs;
            m_CACons[m_iCAs] = con;
            m_iCAs++;
        }

        internal void FillUnmanagedVersionInfo()
        {
            CultureInfo locale = m_assembly.GetLocale();
            for (int i = 0; i < m_iCABuilder; i++)
            {
                Type conType = m_CABuilders[i].m_con.DeclaringType;
                if (m_CABuilders[i].m_constructorArgs.Length == 0 || m_CABuilders[i].m_constructorArgs[0] == null)
                    continue;
                if (conType.Equals(typeof (System.Reflection.AssemblyCopyrightAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strCopyright = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyTrademarkAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strTrademark = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyProductAttribute)))
                {
                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strProduct = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyCompanyAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strCompany = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyDescriptionAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    m_nativeVersion.m_strDescription = m_CABuilders[i].m_constructorArgs[0].ToString();
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyTitleAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    m_nativeVersion.m_strTitle = m_CABuilders[i].m_constructorArgs[0].ToString();
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyInformationalVersionAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strProductVersion = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyCultureAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    CultureInfo culture = new CultureInfo(m_CABuilders[i].m_constructorArgs[0].ToString());
                }
                else if (conType.Equals(typeof (System.Reflection.AssemblyFileVersionAttribute)))
                {
                    if (m_CABuilders[i].m_constructorArgs.Length != 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadCAForUnmngRSC", m_CABuilders[i].m_con.ReflectedType.Name));
                    }

                    if (m_OverrideUnmanagedVersionInfo == false)
                    {
                        m_nativeVersion.m_strFileVersion = m_CABuilders[i].m_constructorArgs[0].ToString();
                    }
                }
            }
        }

        internal void CheckResNameConflict(String strNewResName)
        {
            int size = m_resWriterList.Count;
            int i;
            for (i = 0; i < size; i++)
            {
                ResWriterData resWriter = m_resWriterList[i];
                if (resWriter.m_strName.Equals(strNewResName))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateResourceName"));
                }
            }
        }

        internal void CheckNameConflict(String strNewModuleName)
        {
            int size = m_moduleBuilderList.Count;
            int i;
            for (i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName.Equals(strNewModuleName))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateModuleName"));
                }
            }
        }

        internal void CheckTypeNameConflict(String strTypeName, TypeBuilder enclosingType)
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilderData.CheckTypeNameConflict( " + strTypeName + " )");
            for (int i = 0; i < m_moduleBuilderList.Count; i++)
            {
                ModuleBuilder curModule = m_moduleBuilderList[i];
                curModule.CheckTypeNameConflict(strTypeName, enclosingType);
            }
        }

        internal void CheckFileNameConflict(String strFileName)
        {
            int size = m_moduleBuilderList.Count;
            int i;
            for (i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strFileName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_DuplicatedFileName"));
                    }
                }
            }

            size = m_resWriterList.Count;
            for (i = 0; i < size; i++)
            {
                ResWriterData resWriter = m_resWriterList[i];
                if (resWriter.m_strFileName != null)
                {
                    if (String.Compare(resWriter.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_DuplicatedFileName"));
                    }
                }
            }
        }

        internal ModuleBuilder FindModuleWithFileName(String strFileName)
        {
            int size = m_moduleBuilderList.Count;
            int i;
            for (i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strFileName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strFileName, strFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return moduleBuilder;
                    }
                }
            }

            return null;
        }

        internal ModuleBuilder FindModuleWithName(String strName)
        {
            int size = m_moduleBuilderList.Count;
            int i;
            for (i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName != null)
                {
                    if (String.Compare(moduleBuilder.m_moduleData.m_strModuleName, strName, StringComparison.OrdinalIgnoreCase) == 0)
                        return moduleBuilder;
                }
            }

            return null;
        }

        internal void AddPublicComType(Type type)
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilderData.AddPublicComType( " + type.FullName + " )");
            if (m_isSaved == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotAlterAssembly"));
            }

            EnsurePublicComTypeCapacity();
            m_publicComTypeList[m_iPublicComTypeCount] = type;
            m_iPublicComTypeCount++;
        }

        internal void AddPermissionRequests(PermissionSet required, PermissionSet optional, PermissionSet refused)
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilderData.AddPermissionRequests");
            if (m_isSaved == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotAlterAssembly"));
            }

            m_RequiredPset = required;
            m_OptionalPset = optional;
            m_RefusedPset = refused;
        }

        private void EnsurePublicComTypeCapacity()
        {
            if (m_publicComTypeList == null)
            {
                m_publicComTypeList = new Type[m_iInitialSize];
            }

            if (m_iPublicComTypeCount == m_publicComTypeList.Length)
            {
                Type[] tempTypeList = new Type[m_iPublicComTypeCount * 2];
                Array.Copy(m_publicComTypeList, tempTypeList, m_iPublicComTypeCount);
                m_publicComTypeList = tempTypeList;
            }
        }

        internal List<ModuleBuilder> m_moduleBuilderList;
        internal List<ResWriterData> m_resWriterList;
        internal String m_strAssemblyName;
        internal AssemblyBuilderAccess m_access;
        private InternalAssemblyBuilder m_assembly;
        internal Type[] m_publicComTypeList;
        internal int m_iPublicComTypeCount;
        internal bool m_isSaved;
        internal const int m_iInitialSize = 16;
        internal String m_strDir;
        internal const int m_tkAssembly = 0x20000001;
        internal PermissionSet m_RequiredPset;
        internal PermissionSet m_OptionalPset;
        internal PermissionSet m_RefusedPset;
        internal CustomAttributeBuilder[] m_CABuilders;
        internal int m_iCABuilder;
        internal byte[][] m_CABytes;
        internal ConstructorInfo[] m_CACons;
        internal int m_iCAs;
        internal PEFileKinds m_peFileKind;
        internal MethodInfo m_entryPointMethod;
        internal Assembly m_ISymWrapperAssembly;
        internal String m_strResourceFileName;
        internal byte[] m_resourceBytes;
        internal NativeVersionInfo m_nativeVersion;
        internal bool m_hasUnmanagedVersionInfo;
        internal bool m_OverrideUnmanagedVersionInfo;
    }

    internal class ResWriterData
    {
        internal ResWriterData(IResourceWriter resWriter, Stream memoryStream, String strName, String strFileName, String strFullFileName, ResourceAttributes attribute)
        {
            m_resWriter = resWriter;
            m_memoryStream = memoryStream;
            m_strName = strName;
            m_strFileName = strFileName;
            m_strFullFileName = strFullFileName;
            m_nextResWriter = null;
            m_attribute = attribute;
        }

        internal IResourceWriter m_resWriter;
        internal String m_strName;
        internal String m_strFileName;
        internal String m_strFullFileName;
        internal Stream m_memoryStream;
        internal ResWriterData m_nextResWriter;
        internal ResourceAttributes m_attribute;
    }

    internal class NativeVersionInfo
    {
        internal NativeVersionInfo()
        {
            m_strDescription = null;
            m_strCompany = null;
            m_strTitle = null;
            m_strCopyright = null;
            m_strTrademark = null;
            m_strProduct = null;
            m_strProductVersion = null;
            m_strFileVersion = null;
            m_lcid = -1;
        }

        internal String m_strDescription;
        internal String m_strCompany;
        internal String m_strTitle;
        internal String m_strCopyright;
        internal String m_strTrademark;
        internal String m_strProduct;
        internal String m_strProductVersion;
        internal String m_strFileVersion;
        internal int m_lcid;
    }
}