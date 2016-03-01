using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace System.Runtime.ConstrainedExecution
{
    public abstract class CriticalFinalizerObject
    {
        protected CriticalFinalizerObject()
        {
        }

        ~CriticalFinalizerObject()
        {
        }
    }
}