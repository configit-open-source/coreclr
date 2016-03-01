using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public class StringWriter : TextWriter
    {
        private static volatile UnicodeEncoding m_encoding = null;
        private StringBuilder _sb;
        private bool _isOpen;
        public StringWriter(): this (new StringBuilder(), CultureInfo.CurrentCulture)
        {
        }

        public StringWriter(IFormatProvider formatProvider): this (new StringBuilder(), formatProvider)
        {
        }

        public StringWriter(StringBuilder sb): this (sb, CultureInfo.CurrentCulture)
        {
        }

        public StringWriter(StringBuilder sb, IFormatProvider formatProvider): base (formatProvider)
        {
            if (sb == null)
                throw new ArgumentNullException("sb", Environment.GetResourceString("ArgumentNull_Buffer"));
            Contract.EndContractBlock();
            _sb = sb;
            _isOpen = true;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            base.Dispose(disposing);
        }

        public override Encoding Encoding
        {
            get
            {
                if (m_encoding == null)
                {
                    m_encoding = new UnicodeEncoding(false, false);
                }

                return m_encoding;
            }
        }

        public virtual StringBuilder GetStringBuilder()
        {
            return _sb;
        }

        public override void Write(char value)
        {
            if (!_isOpen)
                __Error.WriterClosed();
            _sb.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
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
            if (!_isOpen)
                __Error.WriterClosed();
            _sb.Append(buffer, index, count);
        }

        public override void Write(String value)
        {
            if (!_isOpen)
                __Error.WriterClosed();
            if (value != null)
                _sb.Append(value);
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
            return Task.CompletedTask;
        }

        public override String ToString()
        {
            return _sb.ToString();
        }
    }
}