using System.Diagnostics.Contracts;
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
            Contract.Assert(asyncResult != null);
            Contract.Assert(asyncResult.IsCompleted);
            Contract.Assert(asyncResult.AsyncState is BeginEndAwaitableAdapter);
            BeginEndAwaitableAdapter adapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
            adapter._asyncResult = asyncResult;
            Action continuation = Interlocked.Exchange(ref adapter._continuation, CALLBACK_RAN);
            if (continuation != null)
            {
                Contract.Assert(continuation != CALLBACK_RAN);
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
            Contract.Assert(continuation != null);
            OnCompleted(continuation);
        }

        public void OnCompleted(Action continuation)
        {
            Contract.Assert(continuation != null);
            if (_continuation == CALLBACK_RAN || Interlocked.CompareExchange(ref _continuation, continuation, null) == CALLBACK_RAN)
            {
                Task.Run(continuation);
            }
        }

        public IAsyncResult GetResult()
        {
            Contract.Assert(_asyncResult != null && _asyncResult.IsCompleted);
            IAsyncResult result = _asyncResult;
            _asyncResult = null;
            _continuation = null;
            return result;
        }
    }
}