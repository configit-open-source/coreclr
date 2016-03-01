
using System.IO;
using System.Text;

namespace System.Security.Util
{
    internal sealed class Tokenizer
    {
        internal const byte bra = 0;
        internal const byte ket = 1;
        internal const byte slash = 2;
        internal const byte cstr = 3;
        internal const byte equals = 4;
        internal const byte quest = 5;
        internal const byte bang = 6;
        internal const byte dash = 7;
        internal const int intOpenBracket = (int)'<';
        internal const int intCloseBracket = (int)'>';
        internal const int intSlash = (int)'/';
        internal const int intEquals = (int)'=';
        internal const int intQuote = (int)'\"';
        internal const int intQuest = (int)'?';
        internal const int intBang = (int)'!';
        internal const int intDash = (int)'-';
        internal const int intTab = (int)'\t';
        internal const int intCR = (int)'\r';
        internal const int intLF = (int)'\n';
        internal const int intSpace = (int)' ';
        private enum TokenSource
        {
            UnicodeByteArray,
            UTF8ByteArray,
            ASCIIByteArray,
            CharArray,
            String,
            NestedStrings,
            Other
        }

        internal enum ByteTokenEncoding
        {
            UnicodeTokens,
            UTF8Tokens,
            ByteTokens
        }

        public int LineNo;
        private int _inProcessingTag;
        private byte[] _inBytes;
        private char[] _inChars;
        private String _inString;
        private int _inIndex;
        private int _inSize;
        private int _inSavedCharacter;
        private TokenSource _inTokenSource;
        private ITokenReader _inTokenReader;
        private StringMaker _maker = null;
        private String[] _searchStrings;
        private String[] _replaceStrings;
        private int _inNestedIndex;
        private int _inNestedSize;
        private String _inNestedString;
        internal void BasicInitialization()
        {
            LineNo = 1;
            _inProcessingTag = 0;
            _inSavedCharacter = -1;
            _inIndex = 0;
            _inSize = 0;
            _inNestedSize = 0;
            _inNestedIndex = 0;
            _inTokenSource = TokenSource.Other;
            _maker = System.SharedStatics.GetSharedStringMaker();
        }

        public void Recycle()
        {
            System.SharedStatics.ReleaseSharedStringMaker(ref _maker);
        }

        internal Tokenizer(String input)
        {
            BasicInitialization();
            _inString = input;
            _inSize = input.Length;
            _inTokenSource = TokenSource.String;
        }

        internal Tokenizer(String input, String[] searchStrings, String[] replaceStrings)
        {
            BasicInitialization();
            _inString = input;
            _inSize = _inString.Length;
            _inTokenSource = TokenSource.NestedStrings;
            _searchStrings = searchStrings;
            _replaceStrings = replaceStrings;
                                    for (int istr = 0; istr < searchStrings.Length; istr++)
            {
                String str = searchStrings[istr];
                                                                                str = replaceStrings[istr];
                                            }
        }

