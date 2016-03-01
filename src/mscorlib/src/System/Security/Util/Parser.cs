using System.IO;

namespace System.Security.Util
{
    sealed internal class Parser
    {
        private SecurityDocument _doc;
        private Tokenizer _t;
        internal SecurityElement GetTopElement()
        {
            return _doc.GetRootElement();
        }

        private const short c_flag = 0x4000;
        private const short c_elementtag = (short)(SecurityDocument.c_element << 8 | c_flag);
        private const short c_attributetag = (short)(SecurityDocument.c_attribute << 8 | c_flag);
        private const short c_texttag = (short)(SecurityDocument.c_text << 8 | c_flag);
        private const short c_additionaltexttag = (short)(SecurityDocument.c_text << 8 | c_flag | 0x2000);
        private const short c_childrentag = (short)(SecurityDocument.c_children << 8 | c_flag);
        private const short c_wastedstringtag = (short)(0x1000 | c_flag);
        private void GetRequiredSizes(TokenizerStream stream, ref int index)
        {
            bool needToBreak = false;
            bool needToPop = false;
            bool createdNode = false;
            bool intag = false;
            int stackDepth = 1;
            SecurityElementType type = SecurityElementType.Regular;
            String strValue = null;
            bool sawEquals = false;
            bool sawText = false;
            int status = 0;
            short i;
            do
            {
                for (i = stream.GetNextToken(); i != -1; i = stream.GetNextToken())
                {
                    switch (i & 0x00FF)
                    {
                        case Tokenizer.cstr:
                        {
                            if (intag)
                            {
                                if (type == SecurityElementType.Comment)
                                {
                                    stream.ThrowAwayNextString();
                                    stream.TagLastToken(c_wastedstringtag);
                                }
                                else
                                {
                                    if (strValue == null)
                                    {
                                        strValue = stream.GetNextString();
                                    }
                                    else
                                    {
                                        if (!sawEquals)
                                            throw new XmlSyntaxException(_t.LineNo);
                                        stream.TagLastToken(c_attributetag);
                                        index += SecurityDocument.EncodedStringSize(strValue) + SecurityDocument.EncodedStringSize(stream.GetNextString()) + 1;
                                        strValue = null;
                                        sawEquals = false;
                                    }
                                }
                            }
                            else
                            {
                                if (sawText)
                                {
                                    stream.TagLastToken(c_additionaltexttag);
                                    index += SecurityDocument.EncodedStringSize(stream.GetNextString()) + SecurityDocument.EncodedStringSize(" ");
                                }
                                else
                                {
                                    stream.TagLastToken(c_texttag);
                                    index += SecurityDocument.EncodedStringSize(stream.GetNextString()) + 1;
                                    sawText = true;
                                }
                            }
                        }

                            break;
                        case Tokenizer.bra:
                            intag = true;
                            sawText = false;
                            i = stream.GetNextToken();
                            if (i == Tokenizer.slash)
                            {
                                stream.TagLastToken(c_childrentag);
                                while (true)
                                {
                                    i = stream.GetNextToken();
                                    if (i == Tokenizer.cstr)
                                    {
                                        stream.ThrowAwayNextString();
                                        stream.TagLastToken(c_wastedstringtag);
                                    }
                                    else if (i == -1)
                                        throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
                                    else
                                        break;
                                }

                                if (i != Tokenizer.ket)
                                {
                                    throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_ExpectedCloseBracket"));
                                }

                                intag = false;
                                index++;
                                sawText = false;
                                stackDepth--;
                                needToBreak = true;
                            }
                            else if (i == Tokenizer.cstr)
                            {
                                createdNode = true;
                                stream.TagLastToken(c_elementtag);
                                index += SecurityDocument.EncodedStringSize(stream.GetNextString()) + 1;
                                if (type != SecurityElementType.Regular)
                                    throw new XmlSyntaxException(_t.LineNo);
                                needToBreak = true;
                                stackDepth++;
                            }
                            else if (i == Tokenizer.bang)
                            {
                                status = 1;
                                do
                                {
                                    i = stream.GetNextToken();
                                    switch (i)
                                    {
                                        case Tokenizer.bra:
                                            status++;
                                            break;
                                        case Tokenizer.ket:
                                            status--;
                                            break;
                                        case Tokenizer.cstr:
                                            stream.ThrowAwayNextString();
                                            stream.TagLastToken(c_wastedstringtag);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                while (status > 0);
                                intag = false;
                                sawText = false;
                                needToBreak = true;
                            }
                            else if (i == Tokenizer.quest)
                            {
                                i = stream.GetNextToken();
                                if (i != Tokenizer.cstr)
                                    throw new XmlSyntaxException(_t.LineNo);
                                createdNode = true;
                                type = SecurityElementType.Format;
                                stream.TagLastToken(c_elementtag);
                                index += SecurityDocument.EncodedStringSize(stream.GetNextString()) + 1;
                                status = 1;
                                stackDepth++;
                                needToBreak = true;
                            }
                            else
                            {
                                throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_ExpectedSlashOrString"));
                            }

                            break;
                        case Tokenizer.equals:
                            sawEquals = true;
                            break;
                        case Tokenizer.ket:
                            if (intag)
                            {
                                intag = false;
                                continue;
                            }
                            else
                            {
                                throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedCloseBracket"));
                            }

                        case Tokenizer.slash:
                            i = stream.GetNextToken();
                            if (i == Tokenizer.ket)
                            {
                                stream.TagLastToken(c_childrentag);
                                index++;
                                stackDepth--;
                                sawText = false;
                                needToBreak = true;
                            }
                            else
                            {
                                throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_ExpectedCloseBracket"));
                            }

                            break;
                        case Tokenizer.quest:
                            if (intag && type == SecurityElementType.Format && status == 1)
                            {
                                i = stream.GetNextToken();
                                if (i == Tokenizer.ket)
                                {
                                    stream.TagLastToken(c_childrentag);
                                    index++;
                                    stackDepth--;
                                    sawText = false;
                                    needToBreak = true;
                                }
                                else
                                {
                                    throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_ExpectedCloseBracket"));
                                }
                            }
                            else
                            {
                                throw new XmlSyntaxException(_t.LineNo);
                            }

                            break;
                        case Tokenizer.dash:
                        default:
                            throw new XmlSyntaxException(_t.LineNo);
                    }

                    if (needToBreak)
                    {
                        needToBreak = false;
                        needToPop = false;
                        break;
                    }
                    else
                    {
                        needToPop = true;
                    }
                }

                if (needToPop)
                {
                    index++;
                    stackDepth--;
                    sawText = false;
                }
                else if (i == -1 && (stackDepth != 1 || !createdNode))
                {
                    throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
                }
            }
            while (stackDepth > 1);
        }

        private int DetermineFormat(TokenizerStream stream)
        {
            if (stream.GetNextToken() == Tokenizer.bra)
            {
                if (stream.GetNextToken() == Tokenizer.quest)
                {
                    _t.GetTokens(stream, -1, true);
                    stream.GoToPosition(2);
                    bool sawEquals = false;
                    bool sawEncoding = false;
                    short i;
                    for (i = stream.GetNextToken(); i != -1 && i != Tokenizer.ket; i = stream.GetNextToken())
                    {
                        switch (i)
                        {
                            case Tokenizer.cstr:
                                if (sawEquals && sawEncoding)
                                {
                                    _t.ChangeFormat(System.Text.Encoding.GetEncoding(stream.GetNextString()));
                                    return 0;
                                }
                                else if (!sawEquals)
                                {
                                    if (String.Compare(stream.GetNextString(), "encoding", StringComparison.Ordinal) == 0)
                                        sawEncoding = true;
                                }
                                else
                                {
                                    sawEquals = false;
                                    sawEncoding = false;
                                    stream.ThrowAwayNextString();
                                }

                                break;
                            case Tokenizer.equals:
                                sawEquals = true;
                                break;
                            default:
                                throw new XmlSyntaxException(_t.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
                        }
                    }

                    return 0;
                }
            }

            return 2;
        }

        private void ParseContents()
        {
            short i;
            TokenizerStream stream = new TokenizerStream();
            _t.GetTokens(stream, 2, false);
            stream.Reset();
            int gotoPosition = DetermineFormat(stream);
            stream.GoToPosition(gotoPosition);
            _t.GetTokens(stream, -1, false);
            stream.Reset();
            int neededIndex = 0;
            GetRequiredSizes(stream, ref neededIndex);
            _doc = new SecurityDocument(neededIndex);
            int position = 0;
            stream.Reset();
            for (i = stream.GetNextFullToken(); i != -1; i = stream.GetNextFullToken())
            {
                if ((i & c_flag) != c_flag)
                    continue;
                else
                {
                    switch ((short)(i & 0xFF00))
                    {
                        case c_elementtag:
                            _doc.AddToken(SecurityDocument.c_element, ref position);
                            _doc.AddString(stream.GetNextString(), ref position);
                            break;
                        case c_attributetag:
                            _doc.AddToken(SecurityDocument.c_attribute, ref position);
                            _doc.AddString(stream.GetNextString(), ref position);
                            _doc.AddString(stream.GetNextString(), ref position);
                            break;
                        case c_texttag:
                            _doc.AddToken(SecurityDocument.c_text, ref position);
                            _doc.AddString(stream.GetNextString(), ref position);
                            break;
                        case c_additionaltexttag:
                            _doc.AppendString(" ", ref position);
                            _doc.AppendString(stream.GetNextString(), ref position);
                            break;
                        case c_childrentag:
                            _doc.AddToken(SecurityDocument.c_children, ref position);
                            break;
                        case c_wastedstringtag:
                            stream.ThrowAwayNextString();
                            break;
                        default:
                            throw new XmlSyntaxException();
                    }
                }
            }
        }

        private Parser(Tokenizer t)
        {
            _t = t;
            _doc = null;
            try
            {
                ParseContents();
            }
            finally
            {
                _t.Recycle();
            }
        }

        internal Parser(String input): this (new Tokenizer(input))
        {
        }

        internal Parser(String input, String[] searchStrings, String[] replaceStrings): this (new Tokenizer(input, searchStrings, replaceStrings))
        {
        }

        internal Parser(byte[] array, Tokenizer.ByteTokenEncoding encoding): this (new Tokenizer(array, encoding, 0))
        {
        }

        internal Parser(byte[] array, Tokenizer.ByteTokenEncoding encoding, int startIndex): this (new Tokenizer(array, encoding, startIndex))
        {
        }

        internal Parser(StreamReader input): this (new Tokenizer(input))
        {
        }

        internal Parser(char[] array): this (new Tokenizer(array))
        {
        }
    }
}