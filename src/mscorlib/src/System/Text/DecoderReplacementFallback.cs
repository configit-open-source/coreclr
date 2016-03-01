using System.Diagnostics.Contracts;

namespace System.Text
{
    public sealed class DecoderReplacementFallback : DecoderFallback
    {
        private String strDefault;
        public DecoderReplacementFallback(): this ("?")
        {
        }

        public DecoderReplacementFallback(String replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException("replacement");
            Contract.EndContractBlock();
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

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new DecoderReplacementFallbackBuffer(this);
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
            DecoderReplacementFallback that = value as DecoderReplacementFallback;
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

    public sealed class DecoderReplacementFallbackBuffer : DecoderFallbackBuffer
    {
        private String strDefault;
        int fallbackCount = -1;
        int fallbackIndex = -1;
        public DecoderReplacementFallbackBuffer(DecoderReplacementFallback fallback)
        {
            this.strDefault = fallback.DefaultString;
        }

        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            if (fallbackCount >= 1)
            {
                ThrowLastBytesRecursive(bytesUnknown);
            }

            if (strDefault.Length == 0)
                return false;
            fallbackCount = strDefault.Length;
            fallbackIndex = -1;
            return true;
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

            Contract.Assert(fallbackIndex < strDefault.Length && fallbackIndex >= 0, "Index exceeds buffer range");
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
            fallbackIndex = -1;
            byteStart = null;
        }

        internal unsafe override int InternalFallback(byte[] bytes, byte *pBytes)
        {
            return strDefault.Length;
        }
    }
}