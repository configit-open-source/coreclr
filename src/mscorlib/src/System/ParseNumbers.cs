namespace System
{
    internal static class ParseNumbers
    {
        internal const int PrintAsI1 = 0x40;
        internal const int PrintAsI2 = 0x80;
        internal const int PrintAsI4 = 0x100;
        internal const int TreatAsUnsigned = 0x200;
        internal const int TreatAsI1 = 0x400;
        internal const int TreatAsI2 = 0x800;
        internal const int IsTight = 0x1000;
        internal const int NoSpace = 0x2000;
        public unsafe static long StringToLong(System.String s, int radix, int flags)
        {
            return StringToLong(s, radix, flags, null);
        }

        public unsafe extern static long StringToLong(System.String s, int radix, int flags, int *currPos);
        public unsafe static long StringToLong(System.String s, int radix, int flags, ref int currPos)
        {
            fixed (int *ppos = &currPos)
            {
                return StringToLong(s, radix, flags, ppos);
            }
        }

        public unsafe static int StringToInt(System.String s, int radix, int flags)
        {
            return StringToInt(s, radix, flags, null);
        }

        public unsafe extern static int StringToInt(System.String s, int radix, int flags, int *currPos);
        public unsafe static int StringToInt(System.String s, int radix, int flags, ref int currPos)
        {
            fixed (int *ppos = &currPos)
            {
                return StringToInt(s, radix, flags, ppos);
            }
        }

        public extern static String IntToString(int l, int radix, int width, char paddingChar, int flags);
        public extern static String LongToString(long l, int radix, int width, char paddingChar, int flags);
    }
}