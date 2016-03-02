using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Loader
{
    public abstract class AssemblyLoadContext
    {
        private static extern bool OverrideDefaultAssemblyLoadContextForCurrentDomain(IntPtr ptrNativeAssemblyLoadContext);
        private static extern bool CanUseAppPathAssemblyLoadContextInCurrentDomain();
        private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext);
        private static extern IntPtr LoadFromStream(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);
        internal static extern void InternalSetProfileRoot(string directoryPath);
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);
        protected AssemblyLoadContext()
        {
            InitializeLoadContext(false);
        }

        internal AssemblyLoadContext(bool fRepresentsTPALoadContext)
        {
            InitializeLoadContext(fRepresentsTPALoadContext);
        }

        void InitializeLoadContext(bool fRepresentsTPALoadContext)
        {
            GCHandle gchALC = GCHandle.Alloc(this);
            IntPtr ptrALC = GCHandle.ToIntPtr(gchALC);
            m_pNativeAssemblyLoadContext = InitializeAssemblyLoadContext(ptrALC, fRepresentsTPALoadContext);
            Resolving = null;
            Unloading = null;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContext, string ilPath, string niPath, ObjectHandleOnStack retAssembly);
        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException("assemblyPath");
            }

            if (Path.IsRelative(assemblyPath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "assemblyPath");
            }

            RuntimeAssembly loadedAssembly = null;
            LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, null, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            return loadedAssembly;
        }

        public Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException("nativeImagePath");
            }

            if (Path.IsRelative(nativeImagePath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "nativeImagePath");
            }

            if (!(nativeImagePath.EndsWith(".ni.dll", StringComparison.InvariantCultureIgnoreCase) || nativeImagePath.EndsWith(".ni.exe", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException("nativeImagePath");
            }

            if (assemblyPath != null && Path.IsRelative(assemblyPath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "assemblyPath");
            }

            RuntimeAssembly loadedAssembly = null;
            LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, nativeImagePath, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            return loadedAssembly;
        }

        public Assembly LoadFromStream(Stream assembly)
        {
            return LoadFromStream(assembly, null);
        }

        public Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            int iAssemblyStreamLength = (int)assembly.Length;
            int iSymbolLength = 0;
            byte[] arrAssembly = new byte[iAssemblyStreamLength];
            assembly.Read(arrAssembly, 0, iAssemblyStreamLength);
            byte[] arrSymbols = null;
            if (assemblySymbols != null)
            {
                iSymbolLength = (int)assemblySymbols.Length;
                arrSymbols = new byte[iSymbolLength];
                assemblySymbols.Read(arrSymbols, 0, iSymbolLength);
            }

            RuntimeAssembly loadedAssembly = null;
            unsafe
            {
                fixed (byte *ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
                {
                    LoadFromStream(m_pNativeAssemblyLoadContext, new IntPtr(ptrAssembly), iAssemblyStreamLength, new IntPtr(ptrSymbols), iSymbolLength, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                }
            }

            return loadedAssembly;
        }

        protected abstract Assembly Load(AssemblyName assemblyName);
        private static Assembly Resolve(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target);
            return context.LoadFromAssemblyName(assemblyName);
        }

        private Assembly GetFirstResolvedAssembly(AssemblyName assemblyName)
        {
            Assembly resolvedAssembly = null;
            Func<AssemblyLoadContext, AssemblyName, Assembly> assemblyResolveHandler = Resolving;
            if (assemblyResolveHandler != null)
            {
                Delegate[] arrSubscribers = assemblyResolveHandler.GetInvocationList();
                for (int i = 0; i < arrSubscribers.Length; i++)
                {
                    resolvedAssembly = ((Func<AssemblyLoadContext, AssemblyName, Assembly>)arrSubscribers[i])(this, assemblyName);
                    if (resolvedAssembly != null)
                    {
                        break;
                    }
                }
            }

            return resolvedAssembly;
        }

        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            string requestedSimpleName = assemblyName.Name;
            Assembly assembly = Load(assemblyName);
            if (assembly == null)
            {
                assembly = GetFirstResolvedAssembly(assemblyName);
            }

            if (assembly == null)
            {
                throw new FileNotFoundException(Environment.GetResourceString("IO.FileLoad"), requestedSimpleName);
            }

            string loadedSimpleName = null;
            RuntimeAssembly rtLoadedAssembly = assembly as RuntimeAssembly;
            if (rtLoadedAssembly != null)
            {
                loadedSimpleName = rtLoadedAssembly.GetSimpleName();
            }

            if (String.IsNullOrEmpty(loadedSimpleName) || (!requestedSimpleName.Equals(loadedSimpleName, StringComparison.InvariantCultureIgnoreCase)))
                throw new InvalidOperationException(Environment.GetResourceString("Argument_CustomAssemblyLoadContextRequestedNameMismatch"));
            return assembly;
        }

        private static extern IntPtr InternalLoadUnmanagedDllFromPath(string unmanagedDllPath);
        protected IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath)
        {
            if (unmanagedDllPath == null)
            {
                throw new ArgumentNullException("unmanagedDllPath");
            }

            if (unmanagedDllPath.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "unmanagedDllPath");
            }

            if (Path.IsRelative(unmanagedDllPath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "unmanagedDllPath");
            }

            return InternalLoadUnmanagedDllFromPath(unmanagedDllPath);
        }

        protected virtual IntPtr LoadUnmanagedDll(String unmanagedDllName)
        {
            return IntPtr.Zero;
        }

        private static IntPtr ResolveUnmanagedDll(String unmanagedDllName, IntPtr gchManagedAssemblyLoadContext)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target);
            return context.LoadUnmanagedDll(unmanagedDllName);
        }

        public static AssemblyLoadContext Default
        {
            get
            {
                if (s_DefaultAssemblyLoadContext == null)
                {
                    if (AssemblyLoadContext.CanUseAppPathAssemblyLoadContextInCurrentDomain())
                    {
                        lock (s_initLock)
                        {
                            if (s_DefaultAssemblyLoadContext == null)
                            {
                                s_DefaultAssemblyLoadContext = new AppPathAssemblyLoadContext();
                            }
                        }
                    }
                }

                return s_DefaultAssemblyLoadContext;
            }
        }

        public static void InitializeDefaultContext(AssemblyLoadContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (!AssemblyLoadContext.OverrideDefaultAssemblyLoadContextForCurrentDomain(context.m_pNativeAssemblyLoadContext))
            {
                throw new InvalidOperationException(Environment.GetResourceString("AppDomain_BindingModelIsLocked"));
            }

            s_DefaultAssemblyLoadContext = context;
        }

        static internal extern AssemblyName nGetFileInformation(String s);
        public static AssemblyName GetAssemblyName(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException("assemblyPath");
            }

            String fullPath = Path.GetFullPathInternal(assemblyPath);
            return nGetFileInformation(fullPath);
        }

        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly assembly);
        public static AssemblyLoadContext GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            AssemblyLoadContext loadContextForAssembly = null;
            IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly((RuntimeAssembly)assembly);
            if (ptrAssemblyLoadContext == IntPtr.Zero)
            {
                loadContextForAssembly = AssemblyLoadContext.Default;
            }
            else
            {
                loadContextForAssembly = (AssemblyLoadContext)(GCHandle.FromIntPtr(ptrAssemblyLoadContext).Target);
            }

            return loadContextForAssembly;
        }

        public void SetProfileOptimizationRoot(string directoryPath)
        {
            InternalSetProfileRoot(directoryPath);
        }

        public void StartProfileOptimization(string profile)
        {
            InternalStartProfile(profile, m_pNativeAssemblyLoadContext);
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            var unloading = Unloading;
            if (unloading != null)
            {
                unloading(this);
            }
        }

        public event Func<AssemblyLoadContext, AssemblyName, Assembly> Resolving;
        public event Action<AssemblyLoadContext> Unloading;
        private IntPtr m_pNativeAssemblyLoadContext;
        private static volatile AssemblyLoadContext s_DefaultAssemblyLoadContext;
        private static readonly object s_initLock = new Object();
    }

    class AppPathAssemblyLoadContext : AssemblyLoadContext
    {
        internal AppPathAssemblyLoadContext(): base (true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}