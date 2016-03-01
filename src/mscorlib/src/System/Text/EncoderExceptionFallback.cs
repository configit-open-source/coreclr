using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Text
{
    public sealed class EncoderExceptionFallback : EncoderFallback
    {
        public EncoderExceptionFallback()
        {
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return new EncoderExceptionFallbackBuffer();
        }

        public override int MaxCharCount
        {
            get
            {
                return 0;
            }
        }

        public override bool Equals(Object value)
        {
            EncoderExceptionFallback that = value as EncoderExceptionFallback;
            if (that != null)
            {
                return (true);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return 654;
        }
    }

    public sealed class EncoderExceptionFallbackBuffer : EncoderFallbackBuffer
    {
        public EncoderExceptionFallbackBuffer()
        {
        }

        public override bool Fallback(char charUnknown, int index)
        {
            throw new EncoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageConversionIndex", (int)charUnknown, index), charUnknown, index);
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            if (!Char.IsHighSurrogate(charUnknownHigh))
            {
                throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xD800, 0xDBFF));
            }

            if (!Char.IsLowSurrogate(charUnknownLow))
            {
                throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xDC00, 0xDFFF));
            }

            Contract.EndContractBlock();
            int iTemp = Char.ConvertToUtf32(charUnknownHigh, charUnknownLow);
            throw new EncoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageConversionIndex", iTemp, index), charUnknownHigh, charUnknownLow, index);
        }

        public override char GetNextChar()
        {
            return (char)0;
        }

        public override bool MovePrevious()
        {
            return false;
        }

        public override int Remaining
        {
            get
            {
                return 0;
            }
        }
    }

    public sealed class EncoderFallbackException : ArgumentException
    {
        char charUnknown;
        char charUnknownHigh;
        char charUnknownLow;
        int index;
        public EncoderFallbackException(): base (Environment.GetResourceString("Arg_ArgumentException"))
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public EncoderFallbackException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public EncoderFallbackException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        internal EncoderFallbackException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        internal EncoderFallbackException(String message, char charUnknown, int index): base (message)
        {
            this.charUnknown = charUnknown;
            this.index = index;
        }

        internal EncoderFallbackException(String message, char charUnknownHigh, char charUnknownLow, int index): base (message)
        {
            if (!Char.IsHighSurrogate(charUnknownHigh))
            {
                throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xD800, 0xDBFF));
            }

            if (!Char.IsLowSurrogate(charUnknownLow))
            {
                throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", 0xDC00, 0xDFFF));
            }

            Contract.EndContractBlock();
            this.charUnknownHigh = charUnknownHigh;
            this.charUnknownLow = charUnknownLow;
            this.index = index;
        }

        public char CharUnknown
        {
            get
            {
                return (charUnknown);
            }
        }

        public char CharUnknownHigh
        {
            get
            {
                return (charUnknownHigh);
            }
        }

        public char CharUnknownLow
        {
            get
            {
                return (charUnknownLow);
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
        }

        public bool IsUnknownSurrogate()
        {
            return (this.charUnknownHigh != '\0');
        }
    }
}