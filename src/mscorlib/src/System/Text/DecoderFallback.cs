using System;
using System.Security;
using System.Threading;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Text
{
    public abstract class DecoderFallback
    {
        internal bool bIsMicrosoftBestFitFallback = false;
        private static volatile DecoderFallback replacementFallback;
        private static volatile DecoderFallback exceptionFallback;
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        public static DecoderFallback ReplacementFallback
        {
            get
            {
                if (replacementFallback == null)
                    lock (InternalSyncObject)
                        if (replacementFallback == null)
                            replacementFallback = new DecoderReplacementFallback();
                return replacementFallback;
            }
        }

        public static DecoderFallback ExceptionFallback
        {
            get
            {
                if (exceptionFallback == null)
                    lock (InternalSyncObject)
                        if (exceptionFallback == null)
                            exceptionFallback = new DecoderExceptionFallback();
                return exceptionFallback;
            }
        }

        public abstract DecoderFallbackBuffer CreateFallbackBuffer();
        public abstract int MaxCharCount
        {
            get;
        }

        internal bool IsMicrosoftBestFitFallback
        {
            get
            {
                return bIsMicrosoftBestFitFallback;
            }
        }
    }

    public abstract class DecoderFallbackBuffer
    {
        public abstract bool Fallback(byte[] bytesUnknown, int index);
        public abstract char GetNextChar();
        public abstract bool MovePrevious();
        public abstract int Remaining
        {
            get;
        }

        public virtual void Reset()
        {
            while (GetNextChar() != (char)0)
                ;
        }

        internal unsafe byte *byteStart;
        internal unsafe char *charEnd;
        internal unsafe void InternalReset()
        {
            byteStart = null;
            Reset();
        }

        internal unsafe void InternalInitialize(byte *byteStart, char *charEnd)
        {
            this.byteStart = byteStart;
            this.charEnd = charEnd;
        }

        internal unsafe virtual bool InternalFallback(byte[] bytes, byte *pBytes, ref char *chars)
        {
            Contract.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");
            if (this.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                char ch;
                char *charTemp = chars;
                bool bHighSurrogate = false;
                while ((ch = GetNextChar()) != 0)
                {
                    if (Char.IsSurrogate(ch))
                    {
                        if (Char.IsHighSurrogate(ch))
                        {
                            if (bHighSurrogate)
                                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                            bHighSurrogate = true;
                        }
                        else
                        {
                            if (bHighSurrogate == false)
                                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                            bHighSurrogate = false;
                        }
                    }

                    if (charTemp >= charEnd)
                    {
                        return false;
                    }

                    *(charTemp++) = ch;
                }

                if (bHighSurrogate)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                chars = charTemp;
            }

            return true;
        }

        internal unsafe virtual int InternalFallback(byte[] bytes, byte *pBytes)
        {
            Contract.Assert(byteStart != null, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");
            if (this.Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
            {
                int count = 0;
                char ch;
                bool bHighSurrogate = false;
                while ((ch = GetNextChar()) != 0)
                {
                    if (Char.IsSurrogate(ch))
                    {
                        if (Char.IsHighSurrogate(ch))
                        {
                            if (bHighSurrogate)
                                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                            bHighSurrogate = true;
                        }
                        else
                        {
                            if (bHighSurrogate == false)
                                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                            bHighSurrogate = false;
                        }
                    }

                    count++;
                }

                if (bHighSurrogate)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                return count;
            }

            return 0;
        }

        internal void ThrowLastBytesRecursive(byte[] bytesUnknown)
        {
            StringBuilder strBytes = new StringBuilder(bytesUnknown.Length * 3);
            int i;
            for (i = 0; i < bytesUnknown.Length && i < 20; i++)
            {
                if (strBytes.Length > 0)
                    strBytes.Append(" ");
                strBytes.Append(String.Format(CultureInfo.InvariantCulture, "\\x{0:X2}", bytesUnknown[i]));
            }

            if (i == 20)
                strBytes.Append(" ...");
            throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallbackBytes", strBytes.ToString()), "bytesUnknown");
        }
    }
}