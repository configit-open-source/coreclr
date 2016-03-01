using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract class TextWriter : IDisposable
    {
        public static readonly TextWriter Null = new NullTextWriter();
        private const String InitialNewLine = "\r\n";
        protected char[] CoreNewLine = new char[]{'\r', '\n'};
        private IFormatProvider InternalFormatProvider;
        protected TextWriter()
        {
            InternalFormatProvider = null;
        }

        protected TextWriter(IFormatProvider formatProvider)
        {
            InternalFormatProvider = formatProvider;
        }

        public virtual IFormatProvider FormatProvider
        {
            get
            {
                if (InternalFormatProvider == null)
                    return Thread.CurrentThread.CurrentCulture;
                else
                    return InternalFormatProvider;
            }
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Flush()
        {
        }

        public abstract Encoding Encoding
        {
            get;
        }

        public virtual String NewLine
        {
            get
            {
                return new String(CoreNewLine);
            }

            set
            {
                if (value == null)
                    value = InitialNewLine;
                CoreNewLine = value.ToCharArray();
            }
        }

        public static TextWriter Synchronized(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            Contract.Ensures(Contract.Result<TextWriter>() != null);
            Contract.EndContractBlock();
            if (writer is SyncTextWriter)
                return writer;
            return new SyncTextWriter(writer);
        }

        public virtual void Write(char value)
        {
        }

        public virtual void Write(char[] buffer)
        {
            if (buffer != null)
                Write(buffer, 0, buffer.Length);
        }

        public virtual void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            for (int i = 0; i < count; i++)
                Write(buffer[index + i]);
        }

        public virtual void Write(bool value)
        {
            Write(value ? Boolean.TrueLiteral : Boolean.FalseLiteral);
        }

        public virtual void Write(int value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(uint value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(long value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(ulong value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(float value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(double value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(Decimal value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(String value)
        {
            if (value != null)
                Write(value.ToCharArray());
        }

        public virtual void Write(Object value)
        {
            if (value != null)
            {
                IFormattable f = value as IFormattable;
                if (f != null)
                    Write(f.ToString(null, FormatProvider));
                else
                    Write(value.ToString());
            }
        }

        public virtual void Write(String format, Object arg0)
        {
            Write(String.Format(FormatProvider, format, arg0));
        }

        public virtual void Write(String format, Object arg0, Object arg1)
        {
            Write(String.Format(FormatProvider, format, arg0, arg1));
        }

        public virtual void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            Write(String.Format(FormatProvider, format, arg0, arg1, arg2));
        }

        public virtual void Write(String format, params Object[] arg)
        {
            Write(String.Format(FormatProvider, format, arg));
        }

        public virtual void WriteLine()
        {
            Write(CoreNewLine);
        }

        public virtual void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(char[] buffer)
        {
            Write(buffer);
            WriteLine();
        }

        public virtual void WriteLine(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            WriteLine();
        }

        public virtual void WriteLine(bool value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(int value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(uint value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(long value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(ulong value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(float value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(double value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(decimal value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(String value)
        {
            if (value == null)
            {
                WriteLine();
            }
            else
            {
                int vLen = value.Length;
                int nlLen = CoreNewLine.Length;
                char[] chars = new char[vLen + nlLen];
                value.CopyTo(0, chars, 0, vLen);
                if (nlLen == 2)
                {
                    chars[vLen] = CoreNewLine[0];
                    chars[vLen + 1] = CoreNewLine[1];
                }
                else if (nlLen == 1)
                    chars[vLen] = CoreNewLine[0];
                else
                    Buffer.InternalBlockCopy(CoreNewLine, 0, chars, vLen * 2, nlLen * 2);
                Write(chars, 0, vLen + nlLen);
            }
        }

        public virtual void WriteLine(Object value)
        {
            if (value == null)
            {
                WriteLine();
            }
            else
            {
                IFormattable f = value as IFormattable;
                if (f != null)
                    WriteLine(f.ToString(null, FormatProvider));
                else
                    WriteLine(value.ToString());
            }
        }

        public virtual void WriteLine(String format, Object arg0)
        {
            WriteLine(String.Format(FormatProvider, format, arg0));
        }

        public virtual void WriteLine(String format, Object arg0, Object arg1)
        {
            WriteLine(String.Format(FormatProvider, format, arg0, arg1));
        }

        public virtual void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            WriteLine(String.Format(FormatProvider, format, arg0, arg1, arg2));
        }

        public virtual void WriteLine(String format, params Object[] arg)
        {
            WriteLine(String.Format(FormatProvider, format, arg));
        }

        public virtual Task WriteAsync(char value)
        {
            var tuple = new Tuple<TextWriter, char>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char>)state;
                t.Item1.Write(t.Item2);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteAsync(String value)
        {
            var tuple = new Tuple<TextWriter, string>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, string>)state;
                t.Item1.Write(t.Item2);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task WriteAsync(char[] buffer)
        {
            if (buffer == null)
                return Task.CompletedTask;
            return WriteAsync(buffer, 0, buffer.Length);
        }

        public virtual Task WriteAsync(char[] buffer, int index, int count)
        {
            var tuple = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char[], int, int>)state;
                t.Item1.Write(t.Item2, t.Item3, t.Item4);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync(char value)
        {
            var tuple = new Tuple<TextWriter, char>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char>)state;
                t.Item1.WriteLine(t.Item2);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync(String value)
        {
            var tuple = new Tuple<TextWriter, string>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, string>)state;
                t.Item1.WriteLine(t.Item2);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task WriteLineAsync(char[] buffer)
        {
            if (buffer == null)
                return Task.CompletedTask;
            return WriteLineAsync(buffer, 0, buffer.Length);
        }

        public virtual Task WriteLineAsync(char[] buffer, int index, int count)
        {
            var tuple = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char[], int, int>)state;
                t.Item1.WriteLine(t.Item2, t.Item3, t.Item4);
            }

            , tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync()
        {
            return WriteAsync(CoreNewLine);
        }

        public virtual Task FlushAsync()
        {
            return Task.Factory.StartNew(state =>
            {
                ((TextWriter)state).Flush();
            }

            , this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private sealed class NullTextWriter : TextWriter
        {
            internal NullTextWriter(): base (CultureInfo.InvariantCulture)
            {
            }

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.Default;
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
            }

            public override void Write(String value)
            {
            }

            public override void WriteLine()
            {
            }

            public override void WriteLine(String value)
            {
            }

            public override void WriteLine(Object value)
            {
            }
        }

        internal sealed class SyncTextWriter : TextWriter, IDisposable
        {
            private TextWriter _out;
            internal SyncTextWriter(TextWriter t): base (t.FormatProvider)
            {
                _out = t;
            }

            public override Encoding Encoding
            {
                get
                {
                    return _out.Encoding;
                }
            }

            public override IFormatProvider FormatProvider
            {
                get
                {
                    return _out.FormatProvider;
                }
            }

            public override String NewLine
            {
                [MethodImpl(MethodImplOptions.Synchronized)]
                get
                {
                    return _out.NewLine;
                }

                [MethodImpl(MethodImplOptions.Synchronized)]
                set
                {
                    _out.NewLine = value;
                }
            }

            public override void Close()
            {
                _out.Close();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    ((IDisposable)_out).Dispose();
            }

            public override void Flush()
            {
                _out.Flush();
            }

            public override void Write(char value)
            {
                _out.Write(value);
            }

            public override void Write(char[] buffer)
            {
                _out.Write(buffer);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                _out.Write(buffer, index, count);
            }

            public override void Write(bool value)
            {
                _out.Write(value);
            }

            public override void Write(int value)
            {
                _out.Write(value);
            }

            public override void Write(uint value)
            {
                _out.Write(value);
            }

            public override void Write(long value)
            {
                _out.Write(value);
            }

            public override void Write(ulong value)
            {
                _out.Write(value);
            }

            public override void Write(float value)
            {
                _out.Write(value);
            }

            public override void Write(double value)
            {
                _out.Write(value);
            }

            public override void Write(Decimal value)
            {
                _out.Write(value);
            }

            public override void Write(String value)
            {
                _out.Write(value);
            }

            public override void Write(Object value)
            {
                _out.Write(value);
            }

            public override void Write(String format, Object arg0)
            {
                _out.Write(format, arg0);
            }

            public override void Write(String format, Object arg0, Object arg1)
            {
                _out.Write(format, arg0, arg1);
            }

            public override void Write(String format, Object arg0, Object arg1, Object arg2)
            {
                _out.Write(format, arg0, arg1, arg2);
            }

            public override void Write(String format, Object[] arg)
            {
                _out.Write(format, arg);
            }

            public override void WriteLine()
            {
                _out.WriteLine();
            }

            public override void WriteLine(char value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(decimal value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(char[] buffer)
            {
                _out.WriteLine(buffer);
            }

            public override void WriteLine(char[] buffer, int index, int count)
            {
                _out.WriteLine(buffer, index, count);
            }

            public override void WriteLine(bool value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(int value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(uint value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(long value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(ulong value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(float value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(double value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(String value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(Object value)
            {
                _out.WriteLine(value);
            }

            public override void WriteLine(String format, Object arg0)
            {
                _out.WriteLine(format, arg0);
            }

            public override void WriteLine(String format, Object arg0, Object arg1)
            {
                _out.WriteLine(format, arg0, arg1);
            }

            public override void WriteLine(String format, Object arg0, Object arg1, Object arg2)
            {
                _out.WriteLine(format, arg0, arg1, arg2);
            }

            public override void WriteLine(String format, Object[] arg)
            {
                _out.WriteLine(format, arg);
            }

            public override Task WriteAsync(char value)
            {
                Write(value);
                return Task.CompletedTask;
            }

            public override Task WriteAsync(String value)
            {
                Write(value);
                return Task.CompletedTask;
            }

            public override Task WriteAsync(char[] buffer, int index, int count)
            {
                Write(buffer, index, count);
                return Task.CompletedTask;
            }

            public override Task WriteLineAsync(char value)
            {
                WriteLine(value);
                return Task.CompletedTask;
            }

            public override Task WriteLineAsync(String value)
            {
                WriteLine(value);
                return Task.CompletedTask;
            }

            public override Task WriteLineAsync(char[] buffer, int index, int count)
            {
                WriteLine(buffer, index, count);
                return Task.CompletedTask;
            }

            public override Task FlushAsync()
            {
                Flush();
                return Task.CompletedTask;
            }
        }
    }
}