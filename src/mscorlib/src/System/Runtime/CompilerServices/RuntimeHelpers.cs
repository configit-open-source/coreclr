namespace System.Runtime.CompilerServices
{
    using System;
    using System.Security;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public static class RuntimeHelpers
    {
        public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);
        public static extern Object GetObjectValue(Object obj);
        private static extern void _RunClassConstructor(RuntimeType type);
        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            _RunClassConstructor(type.GetRuntimeType());
        }

        private static extern void _RunModuleConstructor(System.Reflection.RuntimeModule module);
        public static void RunModuleConstructor(ModuleHandle module)
        {
            _RunModuleConstructor(module.GetRuntimeModule());
        }

        private static unsafe extern void _PrepareMethod(IRuntimeMethodInfo method, IntPtr*pInstantiation, int cInstantiation);
        internal static extern void _CompileMethod(IRuntimeMethodInfo method);
        public static void PrepareMethod(RuntimeMethodHandle method)
        {
            unsafe
            {
                _PrepareMethod(method.GetMethodInfo(), null, 0);
            }
        }

        public static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[] instantiation)
        {
            unsafe
            {
                int length;
                IntPtr[] instantiationHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out length);
                fixed (IntPtr*pInstantiation = instantiationHandles)
                {
                    _PrepareMethod(method.GetMethodInfo(), pInstantiation, length);
                    GC.KeepAlive(instantiation);
                }
            }
        }

        public static extern void PrepareDelegate(Delegate d);
        public static extern void PrepareContractedDelegate(Delegate d);
        public static extern int GetHashCode(Object o);
        public new static extern bool Equals(Object o1, Object o2);
        public static int OffsetToStringData
        {
            [System.Runtime.Versioning.NonVersionable]
            get
            {
                return 12;
            }
        }

        public static extern void EnsureSufficientExecutionStack();
        internal static extern bool TryEnsureSufficientExecutionStack();
        public static extern void ProbeForSufficientStack();
        public static void PrepareConstrainedRegions()
        {
            ProbeForSufficientStack();
        }

        public static void PrepareConstrainedRegionsNoOP()
        {
        }

        public delegate void TryCode(Object userData);
        public delegate void CleanupCode(Object userData, bool exceptionThrown);
        public static extern void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, Object userData);
        internal static void ExecuteBackoutCodeHelper(Object backoutCode, Object userData, bool exceptionThrown)
        {
            ((CleanupCode)backoutCode)(userData, exceptionThrown);
        }
    }
}