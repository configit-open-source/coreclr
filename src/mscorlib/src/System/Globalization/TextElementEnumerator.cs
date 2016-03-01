using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Globalization
{
    public class TextElementEnumerator : IEnumerator
    {
        private String str;
        private int index;
        private int startIndex;
        private int strLen;
        private int currTextElementLen;
        private UnicodeCategory uc;
        private int charLen;
        internal TextElementEnumerator(String str, int startIndex, int strLen)
        {
            Contract.Assert(str != null, "TextElementEnumerator(): str != null");
            Contract.Assert(startIndex >= 0 && strLen >= 0, "TextElementEnumerator(): startIndex >= 0 && strLen >= 0");
            Contract.Assert(strLen >= startIndex, "TextElementEnumerator(): strLen >= startIndex");
            this.str = str;
            this.startIndex = startIndex;
            this.strLen = strLen;
            Reset();
        }

        private int endIndex;
        private int nextTextElementLen;
        private void OnDeserializing(StreamingContext ctx)
        {
            charLen = -1;
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            strLen = endIndex + 1;
            currTextElementLen = nextTextElementLen;
            if (charLen == -1)
            {
                uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
            endIndex = strLen - 1;
            nextTextElementLen = currTextElementLen;
        }

        public bool MoveNext()
        {
            if (index >= strLen)
            {
                index = strLen + 1;
                return (false);
            }

            currTextElementLen = StringInfo.GetCurrentTextElementLen(str, index, strLen, ref uc, ref charLen);
            index += currTextElementLen;
            return (true);
        }

        public Object Current
        {
            get
            {
                return (GetTextElement());
            }
        }

        public String GetTextElement()
        {
            if (index == startIndex)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
            }

            if (index > strLen)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
            }

            return (str.Substring(index - currTextElementLen, currTextElementLen));
        }

        public int ElementIndex
        {
            get
            {
                if (index == startIndex)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
                }

                return (index - currTextElementLen);
            }
        }

        public void Reset()
        {
            index = startIndex;
            if (index < strLen)
            {
                uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            }
        }
    }
}