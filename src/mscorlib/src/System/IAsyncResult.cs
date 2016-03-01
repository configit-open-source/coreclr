using System.Threading;

namespace System
{
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