
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IClosable
    {
        void Close();
    }

    internal sealed class IDisposableToIClosableAdapter
    {
        private IDisposableToIClosableAdapter()
        {
                    }

        public void Close()
        {
            IDisposable _this = JitHelpers.UnsafeCast<IDisposable>(this);
            _this.Dispose();
        }
    }

    internal sealed class IClosableToIDisposableAdapter
    {
        private IClosableToIDisposableAdapter()
        {
                    }

        private void Dispose()
        {
            IClosable _this = JitHelpers.UnsafeCast<IClosable>(this);
            _this.Close();
        }
    }
}