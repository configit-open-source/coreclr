namespace System.Security.Util
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    internal class SiteString
    {
        protected String m_site;
        protected ArrayList m_separatedSite;
        protected static char[] m_separators = {'.'};
        protected internal SiteString()
        {
        }

        public SiteString(String site)
        {
            m_separatedSite = CreateSeparatedSite(site);
            m_site = site;
        }

        private SiteString(String site, ArrayList separatedSite)
        {
            m_separatedSite = separatedSite;
            m_site = site;
        }

        private static ArrayList CreateSeparatedSite(String site)
        {
            if (site == null || site.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
            }

            Contract.EndContractBlock();
            ArrayList list = new ArrayList();
            int braIndex = -1;
            int ketIndex = -1;
            braIndex = site.IndexOf('[');
            if (braIndex == 0)
                ketIndex = site.IndexOf(']', braIndex + 1);
            if (ketIndex != -1)
            {
                String ipv6Addr = site.Substring(braIndex + 1, ketIndex - braIndex - 1);
                list.Add(ipv6Addr);
                return list;
            }

            String[] separatedArray = site.Split(m_separators);
            for (int index = separatedArray.Length - 1; index > -1; --index)
            {
                if (separatedArray[index] == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
                }
                else if (separatedArray[index].Equals(""))
                {
                    if (index != separatedArray.Length - 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
                    }
                }
                else if (separatedArray[index].Equals("*"))
                {
                    if (index != 0)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
                    }

                    list.Add(separatedArray[index]);
                }
                else if (!AllLegalCharacters(separatedArray[index]))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
                }
                else
                {
                    list.Add(separatedArray[index]);
                }
            }

            return list;
        }

        private static bool AllLegalCharacters(String str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (IsLegalDNSChar(c) || IsNetbiosSplChar(c))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLegalDNSChar(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c == '-'))
                return true;
            else
                return false;
        }

        private static bool IsNetbiosSplChar(char c)
        {
            switch (c)
            {
                case '-':
                case '_':
                case '@':
                case '!':
                case '#':
                case '$':
                case '%':
                case '^':
                case '&':
                case '(':
                case ')':
                case '\'':
                case '{':
                case '}':
                case '.':
                case '~':
                    return true;
                default:
                    return false;
            }
        }

        public override String ToString()
        {
            return m_site;
        }

        public override bool Equals(Object o)
        {
            if (o == null || !(o is SiteString))
                return false;
            else
                return this.Equals((SiteString)o, true);
        }

        public override int GetHashCode()
        {
            TextInfo info = CultureInfo.InvariantCulture.TextInfo;
            return info.GetCaseInsensitiveHashCode(this.m_site);
        }

        internal bool Equals(SiteString ss, bool ignoreCase)
        {
            if (this.m_site == null)
                return ss.m_site == null;
            if (ss.m_site == null)
                return false;
            return this.IsSubsetOf(ss, ignoreCase) && ss.IsSubsetOf(this, ignoreCase);
        }

        public virtual SiteString Copy()
        {
            return new SiteString(m_site, m_separatedSite);
        }

        public virtual bool IsSubsetOf(SiteString operand)
        {
            return this.IsSubsetOf(operand, true);
        }

        public virtual bool IsSubsetOf(SiteString operand, bool ignoreCase)
        {
            StringComparison strComp = (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (operand == null)
            {
                return false;
            }
            else if (this.m_separatedSite.Count == operand.m_separatedSite.Count && this.m_separatedSite.Count == 0)
            {
                return true;
            }
            else if (this.m_separatedSite.Count < operand.m_separatedSite.Count - 1)
            {
                return false;
            }
            else if (this.m_separatedSite.Count > operand.m_separatedSite.Count && operand.m_separatedSite.Count > 0 && !operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*"))
            {
                return false;
            }
            else if (String.Compare(this.m_site, operand.m_site, strComp) == 0)
            {
                return true;
            }

            for (int index = 0; index < operand.m_separatedSite.Count - 1; ++index)
            {
                if (String.Compare((String)this.m_separatedSite[index], (String)operand.m_separatedSite[index], strComp) != 0)
                {
                    return false;
                }
            }

            if (this.m_separatedSite.Count < operand.m_separatedSite.Count)
            {
                return operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*");
            }
            else if (this.m_separatedSite.Count == operand.m_separatedSite.Count)
            {
                return (String.Compare((String)this.m_separatedSite[this.m_separatedSite.Count - 1], (String)operand.m_separatedSite[this.m_separatedSite.Count - 1], strComp) == 0 || operand.m_separatedSite[operand.m_separatedSite.Count - 1].Equals("*"));
            }
            else
                return true;
        }

        public virtual SiteString Intersect(SiteString operand)
        {
            if (operand == null)
            {
                return null;
            }
            else if (this.IsSubsetOf(operand))
            {
                return this.Copy();
            }
            else if (operand.IsSubsetOf(this))
            {
                return operand.Copy();
            }
            else
            {
                return null;
            }
        }

        public virtual SiteString Union(SiteString operand)
        {
            if (operand == null)
            {
                return this;
            }
            else if (this.IsSubsetOf(operand))
            {
                return operand.Copy();
            }
            else if (operand.IsSubsetOf(this))
            {
                return this.Copy();
            }
            else
            {
                return null;
            }
        }
    }
}