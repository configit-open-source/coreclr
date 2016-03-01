namespace System
{
    using System;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using Microsoft.Win32;
    using CultureInfo = System.Globalization.CultureInfo;

    internal class OleAutBinder : DefaultBinder
    {
        public override Object ChangeType(Object value, Type type, CultureInfo cultureInfo)
        {
            Variant myValue = new Variant(value);
            if (cultureInfo == null)
                cultureInfo = CultureInfo.CurrentCulture;
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            if (!type.IsPrimitive && type.IsInstanceOfType(value))
            {
                return value;
            }

            Type srcType = value.GetType();
            if (type.IsEnum && srcType.IsPrimitive)
            {
                return Enum.Parse(type, value.ToString());
            }

            try
            {
                Object RetObj = OAVariantLib.ChangeType(myValue, type, OAVariantLib.LocalBool, cultureInfo).ToObject();
                return RetObj;
            }
            catch (NotSupportedException)
            {
                throw new COMException(Environment.GetResourceString("Interop.COM_TypeMismatch"), unchecked ((int)0x80020005));
            }
        }
    }
}