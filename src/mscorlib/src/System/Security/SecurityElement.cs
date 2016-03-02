using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Security
{
    internal enum SecurityElementType
    {
        Regular = 0,
        Format = 1,
        Comment = 2
    }

    internal interface ISecurityElementFactory
    {
        SecurityElement CreateSecurityElement();
        Object Copy();
        String GetTag();
        String Attribute(String attributeName);
    }

    sealed public class SecurityElement : ISecurityElementFactory
    {
        internal String m_strTag;
        internal String m_strText;
        private ArrayList m_lChildren;
        internal ArrayList m_lAttributes;
        internal SecurityElementType m_type = SecurityElementType.Regular;
        private static readonly char[] s_tagIllegalCharacters = new char[]{' ', '<', '>'};
        private static readonly char[] s_textIllegalCharacters = new char[]{'<', '>'};
        private static readonly char[] s_valueIllegalCharacters = new char[]{'<', '>', '\"'};
        private const String s_strIndent = "   ";
        private const int c_AttributesTypical = 4 * 2;
        private const int c_ChildrenTypical = 1;
        private static readonly String[] s_escapeStringPairs = new String[]{"<", "&lt;", ">", "&gt;", "\"", "&quot;", "\'", "&apos;", "&", "&amp;"};
        private static readonly char[] s_escapeChars = new char[]{'<', '>', '\"', '\'', '&'};
        internal SecurityElement()
        {
        }

        SecurityElement ISecurityElementFactory.CreateSecurityElement()
        {
            return this;
        }

        String ISecurityElementFactory.GetTag()
        {
            return ((SecurityElement)this).Tag;
        }

        Object ISecurityElementFactory.Copy()
        {
            return ((SecurityElement)this).Copy();
        }

        String ISecurityElementFactory.Attribute(String attributeName)
        {
            return ((SecurityElement)this).Attribute(attributeName);
        }

        public SecurityElement(String tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            if (!IsValidTag(tag))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementTag"), tag));
                        m_strTag = tag;
            m_strText = null;
        }

        public SecurityElement(String tag, String text)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
            if (!IsValidTag(tag))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementTag"), tag));
            if (text != null && !IsValidText(text))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementText"), text));
                        m_strTag = tag;
            m_strText = text;
        }

        public String Tag
        {
            
            get
            {
                return m_strTag;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Tag");
                if (!IsValidTag(value))
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementTag"), value));
                                m_strTag = value;
            }
        }

        public Hashtable Attributes
        {
            get
            {
                if (m_lAttributes == null || m_lAttributes.Count == 0)
                {
                    return null;
                }
                else
                {
                    Hashtable hashtable = new Hashtable(m_lAttributes.Count / 2);
                    int iMax = m_lAttributes.Count;
                                        for (int i = 0; i < iMax; i += 2)
                    {
                        hashtable.Add(m_lAttributes[i], m_lAttributes[i + 1]);
                    }

                    return hashtable;
                }
            }

            set
            {
                if (value == null || value.Count == 0)
                {
                    m_lAttributes = null;
                }
                else
                {
                    ArrayList list = new ArrayList(value.Count);
                    System.Collections.IDictionaryEnumerator enumerator = (System.Collections.IDictionaryEnumerator)value.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        String attrName = (String)enumerator.Key;
                        String attrValue = (String)enumerator.Value;
                        if (!IsValidAttributeName(attrName))
                            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementName"), (String)enumerator.Current));
                        if (!IsValidAttributeValue(attrValue))
                            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementValue"), (String)enumerator.Value));
                        list.Add(attrName);
                        list.Add(attrValue);
                    }

                    m_lAttributes = list;
                }
            }
        }

        public String Text
        {
            get
            {
                return Unescape(m_strText);
            }

            set
            {
                if (value == null)
                {
                    m_strText = null;
                }
                else
                {
                    if (!IsValidText(value))
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementTag"), value));
                    m_strText = value;
                }
            }
        }

        public ArrayList Children
        {
            get
            {
                ConvertSecurityElementFactories();
                return m_lChildren;
            }

            set
            {
                if (value != null)
                {
                    IEnumerator enumerator = value.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == null)
                            throw new ArgumentException(Environment.GetResourceString("ArgumentNull_Child"));
                    }
                }

                m_lChildren = value;
            }
        }

        internal void ConvertSecurityElementFactories()
        {
            if (m_lChildren == null)
                return;
            for (int i = 0; i < m_lChildren.Count; ++i)
            {
                ISecurityElementFactory iseFactory = m_lChildren[i] as ISecurityElementFactory;
                if (iseFactory != null && !(m_lChildren[i] is SecurityElement))
                    m_lChildren[i] = iseFactory.CreateSecurityElement();
            }
        }

        internal ArrayList InternalChildren
        {
            get
            {
                return m_lChildren;
            }
        }

        internal void AddAttributeSafe(String name, String value)
        {
            if (m_lAttributes == null)
            {
                m_lAttributes = new ArrayList(c_AttributesTypical);
            }
            else
            {
                int iMax = m_lAttributes.Count;
                                for (int i = 0; i < iMax; i += 2)
                {
                    String strAttrName = (String)m_lAttributes[i];
                    if (String.Equals(strAttrName, name))
                        throw new ArgumentException(Environment.GetResourceString("Argument_AttributeNamesMustBeUnique"));
                }
            }

            m_lAttributes.Add(name);
            m_lAttributes.Add(value);
        }

        public void AddAttribute(String name, String value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");
            if (!IsValidAttributeName(name))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementName"), name));
            if (!IsValidAttributeValue(value))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidElementValue"), value));
                        AddAttributeSafe(name, value);
        }

        public void AddChild(SecurityElement child)
        {
            if (child == null)
                throw new ArgumentNullException("child");
                        if (m_lChildren == null)
                m_lChildren = new ArrayList(c_ChildrenTypical);
            m_lChildren.Add(child);
        }

        internal void AddChild(ISecurityElementFactory child)
        {
            if (child == null)
                throw new ArgumentNullException("child");
                        if (m_lChildren == null)
                m_lChildren = new ArrayList(c_ChildrenTypical);
            m_lChildren.Add(child);
        }

        internal void AddChildNoDuplicates(ISecurityElementFactory child)
        {
            if (child == null)
                throw new ArgumentNullException("child");
                        if (m_lChildren == null)
            {
                m_lChildren = new ArrayList(c_ChildrenTypical);
                m_lChildren.Add(child);
            }
            else
            {
                for (int i = 0; i < m_lChildren.Count; ++i)
                {
                    if (m_lChildren[i] == child)
                        return;
                }

                m_lChildren.Add(child);
            }
        }

        public bool Equal(SecurityElement other)
        {
            if (other == null)
                return false;
            if (!String.Equals(m_strTag, other.m_strTag))
                return false;
            if (!String.Equals(m_strText, other.m_strText))
                return false;
            if (m_lAttributes == null || other.m_lAttributes == null)
            {
                if (m_lAttributes != other.m_lAttributes)
                    return false;
            }
            else
            {
                int iMax = m_lAttributes.Count;
                                if (iMax != other.m_lAttributes.Count)
                    return false;
                for (int i = 0; i < iMax; i++)
                {
                    String lhs = (String)m_lAttributes[i];
                    String rhs = (String)other.m_lAttributes[i];
                    if (!String.Equals(lhs, rhs))
                        return false;
                }
            }

            if (m_lChildren == null || other.m_lChildren == null)
            {
                if (m_lChildren != other.m_lChildren)
                    return false;
            }
            else
            {
                if (m_lChildren.Count != other.m_lChildren.Count)
                    return false;
                this.ConvertSecurityElementFactories();
                other.ConvertSecurityElementFactories();
                IEnumerator lhs = m_lChildren.GetEnumerator();
                IEnumerator rhs = other.m_lChildren.GetEnumerator();
                SecurityElement e1, e2;
                while (lhs.MoveNext())
                {
                    rhs.MoveNext();
                    e1 = (SecurityElement)lhs.Current;
                    e2 = (SecurityElement)rhs.Current;
                    if (e1 == null || !e1.Equal(e2))
                        return false;
                }
            }

            return true;
        }

        public SecurityElement Copy()
        {
            SecurityElement element = new SecurityElement(this.m_strTag, this.m_strText);
            element.m_lChildren = this.m_lChildren == null ? null : new ArrayList(this.m_lChildren);
            element.m_lAttributes = this.m_lAttributes == null ? null : new ArrayList(this.m_lAttributes);
            return element;
        }

        public static bool IsValidTag(String tag)
        {
            if (tag == null)
                return false;
            return tag.IndexOfAny(s_tagIllegalCharacters) == -1;
        }

        public static bool IsValidText(String text)
        {
            if (text == null)
                return false;
            return text.IndexOfAny(s_textIllegalCharacters) == -1;
        }

        public static bool IsValidAttributeName(String name)
        {
            return IsValidTag(name);
        }

        public static bool IsValidAttributeValue(String value)
        {
            if (value == null)
                return false;
            return value.IndexOfAny(s_valueIllegalCharacters) == -1;
        }

        private static String GetEscapeSequence(char c)
        {
            int iMax = s_escapeStringPairs.Length;
                        for (int i = 0; i < iMax; i += 2)
            {
                String strEscSeq = s_escapeStringPairs[i];
                String strEscValue = s_escapeStringPairs[i + 1];
                if (strEscSeq[0] == c)
                    return strEscValue;
            }

                        return c.ToString();
        }

        public static String Escape(String str)
        {
            if (str == null)
                return null;
            StringBuilder sb = null;
            int strLen = str.Length;
            int index;
            int newIndex = 0;
            do
            {
                index = str.IndexOfAny(s_escapeChars, newIndex);
                if (index == -1)
                {
                    if (sb == null)
                        return str;
                    else
                    {
                        sb.Append(str, newIndex, strLen - newIndex);
                        return sb.ToString();
                    }
                }
                else
                {
                    if (sb == null)
                        sb = new StringBuilder();
                    sb.Append(str, newIndex, index - newIndex);
                    sb.Append(GetEscapeSequence(str[index]));
                    newIndex = (index + 1);
                }
            }
            while (true);
        }

        private static String GetUnescapeSequence(String str, int index, out int newIndex)
        {
            int maxCompareLength = str.Length - index;
            int iMax = s_escapeStringPairs.Length;
                        for (int i = 0; i < iMax; i += 2)
            {
                String strEscSeq = s_escapeStringPairs[i];
                String strEscValue = s_escapeStringPairs[i + 1];
                int length = strEscValue.Length;
                if (length <= maxCompareLength && String.Compare(strEscValue, 0, str, index, length, StringComparison.Ordinal) == 0)
                {
                    newIndex = index + strEscValue.Length;
                    return strEscSeq;
                }
            }

            newIndex = index + 1;
            return str[index].ToString();
        }

        private static String Unescape(String str)
        {
            if (str == null)
                return null;
            StringBuilder sb = null;
            int strLen = str.Length;
            int index;
            int newIndex = 0;
            do
            {
                index = str.IndexOf('&', newIndex);
                if (index == -1)
                {
                    if (sb == null)
                        return str;
                    else
                    {
                        sb.Append(str, newIndex, strLen - newIndex);
                        return sb.ToString();
                    }
                }
                else
                {
                    if (sb == null)
                        sb = new StringBuilder();
                    sb.Append(str, newIndex, index - newIndex);
                    sb.Append(GetUnescapeSequence(str, index, out newIndex));
                }
            }
            while (true);
        }

        private delegate void ToStringHelperFunc(Object obj, String str);
        private static void ToStringHelperStringBuilder(Object obj, String str)
        {
            ((StringBuilder)obj).Append(str);
        }

        private static void ToStringHelperStreamWriter(Object obj, String str)
        {
            ((StreamWriter)obj).Write(str);
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString("", sb, new ToStringHelperFunc(ToStringHelperStringBuilder));
            return sb.ToString();
        }

        internal void ToWriter(StreamWriter writer)
        {
            ToString("", writer, new ToStringHelperFunc(ToStringHelperStreamWriter));
        }

        private void ToString(String indent, Object obj, ToStringHelperFunc func)
        {
            func(obj, "<");
            switch (m_type)
            {
                case SecurityElementType.Format:
                    func(obj, "?");
                    break;
                case SecurityElementType.Comment:
                    func(obj, "!");
                    break;
                default:
                    break;
            }

            func(obj, m_strTag);
            if (m_lAttributes != null && m_lAttributes.Count > 0)
            {
                func(obj, " ");
                int iMax = m_lAttributes.Count;
                                for (int i = 0; i < iMax; i += 2)
                {
                    String strAttrName = (String)m_lAttributes[i];
                    String strAttrValue = (String)m_lAttributes[i + 1];
                    func(obj, strAttrName);
                    func(obj, "=\"");
                    func(obj, strAttrValue);
                    func(obj, "\"");
                    if (i != m_lAttributes.Count - 2)
                    {
                        if (m_type == SecurityElementType.Regular)
                        {
                            func(obj, Environment.NewLine);
                        }
                        else
                        {
                            func(obj, " ");
                        }
                    }
                }
            }

            if (m_strText == null && (m_lChildren == null || m_lChildren.Count == 0))
            {
                switch (m_type)
                {
                    case SecurityElementType.Comment:
                        func(obj, ">");
                        break;
                    case SecurityElementType.Format:
                        func(obj, " ?>");
                        break;
                    default:
                        func(obj, "/>");
                        break;
                }

                func(obj, Environment.NewLine);
            }
            else
            {
                func(obj, ">");
                func(obj, m_strText);
                if (m_lChildren != null)
                {
                    this.ConvertSecurityElementFactories();
                    func(obj, Environment.NewLine);
                    for (int i = 0; i < m_lChildren.Count; ++i)
                    {
                        ((SecurityElement)m_lChildren[i]).ToString("", obj, func);
                    }
                }

                func(obj, "</");
                func(obj, m_strTag);
                func(obj, ">");
                func(obj, Environment.NewLine);
            }
        }

        public String Attribute(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        if (m_lAttributes == null)
                return null;
            int iMax = m_lAttributes.Count;
                        for (int i = 0; i < iMax; i += 2)
            {
                String strAttrName = (String)m_lAttributes[i];
                if (String.Equals(strAttrName, name))
                {
                    String strAttrValue = (String)m_lAttributes[i + 1];
                    return Unescape(strAttrValue);
                }
            }

            return null;
        }

        public SecurityElement SearchForChildByTag(String tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
                        if (m_lChildren == null)
                return null;
            IEnumerator enumerator = m_lChildren.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SecurityElement current = (SecurityElement)enumerator.Current;
                if (current != null && String.Equals(current.Tag, tag))
                    return current;
            }

            return null;
        }

        internal String SearchForTextOfLocalName(String strLocalName)
        {
            if (strLocalName == null)
                throw new ArgumentNullException("strLocalName");
                        if (m_strTag == null)
                return null;
            if (m_strTag.Equals(strLocalName) || m_strTag.EndsWith(":" + strLocalName, StringComparison.Ordinal))
                return Unescape(m_strText);
            if (m_lChildren == null)
                return null;
            IEnumerator enumerator = m_lChildren.GetEnumerator();
            while (enumerator.MoveNext())
            {
                String current = ((SecurityElement)enumerator.Current).SearchForTextOfLocalName(strLocalName);
                if (current != null)
                    return current;
            }

            return null;
        }

        public String SearchForTextOfTag(String tag)
        {
            if (tag == null)
                throw new ArgumentNullException("tag");
                        if (String.Equals(m_strTag, tag))
                return Unescape(m_strText);
            if (m_lChildren == null)
                return null;
            IEnumerator enumerator = m_lChildren.GetEnumerator();
            this.ConvertSecurityElementFactories();
            while (enumerator.MoveNext())
            {
                String current = ((SecurityElement)enumerator.Current).SearchForTextOfTag(tag);
                if (current != null)
                    return current;
            }

            return null;
        }
    }
}