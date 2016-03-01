namespace System.Globalization
{
    internal class CodePageDataItem
    {
        internal int m_dataIndex;
        internal int m_uiFamilyCodePage;
        internal String m_webName;
        internal String m_headerName;
        internal String m_bodyName;
        internal uint m_flags;
        unsafe internal CodePageDataItem(int dataIndex)
        {
            m_dataIndex = dataIndex;
            m_uiFamilyCodePage = EncodingTable.codePageDataPtr[dataIndex].uiFamilyCodePage;
            m_flags = EncodingTable.codePageDataPtr[dataIndex].flags;
        }

        unsafe internal static String CreateString(sbyte *pStrings, uint index)
        {
            if (pStrings[0] == '|')
            {
                int start = 1;
                for (int i = 1; true; i++)
                {
                    sbyte ch = pStrings[i];
                    if ((ch == '|') || (ch == 0))
                    {
                        if (index == 0)
                        {
                            return new String(pStrings, start, i - start);
                        }

                        index--;
                        start = i + 1;
                        if (ch == 0)
                        {
                            break;
                        }
                    }
                }

                throw new ArgumentException("pStrings");
            }
            else
            {
                return new String(pStrings);
            }
        }

        unsafe public String WebName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_webName == null)
                {
                    m_webName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 0);
                }

                return m_webName;
            }
        }

        public virtual int UIFamilyCodePage
        {
            get
            {
                return m_uiFamilyCodePage;
            }
        }

        unsafe public String HeaderName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_headerName == null)
                {
                    m_headerName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 1);
                }

                return m_headerName;
            }
        }

        unsafe public String BodyName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_bodyName == null)
                {
                    m_bodyName = CreateString(EncodingTable.codePageDataPtr[m_dataIndex].Names, 2);
                }

                return m_bodyName;
            }
        }

        unsafe public uint Flags
        {
            get
            {
                return (m_flags);
            }
        }
    }
}