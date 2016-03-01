namespace System.Threading
{
    using Microsoft.Win32.SafeHandles;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime;
    using System.Runtime.Versioning;
    using System.Runtime.ConstrainedExecution;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    internal class WinRTSynchronizationContextFactoryBase
    {
        public virtual SynchronizationContext Create(object coreDispatcher)
        {
            return null;
        }
    }

    public class SynchronizationContext
    {
        public SynchronizationContext()
        {
        }

        public virtual void Send(SendOrPostCallback d, Object state)
        {
            d(state);
        }

        public virtual void Post(SendOrPostCallback d, Object state)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(d), state);
        }

        public virtual void OperationStarted()
        {
        }

        public virtual void OperationCompleted()
        {
        }

        private static SynchronizationContext s_threadStaticContext;
        private static SynchronizationContext s_appDomainStaticContext;
        public static void SetSynchronizationContext(SynchronizationContext syncContext)
        {
            s_threadStaticContext = syncContext;
        }

        public static void SetThreadStaticContext(SynchronizationContext syncContext)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhoneMango)
                s_appDomainStaticContext = syncContext;
            else
                s_threadStaticContext = syncContext;
        }

        public static SynchronizationContext Current
        {
            get
            {
                SynchronizationContext context = null;
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhoneMango)
                    context = s_appDomainStaticContext;
                else
                    context = s_threadStaticContext;
                if (context == null && Environment.IsWinRTSupported)
                    context = GetWinRTContext();
                return context;
            }
        }

        internal static SynchronizationContext CurrentNoFlow
        {
            [FriendAccessAllowed]
            get
            {
                return Current;
            }
        }

        private static SynchronizationContext GetWinRTContext()
        {
            Contract.Assert(Environment.IsWinRTSupported);
            if (!AppDomain.IsAppXModel())
                return null;
            object dispatcher = GetWinRTDispatcherForCurrentThread();
            if (dispatcher != null)
                return GetWinRTSynchronizationContextFactory().Create(dispatcher);
            return null;
        }

        static WinRTSynchronizationContextFactoryBase s_winRTContextFactory;
        private static WinRTSynchronizationContextFactoryBase GetWinRTSynchronizationContextFactory()
        {
            WinRTSynchronizationContextFactoryBase factory = s_winRTContextFactory;
            if (factory == null)
            {
                Type factoryType = Type.GetType("System.Threading.WinRTSynchronizationContextFactory, " + AssemblyRef.SystemRuntimeWindowsRuntime, true);
                s_winRTContextFactory = factory = (WinRTSynchronizationContextFactoryBase)Activator.CreateInstance(factoryType, true);
            }

            return factory;
        }

        private static extern object GetWinRTDispatcherForCurrentThread();
        public virtual SynchronizationContext CreateCopy()
        {
            return new SynchronizationContext();
        }
    }
}