using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Resources
{
    internal sealed class FastResourceComparer : IComparer, IEqualityComparer, IComparer<String>, IEqualityComparer<String>
    {
        internal static readonly FastResourceComparer Default = new FastResourceComparer();
        public int GetHashCode(Object key)
        {
            String s = (String)key;
            return FastResourceComparer.HashFunction(s);
        }

        public int GetHashCode(String key)
        {
            return FastResourceComparer.HashFunction(key);
        }

        internal static int HashFunction(String key)
        {
            uint hash = 5381;
            for (int i = 0; i < key.Length; i++)
                hash = ((hash << 5) + hash) ^ key[i];
            return (int)hash;
        }

        public int Compare(Object a, Object b)
        {
            if (a == b)
                return 0;
            String sa = (String)a;
            String sb = (String)b;
            return String.CompareOrdinal(sa, sb);
        }

        public int Compare(String a, String b)
        {
            return String.CompareOrdinal(a, b);
        }

        public bool Equals(String a, String b)
        {
            return String.Equals(a, b);
        }

        public new bool Equals(Object a, Object b)
        {
            if (a == b)
                return true;
            String sa = (String)a;
            String sb = (String)b;
            return String.Equals(sa, sb);
        }

        public unsafe static int CompareOrdinal(String a, byte[] bytes, int bCharLength)
        {
            Contract.Assert(a != null && bytes != null, "FastResourceComparer::CompareOrdinal must have non-null params");
            Contract.Assert(bCharLength * 2 <= bytes.Length, "FastResourceComparer::CompareOrdinal - numChars is too big!");
            int i = 0;
            int r = 0;
            int numChars = a.Length;
            if (numChars > bCharLength)
                numChars = bCharLength;
            if (bCharLength == 0)
                return (a.Length == 0) ? 0 : -1;
            fixed (byte *pb = bytes)
            {
                byte *pChar = pb;
                while (i < numChars && r == 0)
                {
                    int b = pChar[0] | pChar[1] << 8;
                    r = a[i++] - b;
                    pChar += sizeof (char);
                }
            }

            if (r != 0)
                return r;
            return a.Length - bCharLength;
        }

        public static int CompareOrdinal(byte[] bytes, int aCharLength, String b)
        {
            return -CompareOrdinal(b, bytes, aCharLength);
        }

        internal unsafe static int CompareOrdinal(byte *a, int byteLen, String b)
        {
            Contract.Assert((byteLen & 1) == 0, "CompareOrdinal is expecting a UTF-16 string length, which must be even!");
            Contract.Assert(a != null && b != null, "Null args not allowed.");
            Contract.Assert(byteLen >= 0, "byteLen must be non-negative.");
            int r = 0;
            int i = 0;
            int numChars = byteLen >> 1;
            if (numChars > b.Length)
                numChars = b.Length;
            while (i < numChars && r == 0)
            {
                char aCh = (char)(*a++ | (*a++ << 8));
                r = aCh - b[i++];
            }

            if (r != 0)
                return r;
            return byteLen - b.Length * 2;
        }
    }
}