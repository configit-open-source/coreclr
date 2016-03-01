namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;

    public static class WindowsRuntimeMetadata
    {
        public static IEnumerable<string> ResolveNamespace(string namespaceName, IEnumerable<string> packageGraphFilePaths)
        {
            return ResolveNamespace(namespaceName, null, packageGraphFilePaths);
        }

        public static IEnumerable<string> ResolveNamespace(string namespaceName, string windowsSdkFilePath, IEnumerable<string> packageGraphFilePaths)
        {
            if (namespaceName == null)
                throw new ArgumentNullException("namespaceName");
            Contract.EndContractBlock();
            string[] packageGraphFilePathsArray = null;
            if (packageGraphFilePaths != null)
            {
                List<string> packageGraphFilePathsList = new List<string>(packageGraphFilePaths);
                packageGraphFilePathsArray = new string[packageGraphFilePathsList.Count];
                int index = 0;
                foreach (string packageGraphFilePath in packageGraphFilePathsList)
                {
                    packageGraphFilePathsArray[index] = packageGraphFilePath;
                    index++;
                }
            }

            string[] retFileNames = null;
            nResolveNamespace(namespaceName, windowsSdkFilePath, packageGraphFilePathsArray, ((packageGraphFilePathsArray == null) ? 0 : packageGraphFilePathsArray.Length), JitHelpers.GetObjectHandleOnStack(ref retFileNames));
            return retFileNames;
        }

        private extern static void nResolveNamespace(string namespaceName, string windowsSdkFilePath, string[] packageGraphFilePaths, int cPackageGraphFilePaths, ObjectHandleOnStack retFileNames);
        public static event EventHandler<DesignerNamespaceResolveEventArgs> DesignerNamespaceResolve;
        internal static string[] OnDesignerNamespaceResolveEvent(AppDomain appDomain, string namespaceName)
        {
            EventHandler<DesignerNamespaceResolveEventArgs> eventHandler = DesignerNamespaceResolve;
            if (eventHandler != null)
            {
                Delegate[] ds = eventHandler.GetInvocationList();
                int len = ds.Length;
                for (int i = 0; i < len; i++)
                {
                    DesignerNamespaceResolveEventArgs eventArgs = new DesignerNamespaceResolveEventArgs(namespaceName);
                    ((EventHandler<DesignerNamespaceResolveEventArgs>)ds[i])(appDomain, eventArgs);
                    Collection<string> assemblyFilesCollection = eventArgs.ResolvedAssemblyFiles;
                    if (assemblyFilesCollection.Count > 0)
                    {
                        string[] retAssemblyFiles = new string[assemblyFilesCollection.Count];
                        int retIndex = 0;
                        foreach (string assemblyFile in assemblyFilesCollection)
                        {
                            if (String.IsNullOrEmpty(assemblyFile))
                            {
                                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullString"), "DesignerNamespaceResolveEventArgs.ResolvedAssemblyFiles");
                            }

                            retAssemblyFiles[retIndex] = assemblyFile;
                            retIndex++;
                        }

                        return retAssemblyFiles;
                    }
                }
            }

            return null;
        }
    }

    public class DesignerNamespaceResolveEventArgs : EventArgs
    {
        private string _NamespaceName;
        private Collection<string> _ResolvedAssemblyFiles;
        public string NamespaceName
        {
            get
            {
                return _NamespaceName;
            }
        }

        public Collection<string> ResolvedAssemblyFiles
        {
            get
            {
                return _ResolvedAssemblyFiles;
            }
        }

        public DesignerNamespaceResolveEventArgs(string namespaceName)
        {
            _NamespaceName = namespaceName;
            _ResolvedAssemblyFiles = new Collection<string>();
        }
    }
}