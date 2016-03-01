namespace System.Threading
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    public sealed class AutoResetEvent : EventWaitHandle
    {
        public AutoResetEvent(bool initialState): base (initialState, EventResetMode.AutoReset)
        {
        }
    }
}