using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public class SortKey
    {
        internal String localeName;
        internal int win32LCID;
        internal CompareOptions options;
        internal String m_String;
        internal byte[] m_KeyData;
        internal SortKey(String localeName, String str, CompareOptions options, byte[] keyData)
        {
            this.m_KeyData = keyData;
            this.localeName = localeName;
            this.options = options;
            this.m_String = str;
        }

        public virtual String OriginalString
        {
            get
            {
                return (m_String);
            }
        }

        public virtual byte[] KeyData
        {
            get
            {
                return (byte[])(m_KeyData.Clone());
            }
        }

        public static int Compare(SortKey sortkey1, SortKey sortkey2)
        {
            if (sortkey1 == null || sortkey2 == null)
            {
                throw new ArgumentNullException((sortkey1 == null ? "sortkey1" : "sortkey2"));
            }

            Contract.EndContractBlock();
            byte[] key1Data = sortkey1.m_KeyData;
            byte[] key2Data = sortkey2.m_KeyData;
            Contract.Assert(key1Data != null, "key1Data!=null");
            Contract.Assert(key2Data != null, "key2Data!=null");
            if (key1Data.Length == 0)
            {
                if (key2Data.Length == 0)
                {
                    return (0);
                }

                return (-1);
            }

            if (key2Data.Length == 0)
            {
                return (1);
            }

            int compLen = (key1Data.Length < key2Data.Length) ? key1Data.Length : key2Data.Length;
            for (int i = 0; i < compLen; i++)
            {
                if (key1Data[i] > key2Data[i])
                {
                    return (1);
                }

                if (key1Data[i] < key2Data[i])
                {
                    return (-1);
                }
            }

            return 0;
        }

        public override bool Equals(Object value)
        {
            SortKey that = value as SortKey;
            if (that != null)
            {
                return Compare(this, that) == 0;
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (CompareInfo.GetCompareInfo(this.localeName).GetHashCodeOfString(this.m_String, this.options));
        }

        public override String ToString()
        {
            return ("SortKey - " + localeName + ", " + options + ", " + m_String);
        }
    }
}