namespace System
{
    using System;
    using System.Threading;

    public interface IAsyncResult
    {
        bool IsCompleted
        {
            get;
        }

        WaitHandle AsyncWaitHandle
        {
            get;
        }

        Object AsyncState
        {
            get;
        }

        bool CompletedSynchronously
        {
            get;
        }
    }
}