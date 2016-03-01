namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Globalization;

    public sealed class DecoderExceptionFallback : DecoderFallback
    {
        public DecoderExceptionFallback()
        {
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new DecoderExceptionFallbackBuffer();
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
            DecoderExceptionFallback that = value as DecoderExceptionFallback;
            if (that != null)
            {
                return (true);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return 879;
        }
    }

    public sealed class DecoderExceptionFallbackBuffer : DecoderFallbackBuffer
    {
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            Throw(bytesUnknown, index);
            return true;
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

        private void Throw(byte[] bytesUnknown, int index)
        {
            StringBuilder strBytes = new StringBuilder(bytesUnknown.Length * 3);
            int i;
            for (i = 0; i < bytesUnknown.Length && i < 20; i++)
            {
                strBytes.Append("[");
                strBytes.Append(bytesUnknown[i].ToString("X2", CultureInfo.InvariantCulture));
                strBytes.Append("]");
            }

            if (i == 20)
                strBytes.Append(" ...");
            throw new DecoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageBytesIndex", strBytes, index), bytesUnknown, index);
        }
    }

    public sealed class DecoderFallbackException : ArgumentException
    {
        byte[] bytesUnknown = null;
        int index = 0;
        public DecoderFallbackException(): base (Environment.GetResourceString("Arg_ArgumentException"))
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public DecoderFallbackException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public DecoderFallbackException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        internal DecoderFallbackException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public DecoderFallbackException(String message, byte[] bytesUnknown, int index): base (message)
        {
            this.bytesUnknown = bytesUnknown;
            this.index = index;
        }

        public byte[] BytesUnknown
        {
            get
            {
                return (bytesUnknown);
            }
        }

        public int Index
        {
            get
            {
                return this.index;
            }
        }
    }
}