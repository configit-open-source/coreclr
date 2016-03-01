using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Collections.Generic;

namespace System.Reflection
{
    internal sealed class LoaderAllocatorScout
    {
        internal IntPtr m_nativeLoaderAllocator;
        private static extern bool Destroy(IntPtr nativeLoaderAllocator);
        ~LoaderAllocatorScout()
        {
            if (m_nativeLoaderAllocator.IsNull())
                return;
            if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                if (!Destroy(m_nativeLoaderAllocator))
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }
    }

    internal sealed class LoaderAllocator
    {
        LoaderAllocator()
        {
            m_slots = new object[5];
            m_scout = new LoaderAllocatorScout();
        }

        LoaderAllocatorScout m_scout;
        object[] m_slots;
        internal CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> m_methodInstantiations;
        int m_slotsUsed;
    }
}