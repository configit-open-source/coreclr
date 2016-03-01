using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System
{
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