namespace System
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    public sealed class DBNull : ISerializable, IConvertible
    {
        private DBNull()
        {
        }

        private DBNull(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DBNullSerial"));
        }

        public static readonly DBNull Value = new DBNull();
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.NullUnity, null, null);
        }

        public override String ToString()
        {
            return String.Empty;
        }

        public String ToString(IFormatProvider provider)
        {
            return String.Empty;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.DBNull;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}