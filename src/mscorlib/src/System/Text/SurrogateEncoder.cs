using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Text
{
    internal sealed class SurrogateEncoder : ISerializable, IObjectReference
    {
        private Encoding realEncoding = null;
        internal SurrogateEncoder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            this.realEncoding = (Encoding)info.GetValue("m_encoding", typeof (Encoding));
        }

        public Object GetRealObject(StreamingContext context)
        {
            return this.realEncoding.GetEncoder();
        }
    }
}