namespace System.Threading
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    public sealed class ManualResetEvent : EventWaitHandle
    {
        public ManualResetEvent(bool initialState): base (initialState, EventResetMode.ManualReset)
        {
        }
    }
}