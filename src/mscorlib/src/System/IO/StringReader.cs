using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace System.IO
{
    public class StringReader : TextReader
    {
        private String _s;
        private int _pos;
        private int _length;
        public StringReader(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            _s = s;
            _length = s == null ? 0 : s.Length;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            _s = null;
            _pos = 0;
            _length = 0;
            base.Dispose(disposing);
        }

        public override int Peek()
        {
            if (_s == null)
                __Error.ReaderClosed();
            if (_pos == _length)
                return -1;
            return _s[_pos];
        }

        public override int Read()
        {
            if (_s == null)
                __Error.ReaderClosed();
            if (_pos == _length)
                return -1;
            return _s[_pos++];
        }

        public override int Read([In, Out] char[] buffer, int index, int count)
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
            if (_s == null)
                __Error.ReaderClosed();
            int n = _length - _pos;
            if (n > 0)
            {
                if (n > count)
                    n = count;
                _s.CopyTo(_pos, buffer, index, n);
                _pos += n;
            }

            return n;
        }

        public override String ReadToEnd()
        {
            if (_s == null)
                __Error.ReaderClosed();
            String s;
            if (_pos == 0)
                s = _s;
            else
                s = _s.Substring(_pos, _length - _pos);
            _pos = _length;
            return s;
        }

        public override String ReadLine()
        {
            if (_s == null)
                __Error.ReaderClosed();
            int i = _pos;
            while (i < _length)
            {
                char ch = _s[i];
                if (ch == '\r' || ch == '\n')
                {
                    String result = _s.Substring(_pos, i - _pos);
                    _pos = i + 1;
                    if (ch == '\r' && _pos < _length && _s[_pos] == '\n')
                        _pos++;
                    return result;
                }

                i++;
            }

            if (i > _pos)
            {
                String result = _s.Substring(_pos, i - _pos);
                _pos = i;
                return result;
            }

            return null;
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