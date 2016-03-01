namespace Microsoft.Win32
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using CultureInfo = System.Globalization.CultureInfo;

    internal static class OAVariantLib
    {
        public const int NoValueProp = 0x01;
        public const int AlphaBool = 0x02;
        public const int NoUserOverride = 0x04;
        public const int CalendarHijri = 0x08;
        public const int LocalBool = 0x10;
        internal static readonly Type[] ClassTypes = {typeof (Empty), typeof (void), typeof (Boolean), typeof (Char), typeof (SByte), typeof (Byte), typeof (Int16), typeof (UInt16), typeof (Int32), typeof (UInt32), typeof (Int64), typeof (UInt64), typeof (Single), typeof (Double), typeof (String), typeof (void), typeof (DateTime), typeof (TimeSpan), typeof (Object), typeof (Decimal), null, typeof (Missing), typeof (DBNull), };
        private const int CV_OBJECT = 0x12;
        internal static Variant ChangeType(Variant source, Type targetClass, short options, CultureInfo culture)
        {
            if (targetClass == null)
                throw new ArgumentNullException("targetClass");
            if (culture == null)
                throw new ArgumentNullException("culture");
            Variant result = new Variant();
            ChangeTypeEx(ref result, ref source, 0, targetClass.TypeHandle.Value, GetCVTypeFromClass(targetClass), options);
            return result;
        }

        private static int GetCVTypeFromClass(Type ctype)
        {
            Contract.Requires(ctype != null);
            BCLDebug.Assert(ClassTypes[CV_OBJECT] == typeof (Object), "OAVariantLib::ClassTypes[CV_OBJECT] == Object.class");
            int cvtype = -1;
            for (int i = 0; i < ClassTypes.Length; i++)
            {
                if (ctype.Equals(ClassTypes[i]))
                {
                    cvtype = i;
                    break;
                }
            }

            if (cvtype == -1)
                cvtype = CV_OBJECT;
            return cvtype;
        }

        private static extern void ChangeTypeEx(ref Variant result, ref Variant source, int lcid, IntPtr typeHandle, int cvType, short flags);
    }
}