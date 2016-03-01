using System.Diagnostics.Contracts;

namespace System
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;

    internal sealed class Empty : ISerializable
    {
        private Empty()
        {
        }

        public static readonly Empty Value = new Empty();
        public override String ToString()
        {
            return String.Empty;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.EmptyUnity, null, null);
        }
    }
}