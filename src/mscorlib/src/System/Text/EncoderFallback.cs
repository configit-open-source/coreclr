using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Text
{
    public abstract class EncoderFallback
    {
        internal bool bIsMicrosoftBestFitFallback = false;
        private static volatile EncoderFallback replacementFallback;
        private static volatile EncoderFallback exceptionFallback;
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

        public static EncoderFallback ReplacementFallback
        {
            get
            {
                if (replacementFallback == null)
                    lock (InternalSyncObject)
                        if (replacementFallback == null)
                            replacementFallback = new EncoderReplacementFallback();
                return replacementFallback;
            }
        }

        public static EncoderFallback ExceptionFallback
        {
            get
            {
                if (exceptionFallback == null)
                    lock (InternalSyncObject)
                        if (exceptionFallback == null)
                            exceptionFallback = new EncoderExceptionFallback();
                return exceptionFallback;
            }
        }

        public abstract EncoderFallbackBuffer CreateFallbackBuffer();
        public abstract int MaxCharCount
        {
            get;
        }
    }

    public abstract class EncoderFallbackBuffer
    {
        public abstract bool Fallback(char charUnknown, int index);
        public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);
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

        internal unsafe char *charStart;
        internal unsafe char *charEnd;
        internal EncoderNLS encoder;
        internal bool setEncoder;
        internal bool bUsedEncoder;
        internal bool bFallingBack = false;
        internal int iRecursionCount = 0;
        private const int iMaxRecursion = 250;
        internal unsafe void InternalReset()
        {
            charStart = null;
            bFallingBack = false;
            iRecursionCount = 0;
            Reset();
        }

        internal unsafe void InternalInitialize(char *charStart, char *charEnd, EncoderNLS encoder, bool setEncoder)
        {
            this.charStart = charStart;
            this.charEnd = charEnd;
            this.encoder = encoder;
            this.setEncoder = setEncoder;
            this.bUsedEncoder = false;
            this.bFallingBack = false;
            this.iRecursionCount = 0;
        }

        internal char InternalGetNextChar()
        {
            char ch = GetNextChar();
            bFallingBack = (ch != 0);
            if (ch == 0)
                iRecursionCount = 0;
            return ch;
        }

        internal unsafe virtual bool InternalFallback(char ch, ref char *chars)
        {
            Contract.Assert(charStart != null, "[EncoderFallback.InternalFallbackBuffer]Fallback buffer is not initialized");
            int index = (int)(chars - charStart) - 1;
            if (Char.IsHighSurrogate(ch))
            {
                if (chars >= this.charEnd)
                {
                    if (this.encoder != null && !this.encoder.MustFlush)
                    {
                        if (this.setEncoder)
                        {
                            bUsedEncoder = true;
                            this.encoder.charLeftOver = ch;
                        }

                        bFallingBack = false;
                        return false;
                    }
                }
                else
                {
                    char cNext = *chars;
                    if (Char.IsLowSurrogate(cNext))
                    {
                        if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                            ThrowLastCharRecursive(Char.ConvertToUtf32(ch, cNext));
                        chars++;
                        bFallingBack = Fallback(ch, cNext, index);
                        return bFallingBack;
                    }
                }
            }

            if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                ThrowLastCharRecursive((int)ch);
            bFallingBack = Fallback(ch, index);
            return bFallingBack;
        }

        internal void ThrowLastCharRecursive(int charRecursive)
        {
            throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallback", charRecursive), "chars");
        }
    }
}