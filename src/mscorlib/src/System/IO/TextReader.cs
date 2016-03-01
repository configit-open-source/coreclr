using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract class TextReader : IDisposable
    {
        public static readonly TextReader Null = new NullTextReader();
        protected TextReader()
        {
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public virtual int Peek()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            return -1;
        }

        public virtual int Read()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            return -1;
        }

        public virtual int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= Contract.OldValue(count));
            Contract.EndContractBlock();
            int n = 0;
            do
            {
                int ch = Read();
                if (ch == -1)
                    break;
                buffer[index + n++] = (char)ch;
            }
            while (n < count);
            return n;
        }

        public virtual String ReadToEnd()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while ((len = Read(chars, 0, chars.Length)) != 0)
            {
                sb.Append(chars, 0, len);
            }

            return sb.ToString();
        }

        public virtual int ReadBlock([In, Out] char[] buffer, int index, int count)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            int i, n = 0;
            do
            {
                n += (i = Read(buffer, index + n, count - n));
            }
            while (i > 0 && n < count);
            return n;
        }

        public virtual String ReadLine()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int ch = Read();
                if (ch == -1)
                    break;
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && Peek() == '\n')
                        Read();
                    return sb.ToString();
                }

                sb.Append((char)ch);
            }

            if (sb.Length > 0)
                return sb.ToString();
            return null;
        }

        public virtual Task<String> ReadLineAsync()
        {
            return Task<String>.Factory.StartNew(state =>
            {
                return ((TextReader)state).ReadLine();
            }

            , this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public async virtual Task<String> ReadToEndAsync()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while ((len = await ReadAsyncInternal(chars, 0, chars.Length).ConfigureAwait(false)) != 0)
            {
                sb.Append(chars, 0, len);
            }

            return sb.ToString();
        }

        public virtual Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            return ReadAsyncInternal(buffer, index, count);
        }

        internal virtual Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - index >= count);
            var tuple = new Tuple<TextReader, char[], int, int>(this, buffer, index, count);
            return Task<int>.Factory.StartNew(state =>
            {
                var t = (Tuple<TextReader, char[], int, int>)state;
                return t.Item1.Read(t.Item2, t.Item3, t.Item4);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            return ReadBlockAsyncInternal(buffer, index, count);
        }

        private async Task<int> ReadBlockAsyncInternal(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - index >= count);
            int i, n = 0;
            do
            {
                i = await ReadAsyncInternal(buffer, index + n, count - n).ConfigureAwait(false);
                n += i;
            }
            while (i > 0 && n < count);
            return n;
        }

        public static TextReader Synchronized(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            Contract.Ensures(Contract.Result<TextReader>() != null);
            Contract.EndContractBlock();
            if (reader is SyncTextReader)
                return reader;
            return new SyncTextReader(reader);
        }

        private sealed class NullTextReader : TextReader
        {
            public NullTextReader()
            {
            }

            public override int Read(char[] buffer, int index, int count)
            {
                return 0;
            }

            public override String ReadLine()
            {
                return null;
            }
        }

        internal sealed class SyncTextReader : TextReader
        {
            internal TextReader _in;
            internal SyncTextReader(TextReader t)
            {
                _in = t;
            }

            public override void Close()
            {
                _in.Close();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    ((IDisposable)_in).Dispose();
            }

            public override int Peek()
            {
                return _in.Peek();
            }

            public override int Read()
            {
                return _in.Read();
            }

            public override int Read([In, Out] char[] buffer, int index, int count)
            {
                return _in.Read(buffer, index, count);
            }

            public override int ReadBlock([In, Out] char[] buffer, int index, int count)
            {
                return _in.ReadBlock(buffer, index, count);
            }

            public override String ReadLine()
            {
                return _in.ReadLine();
            }

            public override String ReadToEnd()
            {
                return _in.ReadToEnd();
            }

            public override Task<String> ReadLineAsync()
            {
                return Task.FromResult(ReadLine());
            }

            public override Task<String> ReadToEndAsync()
            {
                return Task.FromResult(ReadToEnd());
            }

            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (buffer.Length - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();
                return Task.FromResult(ReadBlock(buffer, index, count));
            }

            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (buffer.Length - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();
                return Task.FromResult(Read(buffer, index, count));
            }
        }
    }
}