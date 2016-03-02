
using System.Threading;

namespace System.Text
{
    internal class InternalEncoderBestFitFallback : EncoderFallback
    {
        internal Encoding encoding = null;
        internal char[] arrayBestFit = null;
        internal InternalEncoderBestFitFallback(Encoding encoding)
        {
            this.encoding = encoding;
            this.bIsMicrosoftBestFitFallback = true;
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new InternalEncoderBestFitFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get
            {
                return 1;
            }
        }

        public override bool Equals(Object value)
        {
            InternalEncoderBestFitFallback that = value as InternalEncoderBestFitFallback;
            if (that != null)
            {
                return (this.encoding.CodePage == that.encoding.CodePage);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return this.encoding.CodePage;
        }
    }

    internal sealed class InternalEncoderBestFitFallbackBuffer : EncoderFallbackBuffer
    {
        private char cBestFit = '\0';
        private InternalEncoderBestFitFallback oFallback;
        private int iCount = -1;
        private int iSize;
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

        public InternalEncoderBestFitFallbackBuffer(InternalEncoderBestFitFallback fallback)
        {
            this.oFallback = fallback;
            if (oFallback.arrayBestFit == null)
            {
                lock (InternalSyncObject)
                {
                    if (oFallback.arrayBestFit == null)
                        oFallback.arrayBestFit = fallback.encoding.GetBestFitUnicodeToBytesData();
                }
            }
        }

        public override bool Fallback(char charUnknown, int index)
        {
                        iCount = iSize = 1;
            cBestFit = TryBestFit(charUnknown);
            if (cBestFit == '\0')
                cBestFit = '?';
            return true;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            if (!Char.IsHighSurrogate(charUnknownHigh))
                throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xD800, 0xDBFF));
            if (!Char.IsLowSurrogate(charUnknownLow))
                throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xDC00, 0xDFFF));
                                    cBestFit = '?';
            iCount = iSize = 2;
            return true;
        }

        public override char GetNextChar()
        {
            iCount--;
            if (iCount < 0)
                return '\0';
            if (iCount == int.MaxValue)
            {
                iCount = -1;
                return '\0';
            }

            return cBestFit;
        }

        public override bool MovePrevious()
        {
            if (iCount >= 0)
                iCount++;
            return (iCount >= 0 && iCount <= iSize);
        }

        public override int Remaining
        {
            get
            {
                return (iCount > 0) ? iCount : 0;
            }
        }

        public override unsafe void Reset()
        {
            iCount = -1;
            charStart = null;
            bFallingBack = false;
        }

        private char TryBestFit(char cUnknown)
        {
            int lowBound = 0;
            int highBound = oFallback.arrayBestFit.Length;
            int index;
            int iDiff;
            while ((iDiff = (highBound - lowBound)) > 6)
            {
                index = ((iDiff / 2) + lowBound) & 0xFFFE;
                char cTest = oFallback.arrayBestFit[index];
                if (cTest == cUnknown)
                {
                                        return oFallback.arrayBestFit[index + 1];
                }
                else if (cTest < cUnknown)
                {
                    lowBound = index;
                }
                else
                {
                    highBound = index;
                }
            }

            for (index = lowBound; index < highBound; index += 2)
            {
                if (oFallback.arrayBestFit[index] == cUnknown)
                {
                                        return oFallback.arrayBestFit[index + 1];
                }
            }

            return '\0';
        }
    }
}