        internal Tokenizer(byte[] array, ByteTokenEncoding encoding, int startIndex)
        {
            BasicInitialization();
            _inBytes = array;
            _inSize = array.Length;
            _inIndex = startIndex;
            switch (encoding)
            {
                case ByteTokenEncoding.UnicodeTokens:
                    _inTokenSource = TokenSource.UnicodeByteArray;
                    break;
                case ByteTokenEncoding.UTF8Tokens:
                    _inTokenSource = TokenSource.UTF8ByteArray;
                    break;
                case ByteTokenEncoding.ByteTokens:
                    _inTokenSource = TokenSource.ASCIIByteArray;
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)encoding));
            }
        }

        internal Tokenizer(char[] array)
        {
            BasicInitialization();
            _inChars = array;
            _inSize = array.Length;
            _inTokenSource = TokenSource.CharArray;
        }

        internal Tokenizer(StreamReader input)
        {
            BasicInitialization();
            _inTokenReader = new StreamTokenReader(input);
        }

        internal void ChangeFormat(System.Text.Encoding encoding)
        {
            if (encoding == null)
            {
                return;
            }

                        switch (_inTokenSource)
            {
                case TokenSource.UnicodeByteArray:
                case TokenSource.UTF8ByteArray:
                case TokenSource.ASCIIByteArray:
                    if (encoding == System.Text.Encoding.Unicode)
                    {
                        _inTokenSource = TokenSource.UnicodeByteArray;
                        return;
                    }

                    if (encoding == System.Text.Encoding.UTF8)
                    {
                        _inTokenSource = TokenSource.UTF8ByteArray;
                        return;
                    }

                    if (encoding == System.Text.Encoding.ASCII)
                    {
                        _inTokenSource = TokenSource.ASCIIByteArray;
                        return;
                    }

                    break;
                case TokenSource.String:
                case TokenSource.CharArray:
                case TokenSource.NestedStrings:
                    return;
            }

            Stream stream = null;
            switch (_inTokenSource)
            {
                case TokenSource.UnicodeByteArray:
                case TokenSource.UTF8ByteArray:
                case TokenSource.ASCIIByteArray:
                    stream = new MemoryStream(_inBytes, _inIndex, _inSize - _inIndex);
                    break;
                case TokenSource.CharArray:
                case TokenSource.String:
                case TokenSource.NestedStrings:
                                        return;
                default:
                    StreamTokenReader reader = _inTokenReader as StreamTokenReader;
                    if (reader == null)
                    {
                                                return;
                    }

                    stream = reader._in.BaseStream;
                                        String fakeReadString = new String(' ', reader.NumCharEncountered);
                    stream.Position = reader._in.CurrentEncoding.GetByteCount(fakeReadString);
                    break;
            }

                        _inTokenReader = new StreamTokenReader(new StreamReader(stream, encoding));
            _inTokenSource = TokenSource.Other;
        }

        internal void GetTokens(TokenizerStream stream, int maxNum, bool endAfterKet)
        {
            while (maxNum == -1 || stream.GetTokenCount() < maxNum)
            {
                int i = -1;
                byte ch;
                int cb = 0;
                bool inLiteral = false;
                bool inQuotedString = false;
                StringMaker m = _maker;
                m._outStringBuilder = null;
                m._outIndex = 0;
                BEGINNING:
                    if (_inSavedCharacter != -1)
                    {
                        i = _inSavedCharacter;
                        _inSavedCharacter = -1;
                    }
                    else
                        switch (_inTokenSource)
                        {
                            case TokenSource.UnicodeByteArray:
                                if (_inIndex + 1 >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)((_inBytes[_inIndex + 1] << 8) + _inBytes[_inIndex]);
                                _inIndex += 2;
                                break;
                            case TokenSource.UTF8ByteArray:
                                if (_inIndex >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)(_inBytes[_inIndex++]);
                                if ((i & 0x80) == 0x00)
                                    break;
                                switch ((i & 0xf0) >> 4)
                                {
                                    case 0x8:
                                    case 0x9:
                                    case 0xa:
                                    case 0xb:
                                        throw new XmlSyntaxException(LineNo);
                                    case 0xc:
                                    case 0xd:
                                        i &= 0x1f;
                                        cb = 2;
                                        break;
                                    case 0xe:
                                        i &= 0x0f;
                                        cb = 3;
                                        break;
                                    case 0xf:
                                        throw new XmlSyntaxException(LineNo);
                                }

                                if (_inIndex >= _inSize)
                                    throw new XmlSyntaxException(LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
                                ch = _inBytes[_inIndex++];
                                if ((ch & 0xc0) != 0x80)
                                    throw new XmlSyntaxException(LineNo);
                                i = (i << 6) | (ch & 0x3f);
                                if (cb == 2)
                                    break;
                                if (_inIndex >= _inSize)
                                    throw new XmlSyntaxException(LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
                                ch = _inBytes[_inIndex++];
                                if ((ch & 0xc0) != 0x80)
                                    throw new XmlSyntaxException(LineNo);
                                i = (i << 6) | (ch & 0x3f);
                                break;
                            case TokenSource.ASCIIByteArray:
                                if (_inIndex >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)(_inBytes[_inIndex++]);
                                break;
                            case TokenSource.CharArray:
                                if (_inIndex >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)(_inChars[_inIndex++]);
                                break;
                            case TokenSource.String:
                                if (_inIndex >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)(_inString[_inIndex++]);
                                break;
                            case TokenSource.NestedStrings:
                                if (_inNestedSize != 0)
                                {
                                    if (_inNestedIndex < _inNestedSize)
                                    {
                                        i = _inNestedString[_inNestedIndex++];
                                        break;
                                    }

                                    _inNestedSize = 0;
                                }

                                if (_inIndex >= _inSize)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                i = (int)(_inString[_inIndex++]);
                                if (i != '{')
                                    break;
                                for (int istr = 0; istr < _searchStrings.Length; istr++)
                                {
                                    if (0 == String.Compare(_searchStrings[istr], 0, _inString, _inIndex - 1, _searchStrings[istr].Length, StringComparison.Ordinal))
                                    {
                                        _inNestedString = _replaceStrings[istr];
                                        _inNestedSize = _inNestedString.Length;
                                        _inNestedIndex = 1;
                                        i = _inNestedString[0];
                                        _inIndex += _searchStrings[istr].Length - 1;
                                        break;
                                    }
                                }

                                break;
                            default:
                                i = _inTokenReader.Read();
                                if (i == -1)
                                {
                                    stream.AddToken(-1);
                                    return;
                                }

                                break;
                        }

                if (!inLiteral)
                {
                    switch (i)
                    {
                        case intSpace:
                        case intTab:
                        case intCR:
                            goto BEGINNING;
                        case intLF:
                            LineNo++;
                            goto BEGINNING;
                        case intOpenBracket:
                            _inProcessingTag++;
                            stream.AddToken(bra);
                            continue;
                        case intCloseBracket:
                            _inProcessingTag--;
                            stream.AddToken(ket);
                            if (endAfterKet)
                                return;
                            continue;
                        case intEquals:
                            stream.AddToken(equals);
                            continue;
                        case intSlash:
                            if (_inProcessingTag != 0)
                            {
                                stream.AddToken(slash);
                                continue;
                            }

                            break;
                        case intQuest:
                            if (_inProcessingTag != 0)
                            {
                                stream.AddToken(quest);
                                continue;
                            }

                            break;
                        case intBang:
                            if (_inProcessingTag != 0)
                            {
                                stream.AddToken(bang);
                                continue;
                            }

                            break;
                        case intDash:
                            if (_inProcessingTag != 0)
                            {
                                stream.AddToken(dash);
                                continue;
                            }

                            break;
                        case intQuote:
                            inLiteral = true;
                            inQuotedString = true;
                            goto BEGINNING;
                    }
                }
                else
                {
                    switch (i)
                    {
                        case intOpenBracket:
                            if (!inQuotedString)
                            {
                                _inSavedCharacter = i;
                                stream.AddToken(cstr);
                                stream.AddString(this.GetStringToken());
                                continue;
                            }

                            break;
                        case intCloseBracket:
                        case intEquals:
                        case intSlash:
                            if (!inQuotedString && _inProcessingTag != 0)
                            {
                                _inSavedCharacter = i;
                                stream.AddToken(cstr);
                                stream.AddString(this.GetStringToken());
                                continue;
                            }

                            break;
                        case intQuote:
                            if (inQuotedString)
                            {
                                stream.AddToken(cstr);
                                stream.AddString(this.GetStringToken());
                                continue;
                            }

                            break;
                        case intTab:
                        case intCR:
                        case intSpace:
                            if (!inQuotedString)
                            {
                                stream.AddToken(cstr);
                                stream.AddString(this.GetStringToken());
                                continue;
                            }

                            break;
                        case intLF:
                            LineNo++;
                            if (!inQuotedString)
                            {
                                stream.AddToken(cstr);
                                stream.AddString(this.GetStringToken());
                                continue;
                            }

                            break;
                    }
                }

                inLiteral = true;
                if (m._outIndex < StringMaker.outMaxSize)
                {
                    m._outChars[m._outIndex++] = (char)i;
                }
                else
                {
                    if (m._outStringBuilder == null)
                    {
                        m._outStringBuilder = new StringBuilder();
                    }

                    m._outStringBuilder.Append(m._outChars, 0, StringMaker.outMaxSize);
                    m._outChars[0] = (char)i;
                    m._outIndex = 1;
                }

                goto BEGINNING;
            }
        }

        internal sealed class StringMaker
        {
            String[] aStrings;
            uint cStringsMax;
            uint cStringsUsed;
            public StringBuilder _outStringBuilder;
            public char[] _outChars;
            public int _outIndex;
            public const int outMaxSize = 512;
            static uint HashString(String str)
            {
                uint hash = 0;
                int l = str.Length;
                for (int i = 0; i < l; i++)
                {
                    hash = (hash << 3) ^ (uint)str[i] ^ (hash >> 29);
                }

                return hash;
            }

            static uint HashCharArray(char[] a, int l)
            {
                uint hash = 0;
                for (int i = 0; i < l; i++)
                {
                    hash = (hash << 3) ^ (uint)a[i] ^ (hash >> 29);
                }

                return hash;
            }

            public StringMaker()
            {
                cStringsMax = 2048;
                cStringsUsed = 0;
                aStrings = new String[cStringsMax];
                _outChars = new char[outMaxSize];
            }

            bool CompareStringAndChars(String str, char[] a, int l)
            {
                if (str.Length != l)
                    return false;
                for (int i = 0; i < l; i++)
                    if (a[i] != str[i])
                        return false;
                return true;
            }

            public String MakeString()
            {
                uint hash;
                char[] a = _outChars;
                int l = _outIndex;
                if (_outStringBuilder != null)
                {
                    _outStringBuilder.Append(_outChars, 0, _outIndex);
                    return _outStringBuilder.ToString();
                }

                if (cStringsUsed > (cStringsMax / 4) * 3)
                {
                    uint cNewMax = cStringsMax * 2;
                    String[] aStringsNew = new String[cNewMax];
                    for (int i = 0; i < cStringsMax; i++)
                    {
                        if (aStrings[i] != null)
                        {
                            hash = HashString(aStrings[i]) % cNewMax;
                            while (aStringsNew[hash] != null)
                            {
                                if (++hash >= cNewMax)
                                    hash = 0;
                            }

                            aStringsNew[hash] = aStrings[i];
                        }
                    }

                    cStringsMax = cNewMax;
                    aStrings = aStringsNew;
                }

                hash = HashCharArray(a, l) % cStringsMax;
                String str;
                while ((str = aStrings[hash]) != null)
                {
                    if (CompareStringAndChars(str, a, l))
                        return str;
                    if (++hash >= cStringsMax)
                        hash = 0;
                }

                str = new String(a, 0, l);
                aStrings[hash] = str;
                cStringsUsed++;
                return str;
            }
        }

        private String GetStringToken()
        {
            return _maker.MakeString();
        }

        internal interface ITokenReader
        {
            int Read();
        }

        internal class StreamTokenReader : ITokenReader
        {
            internal StreamReader _in;
            internal int _numCharRead;
            internal StreamTokenReader(StreamReader input)
            {
                _in = input;
                _numCharRead = 0;
            }

            public virtual int Read()
            {
                int value = _in.Read();
                if (value != -1)
                    _numCharRead++;
                return value;
            }

            internal int NumCharEncountered
            {
                get
                {
                    return _numCharRead;
                }
            }
        }
    }

    internal sealed class TokenizerShortBlock
    {
        internal short[] m_block = new short[16];
        internal TokenizerShortBlock m_next = null;
    }

    internal sealed class TokenizerStringBlock
    {
        internal String[] m_block = new String[16];
        internal TokenizerStringBlock m_next = null;
    }

    internal sealed class TokenizerStream
    {
        private int m_countTokens;
        private TokenizerShortBlock m_headTokens;
        private TokenizerShortBlock m_lastTokens;
        private TokenizerShortBlock m_currentTokens;
        private int m_indexTokens;
        private TokenizerStringBlock m_headStrings;
        private TokenizerStringBlock m_currentStrings;
        private int m_indexStrings;
        private bool m_bLastWasCStr;
        internal TokenizerStream()
        {
            m_countTokens = 0;
            m_headTokens = new TokenizerShortBlock();
            m_headStrings = new TokenizerStringBlock();
            Reset();
        }

        internal void AddToken(short token)
        {
            if (m_currentTokens.m_block.Length <= m_indexTokens)
            {
                m_currentTokens.m_next = new TokenizerShortBlock();
                m_currentTokens = m_currentTokens.m_next;
                m_indexTokens = 0;
            }

            m_countTokens++;
            m_currentTokens.m_block[m_indexTokens++] = token;
        }

        internal void AddString(String str)
        {
            if (m_currentStrings.m_block.Length <= m_indexStrings)
            {
                m_currentStrings.m_next = new TokenizerStringBlock();
                m_currentStrings = m_currentStrings.m_next;
                m_indexStrings = 0;
            }

            m_currentStrings.m_block[m_indexStrings++] = str;
        }

        internal void Reset()
        {
            m_lastTokens = null;
            m_currentTokens = m_headTokens;
            m_currentStrings = m_headStrings;
            m_indexTokens = 0;
            m_indexStrings = 0;
            m_bLastWasCStr = false;
        }

        internal short GetNextFullToken()
        {
            if (m_currentTokens.m_block.Length <= m_indexTokens)
            {
                m_lastTokens = m_currentTokens;
                m_currentTokens = m_currentTokens.m_next;
                m_indexTokens = 0;
            }

            return m_currentTokens.m_block[m_indexTokens++];
        }

        internal short GetNextToken()
        {
            short retval = (short)(GetNextFullToken() & 0x00FF);
                        m_bLastWasCStr = (retval == Tokenizer.cstr);
            return retval;
        }

        internal String GetNextString()
        {
            if (m_currentStrings.m_block.Length <= m_indexStrings)
            {
                m_currentStrings = m_currentStrings.m_next;
                m_indexStrings = 0;
            }

            m_bLastWasCStr = false;
            return m_currentStrings.m_block[m_indexStrings++];
        }

        internal void ThrowAwayNextString()
        {
            GetNextString();
        }

        internal void TagLastToken(short tag)
        {
            if (m_indexTokens == 0)
                m_lastTokens.m_block[m_lastTokens.m_block.Length - 1] = (short)((ushort)m_lastTokens.m_block[m_lastTokens.m_block.Length - 1] | (ushort)tag);
            else
                m_currentTokens.m_block[m_indexTokens - 1] = (short)((ushort)m_currentTokens.m_block[m_indexTokens - 1] | (ushort)tag);
        }

        internal int GetTokenCount()
        {
            return m_countTokens;
        }

        internal void GoToPosition(int position)
        {
            Reset();
            for (int count = 0; count < position; ++count)
            {
                if (GetNextToken() == Tokenizer.cstr)
                    ThrowAwayNextString();
            }
        }
    }
}