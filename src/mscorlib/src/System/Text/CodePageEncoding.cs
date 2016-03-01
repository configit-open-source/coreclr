using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Text
{
    internal sealed class CodePageEncoding : ISerializable, IObjectReference
    {
        private int m_codePage;
        private bool m_isReadOnly;
        private bool m_deserializedFromEverett = false;
        private EncoderFallback encoderFallback = null;
        private DecoderFallback decoderFallback = null;
        private Encoding realEncoding = null;
        internal CodePageEncoding(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            this.m_codePage = (int)info.GetValue("m_codePage", typeof (int));
            try
            {
                this.m_isReadOnly = (bool)info.GetValue("m_isReadOnly", typeof (bool));
                this.encoderFallback = (EncoderFallback)info.GetValue("encoderFallback", typeof (EncoderFallback));
                this.decoderFallback = (DecoderFallback)info.GetValue("decoderFallback", typeof (DecoderFallback));
            }
            catch (SerializationException)
            {
                this.m_deserializedFromEverett = true;
                this.m_isReadOnly = true;
            }
        }

        public Object GetRealObject(StreamingContext context)
        {
            this.realEncoding = Encoding.GetEncoding(this.m_codePage);
            if (!this.m_deserializedFromEverett && !this.m_isReadOnly)
            {
                this.realEncoding = (Encoding)this.realEncoding.Clone();
                this.realEncoding.EncoderFallback = this.encoderFallback;
                this.realEncoding.DecoderFallback = this.decoderFallback;
            }

            return this.realEncoding;
        }

        internal sealed class Decoder : ISerializable, IObjectReference
        {
            private Encoding realEncoding = null;
            internal Decoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.realEncoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
            }

            public Object GetRealObject(StreamingContext context)
            {
                return this.realEncoding.GetDecoder();
            }
        }
    }
}