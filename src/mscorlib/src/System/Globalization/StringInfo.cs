
using System.Runtime.Serialization;

namespace System.Globalization
{
    public class StringInfo
    {
        private String m_str;
        private int[] m_indexes;
        public StringInfo(): this ("")
        {
        }

        public StringInfo(String value)
        {
            this.String = value;
        }

        private void OnDeserializing(StreamingContext ctx)
        {
            m_str = String.Empty;
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            if (m_str.Length == 0)
            {
                m_indexes = null;
            }
        }

        public override bool Equals(Object value)
        {
            StringInfo that = value as StringInfo;
            if (that != null)
            {
                return (this.m_str.Equals(that.m_str));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return this.m_str.GetHashCode();
        }

        private int[] Indexes
        {
            get
            {
                if ((null == this.m_indexes) && (0 < this.String.Length))
                {
                    this.m_indexes = StringInfo.ParseCombiningCharacters(this.String);
                }

                return (this.m_indexes);
            }
        }

        public String String
        {
            get
            {
                return (this.m_str);
            }

            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("String", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.m_str = value;
                this.m_indexes = null;
            }
        }

        public int LengthInTextElements
        {
            get
            {
                if (null == this.Indexes)
                {
                    return (0);
                }

                return (this.Indexes.Length);
            }
        }

        public String SubstringByTextElements(int startingTextElement)
        {
            if (null == this.Indexes)
            {
                if (startingTextElement < 0)
                {
                    throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
                }
                else
                {
                    throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
                }
            }

            return (this.SubstringByTextElements(startingTextElement, this.Indexes.Length - startingTextElement));
        }

        public String SubstringByTextElements(int startingTextElement, int lengthInTextElements)
        {
            if (startingTextElement < 0)
            {
                throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (this.String.Length == 0 || startingTextElement >= this.Indexes.Length)
            {
                throw new ArgumentOutOfRangeException("startingTextElement", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
            }

            if (lengthInTextElements < 0)
            {
                throw new ArgumentOutOfRangeException("lengthInTextElements", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (startingTextElement > this.Indexes.Length - lengthInTextElements)
            {
                throw new ArgumentOutOfRangeException("lengthInTextElements", Environment.GetResourceString("Arg_ArgumentOutOfRangeException"));
            }

            int start = this.Indexes[startingTextElement];
            if (startingTextElement + lengthInTextElements == this.Indexes.Length)
            {
                return (this.String.Substring(start));
            }
            else
            {
                return (this.String.Substring(start, (this.Indexes[lengthInTextElements + startingTextElement] - start)));
            }
        }

        public static String GetNextTextElement(String str)
        {
            return (GetNextTextElement(str, 0));
        }

        internal static int GetCurrentTextElementLen(String str, int index, int len, ref UnicodeCategory ucCurrent, ref int currentCharCount)
        {
                                    if (index + currentCharCount == len)
            {
                return (currentCharCount);
            }

            int nextCharCount;
            UnicodeCategory ucNext = CharUnicodeInfo.InternalGetUnicodeCategory(str, index + currentCharCount, out nextCharCount);
            if (CharUnicodeInfo.IsCombiningCategory(ucNext))
            {
                if (CharUnicodeInfo.IsCombiningCategory(ucCurrent) || (ucCurrent == UnicodeCategory.Format) || (ucCurrent == UnicodeCategory.Control) || (ucCurrent == UnicodeCategory.OtherNotAssigned) || (ucCurrent == UnicodeCategory.Surrogate))
                {
                }
                else
                {
                    int startIndex = index;
                    index += currentCharCount + nextCharCount;
                    while (index < len)
                    {
                        ucNext = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out nextCharCount);
                        if (!CharUnicodeInfo.IsCombiningCategory(ucNext))
                        {
                            ucCurrent = ucNext;
                            currentCharCount = nextCharCount;
                            break;
                        }

                        index += nextCharCount;
                    }

                    return (index - startIndex);
                }
            }

            int ret = currentCharCount;
            ucCurrent = ucNext;
            currentCharCount = nextCharCount;
            return (ret);
        }

        public static String GetNextTextElement(String str, int index)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        int len = str.Length;
            if (index < 0 || index >= len)
            {
                if (index == len)
                {
                    return (String.Empty);
                }

                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            int charLen;
            UnicodeCategory uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            return (str.Substring(index, GetCurrentTextElementLen(str, index, len, ref uc, ref charLen)));
        }

        public static TextElementEnumerator GetTextElementEnumerator(String str)
        {
            return (GetTextElementEnumerator(str, 0));
        }

        public static TextElementEnumerator GetTextElementEnumerator(String str, int index)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        int len = str.Length;
            if (index < 0 || (index > len))
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            return (new TextElementEnumerator(str, index, len));
        }

        public static int[] ParseCombiningCharacters(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        int len = str.Length;
            int[] result = new int[len];
            if (len == 0)
            {
                return (result);
            }

            int resultCount = 0;
            int i = 0;
            int currentCharLen;
            UnicodeCategory currentCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, 0, out currentCharLen);
            while (i < len)
            {
                result[resultCount++] = i;
                i += GetCurrentTextElementLen(str, i, len, ref currentCategory, ref currentCharLen);
            }

            if (resultCount < len)
            {
                int[] returnArray = new int[resultCount];
                Array.Copy(result, returnArray, resultCount);
                return (returnArray);
            }

            return (result);
        }
    }
}