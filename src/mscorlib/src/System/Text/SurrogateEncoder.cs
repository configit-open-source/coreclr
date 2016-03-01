namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

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