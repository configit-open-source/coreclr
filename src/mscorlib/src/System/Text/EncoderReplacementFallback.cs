

namespace System.Text
{
    public sealed class EncoderReplacementFallback : EncoderFallback
    {
        private String strDefault;
        public EncoderReplacementFallback(): this ("?")
        {
        }

        public EncoderReplacementFallback(String replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException("replacement");
                        bool bFoundHigh = false;
            for (int i = 0; i < replacement.Length; i++)
            {
                if (Char.IsSurrogate(replacement, i))
                {
                    if (Char.IsHighSurrogate(replacement, i))
                    {
                        if (bFoundHigh)
                            break;
                        bFoundHigh = true;
                    }
                    else
                    {
                        if (!bFoundHigh)
                        {
                            bFoundHigh = true;
                            break;
                        }

                        bFoundHigh = false;
                    }
                }
                else if (bFoundHigh)
                    break;
            }

            if (bFoundHigh)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex", "replacement"));
            strDefault = replacement;
        }

        public String DefaultString
        {
            get
            {
                return strDefault;
            }
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new EncoderReplacementFallbackBuffer(this);
        }

        public override int MaxCharCount
        {
            get
            {
                return strDefault.Length;
            }
        }

        public override bool Equals(Object value)
        {
            EncoderReplacementFallback that = value as EncoderReplacementFallback;
            if (that != null)
            {
                return (this.strDefault == that.strDefault);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return strDefault.GetHashCode();
        }
    }

    public sealed class EncoderReplacementFallbackBuffer : EncoderFallbackBuffer
    {
        private String strDefault;
        int fallbackCount = -1;
        int fallbackIndex = -1;
        public EncoderReplacementFallbackBuffer(EncoderReplacementFallback fallback)
        {
            this.strDefault = fallback.DefaultString + fallback.DefaultString;
        }

        public override bool Fallback(char charUnknown, int index)
        {
            if (fallbackCount >= 1)
            {
                if (char.IsHighSurrogate(charUnknown) && fallbackCount >= 0 && char.IsLowSurrogate(strDefault[fallbackIndex + 1]))
                    ThrowLastCharRecursive(Char.ConvertToUtf32(charUnknown, strDefault[fallbackIndex + 1]));
                ThrowLastCharRecursive(unchecked ((int)charUnknown));
            }

            fallbackCount = strDefault.Length / 2;
            fallbackIndex = -1;
            return fallbackCount != 0;
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            if (!Char.IsHighSurrogate(charUnknownHigh))
                throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xD800, 0xDBFF));
            if (!Char.IsLowSurrogate(charUnknownLow))
                throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xDC00, 0xDFFF));
                        if (fallbackCount >= 1)
                ThrowLastCharRecursive(Char.ConvertToUtf32(charUnknownHigh, charUnknownLow));
            fallbackCount = strDefault.Length;
            fallbackIndex = -1;
            return fallbackCount != 0;
        }

        public override char GetNextChar()
        {
            fallbackCount--;
            fallbackIndex++;
            if (fallbackCount < 0)
                return '\0';
            if (fallbackCount == int.MaxValue)
            {
                fallbackCount = -1;
                return '\0';
            }

                        return strDefault[fallbackIndex];
        }

        public override bool MovePrevious()
        {
            if (fallbackCount >= -1 && fallbackIndex >= 0)
            {
                fallbackIndex--;
                fallbackCount++;
                return true;
            }

            return false;
        }

        public override int Remaining
        {
            get
            {
                return (fallbackCount < 0) ? 0 : fallbackCount;
            }
        }

        public override unsafe void Reset()
        {
            fallbackCount = -1;
            fallbackIndex = 0;
            charStart = null;
            bFallingBack = false;
        }
    }
}