
using System.Threading;

namespace System.Text
{
    internal sealed class InternalDecoderBestFitFallback : DecoderFallback
    {
        internal Encoding encoding = null;
        internal char[] arrayBestFit = null;
        internal char cReplacement = '?';
        internal InternalDecoderBestFitFallback(Encoding encoding)
        {
            this.encoding = encoding;
            this.bIsMicrosoftBestFitFallback = true;
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new InternalDecoderBestFitFallbackBuffer(this);
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
            InternalDecoderBestFitFallback that = value as InternalDecoderBestFitFallback;
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

    internal sealed class InternalDecoderBestFitFallbackBuffer : DecoderFallbackBuffer
    {
        internal char cBestFit = '\0';
        internal int iCount = -1;
        internal int iSize;
        private InternalDecoderBestFitFallback oFallback;
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

        public InternalDecoderBestFitFallbackBuffer(InternalDecoderBestFitFallback fallback)
        {
            this.oFallback = fallback;
            if (oFallback.arrayBestFit == null)
            {
                lock (InternalSyncObject)
                {
                    if (oFallback.arrayBestFit == null)
                        oFallback.arrayBestFit = fallback.encoding.GetBestFitBytesToUnicodeData();
                }
            }
        }

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
                        cBestFit = TryBestFit(bytesUnknown);
            if (cBestFit == '\0')
                cBestFit = oFallback.cReplacement;
            iCount = iSize = 1;
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
            byteStart = null;
        }

        internal unsafe override int InternalFallback(byte[] bytes, byte *pBytes)
        {
            return 1;
        }

        private char TryBestFit(byte[] bytesCheck)
        {
            int lowBound = 0;
            int highBound = oFallback.arrayBestFit.Length;
            int index;
            char cCheck;
            if (highBound == 0)
                return '\0';
            if (bytesCheck.Length == 0 || bytesCheck.Length > 2)
                return '\0';
            if (bytesCheck.Length == 1)
                cCheck = unchecked ((char)bytesCheck[0]);
            else
                cCheck = unchecked ((char)((bytesCheck[0] << 8) + bytesCheck[1]));
            if (cCheck < oFallback.arrayBestFit[0] || cCheck > oFallback.arrayBestFit[highBound - 2])
                return '\0';
            int iDiff;
            while ((iDiff = (highBound - lowBound)) > 6)
            {
                index = ((iDiff / 2) + lowBound) & 0xFFFE;
                char cTest = oFallback.arrayBestFit[index];
                if (cTest == cCheck)
                {
                                        return oFallback.arrayBestFit[index + 1];
                }
                else if (cTest < cCheck)
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
                if (oFallback.arrayBestFit[index] == cCheck)
                {
                                        return oFallback.arrayBestFit[index + 1];
                }
            }

            return '\0';
        }
    }
}