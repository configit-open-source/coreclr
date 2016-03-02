
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    internal sealed class BeginEndAwaitableAdapter : ICriticalNotifyCompletion
    {
        private readonly static Action CALLBACK_RAN = () =>
        {
        }

        ;
        private IAsyncResult _asyncResult;
        private Action _continuation;
        public readonly static AsyncCallback Callback = (asyncResult) =>
        {
                                                BeginEndAwaitableAdapter adapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
            adapter._asyncResult = asyncResult;
            Action continuation = Interlocked.Exchange(ref adapter._continuation, CALLBACK_RAN);
            if (continuation != null)
            {
                                continuation();
            }
        }

        ;
        public BeginEndAwaitableAdapter GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted
        {
            get
            {
                return (_continuation == CALLBACK_RAN);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
                        OnCompleted(continuation);
        }

        public void OnCompleted(Action continuation)
        {
                        if (_continuation == CALLBACK_RAN || Interlocked.CompareExchange(ref _continuation, continuation, null) == CALLBACK_RAN)
            {
                Task.Run(continuation);
            }
        }

        public IAsyncResult GetResult()
        {
                        IAsyncResult result = _asyncResult;
            _asyncResult = null;
            _continuation = null;
            return result;
        }
    }
}