using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Security.Util
{
    internal sealed class URLString : SiteString
    {
        private String m_protocol;
        private String m_userpass;
        private SiteString m_siteString;
        private int m_port;
        private LocalSiteString m_localSite;
        private DirectoryString m_directory;
        private const String m_defaultProtocol = "file";
        private bool m_parseDeferred;
        private String m_urlOriginal;
        private bool m_parsedOriginal;
        private bool m_isUncShare;
        private String m_fullurl;
        public void OnDeserialized(StreamingContext ctx)
        {
            if (m_urlOriginal == null)
            {
                m_parseDeferred = false;
                m_parsedOriginal = false;
                m_userpass = "";
                m_urlOriginal = m_fullurl;
                m_fullurl = null;
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != 0)
            {
                DoDeferredParse();
                m_fullurl = m_urlOriginal;
            }
        }

        private void OnSerialized(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != 0)
            {
                m_fullurl = null;
            }
        }

        public URLString()
        {
            m_protocol = "";
            m_userpass = "";
            m_siteString = new SiteString();
            m_port = -1;
            m_localSite = null;
            m_directory = new DirectoryString();
            m_parseDeferred = false;
        }

        private void DoDeferredParse()
        {
            if (m_parseDeferred)
            {
                ParseString(m_urlOriginal, m_parsedOriginal);
                m_parseDeferred = false;
            }
        }

        public URLString(string url): this (url, false, false)
        {
        }

        public URLString(string url, bool parsed): this (url, parsed, false)
        {
        }

        internal URLString(string url, bool parsed, bool doDeferredParsing)
        {
            m_port = -1;
            m_userpass = "";
            DoFastChecks(url);
            m_urlOriginal = url;
            m_parsedOriginal = parsed;
            m_parseDeferred = true;
            if (doDeferredParsing)
                DoDeferredParse();
        }

        private String UnescapeURL(String url)
        {
            StringBuilder intermediate = StringBuilderCache.Acquire(url.Length);
            int Rindex = 0;
            int index;
            int braIndex = -1;
            int ketIndex = -1;
            braIndex = url.IndexOf('[', Rindex);
            if (braIndex != -1)
                ketIndex = url.IndexOf(']', braIndex);
            do
            {
                index = url.IndexOf('%', Rindex);
                if (index == -1)
                {
                    intermediate = intermediate.Append(url, Rindex, (url.Length - Rindex));
                    break;
                }

                if (index > braIndex && index < ketIndex)
                {
                    intermediate = intermediate.Append(url, Rindex, (ketIndex - Rindex + 1));
                    Rindex = ketIndex + 1;
                    continue;
                }

                if (url.Length - index < 2)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                if (url[index + 1] == 'u' || url[index + 1] == 'U')
                {
                    if (url.Length - index < 6)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                    try
                    {
                        char c = (char)(Hex.ConvertHexDigit(url[index + 2]) << 12 | Hex.ConvertHexDigit(url[index + 3]) << 8 | Hex.ConvertHexDigit(url[index + 4]) << 4 | Hex.ConvertHexDigit(url[index + 5]));
                        intermediate = intermediate.Append(url, Rindex, index - Rindex);
                        intermediate = intermediate.Append(c);
                    }
                    catch (ArgumentException)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                    }

                    Rindex = index + 6;
                }
                else
                {
                    if (url.Length - index < 3)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                    try
                    {
                        char c = (char)(Hex.ConvertHexDigit(url[index + 1]) << 4 | Hex.ConvertHexDigit(url[index + 2]));
                        intermediate = intermediate.Append(url, Rindex, index - Rindex);
                        intermediate = intermediate.Append(c);
                    }
                    catch (ArgumentException)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                    }

                    Rindex = index + 3;
                }
            }
            while (true);
            return StringBuilderCache.GetStringAndRelease(intermediate);
        }

        private String ParseProtocol(String url)
        {
            String temp;
            int index = url.IndexOf(':');
            if (index == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
            }
            else if (index == -1)
            {
                m_protocol = m_defaultProtocol;
                temp = url;
            }
            else if (url.Length > index + 1)
            {
                if (index == m_defaultProtocol.Length && String.Compare(url, 0, m_defaultProtocol, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    m_protocol = m_defaultProtocol;
                    temp = url.Substring(index + 1);
                    m_isUncShare = true;
                }
                else if (url[index + 1] != '\\')
                {
                    if (url.Length > index + 2 && url[index + 1] == '/' && url[index + 2] == '/')
                    {
                        m_protocol = url.Substring(0, index);
                        for (int i = 0; i < m_protocol.Length; ++i)
                        {
                            char c = m_protocol[i];
                            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c == '+') || (c == '.') || (c == '-'))
                            {
                                continue;
                            }
                            else
                            {
                                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                            }
                        }

                        temp = url.Substring(index + 3);
                    }
                    else
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                    }
                }
                else
                {
                    m_protocol = m_defaultProtocol;
                    temp = url;
                }
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
            }

            return temp;
        }

        private String ParsePort(String url)
        {
            String temp = url;
            char[] separators = new char[]{':', '/'};
            int Rindex = 0;
            int userpassIndex = temp.IndexOf('@');
            if (userpassIndex != -1)
            {
                if (temp.IndexOf('/', 0, userpassIndex) == -1)
                {
                    m_userpass = temp.Substring(0, userpassIndex);
                    Rindex = userpassIndex + 1;
                }
            }

            int braIndex = -1;
            int ketIndex = -1;
            int portIndex = -1;
            braIndex = url.IndexOf('[', Rindex);
            if (braIndex != -1)
                ketIndex = url.IndexOf(']', braIndex);
            if (ketIndex != -1)
            {
                portIndex = temp.IndexOfAny(separators, ketIndex);
            }
            else
            {
                portIndex = temp.IndexOfAny(separators, Rindex);
            }

            if (portIndex != -1 && temp[portIndex] == ':')
            {
                if (temp[portIndex + 1] >= '0' && temp[portIndex + 1] <= '9')
                {
                    int tempIndex = temp.IndexOf('/', Rindex);
                    if (tempIndex == -1)
                    {
                        m_port = Int32.Parse(temp.Substring(portIndex + 1), CultureInfo.InvariantCulture);
                        if (m_port < 0)
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                        temp = temp.Substring(Rindex, portIndex - Rindex);
                    }
                    else if (tempIndex > portIndex)
                    {
                        m_port = Int32.Parse(temp.Substring(portIndex + 1, tempIndex - portIndex - 1), CultureInfo.InvariantCulture);
                        temp = temp.Substring(Rindex, portIndex - Rindex) + temp.Substring(tempIndex);
                    }
                    else
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
                }
                else
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
            }
            else
            {
                temp = temp.Substring(Rindex);
            }

            return temp;
        }

        internal static String PreProcessForExtendedPathRemoval(String url, bool isFileUrl)
        {
            bool uncShare = false;
            return PreProcessForExtendedPathRemoval(url, isFileUrl, ref uncShare);
        }

        private static String PreProcessForExtendedPathRemoval(String url, bool isFileUrl, ref bool isUncShare)
        {
            StringBuilder modifiedUrl = new StringBuilder(url);
            {
                int curCmpIdx = 0;
                int curModIdx = 0;
                if ((url.Length - curCmpIdx) >= 4 && (String.Compare(url, curCmpIdx, "//?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(url, curCmpIdx, "//./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    modifiedUrl.Remove(curModIdx, 4);
                    curCmpIdx += 4;
                }
                else
                {
                    if (isFileUrl)
                    {
                        while (url[curCmpIdx] == '/')
                        {
                            curCmpIdx++;
                            curModIdx++;
                        }
                    }

                    if ((url.Length - curCmpIdx) >= 4 && (String.Compare(url, curCmpIdx, "\\\\?\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(url, curCmpIdx, "\\\\?/", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(url, curCmpIdx, "\\\\.\\", 0, 4, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(url, curCmpIdx, "\\\\./", 0, 4, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        modifiedUrl.Remove(curModIdx, 4);
                        curCmpIdx += 4;
                    }
                }
            }

            if (isFileUrl)
            {
                int slashCount = 0;
                bool seenFirstBackslash = false;
                while (slashCount < modifiedUrl.Length && (modifiedUrl[slashCount] == '/' || modifiedUrl[slashCount] == '\\'))
                {
                    if (!seenFirstBackslash && modifiedUrl[slashCount] == '\\')
                    {
                        seenFirstBackslash = true;
                        if (slashCount + 1 < modifiedUrl.Length && modifiedUrl[slashCount + 1] == '\\')
                            isUncShare = true;
                    }

                    slashCount++;
                }

                modifiedUrl.Remove(0, slashCount);
                modifiedUrl.Replace('\\', '/');
            }

            if (modifiedUrl.Length >= Path.MaxPath)
            {
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            }

            return modifiedUrl.ToString();
        }

        private String PreProcessURL(String url, bool isFileURL)
        {
            if (isFileURL)
            {
                url = PreProcessForExtendedPathRemoval(url, true, ref m_isUncShare);
            }
            else
            {
                url = url.Replace('\\', '/');
            }

            return url;
        }

        private void ParseFileURL(String url)
        {
            String temp = url;
            int index = temp.IndexOf('/');
            if (index != -1 && ((index == 2 && temp[index - 1] != ':' && temp[index - 1] != '|') || index != 2) && index != temp.Length - 1)
            {
                int tempIndex = temp.IndexOf('/', index + 1);
                if (tempIndex != -1)
                    index = tempIndex;
                else
                    index = -1;
            }

            String localSite;
            if (index == -1)
                localSite = temp;
            else
                localSite = temp.Substring(0, index);
            if (localSite.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
            int i;
            bool spacesAllowed;
            if (localSite[0] == '\\' && localSite[1] == '\\')
            {
                spacesAllowed = true;
                i = 2;
            }
            else
            {
                i = 0;
                spacesAllowed = false;
            }

            bool useSmallCharToUpper = true;
            for (; i < localSite.Length; ++i)
            {
                char c = localSite[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || (c == '-') || (c == '/') || (c == ':') || (c == '|') || (c == '.') || (c == '*') || (c == '$') || (spacesAllowed && c == ' '))
                {
                    continue;
                }
                else
                {
                    useSmallCharToUpper = false;
                    break;
                }
            }

            if (useSmallCharToUpper)
                localSite = String.SmallCharToUpper(localSite);
            else
                localSite = localSite.ToUpper(CultureInfo.InvariantCulture);
            m_localSite = new LocalSiteString(localSite);
            if (index == -1)
            {
                if (localSite[localSite.Length - 1] == '*')
                    m_directory = new DirectoryString("*", false);
                else
                    m_directory = new DirectoryString();
            }
            else
            {
                String directoryString = temp.Substring(index + 1);
                if (directoryString.Length == 0)
                {
                    m_directory = new DirectoryString();
                }
                else
                {
                    m_directory = new DirectoryString(directoryString, true);
                }
            }

            m_siteString = null;
            return;
        }

        private void ParseNonFileURL(String url)
        {
            String temp = url;
            int index = temp.IndexOf('/');
            if (index == -1)
            {
                m_localSite = null;
                m_siteString = new SiteString(temp);
                m_directory = new DirectoryString();
            }
            else
            {
                String site = temp.Substring(0, index);
                m_localSite = null;
                m_siteString = new SiteString(site);
                String directoryString = temp.Substring(index + 1);
                if (directoryString.Length == 0)
                {
                    m_directory = new DirectoryString();
                }
                else
                {
                    m_directory = new DirectoryString(directoryString, false);
                }
            }

            return;
        }

        void DoFastChecks(String url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            Contract.EndContractBlock();
            if (url.Length == 0)
            {
                throw new FormatException(Environment.GetResourceString("Format_StringZeroLength"));
            }
        }

        void ParseString(String url, bool parsed)
        {
            if (!parsed)
            {
                url = UnescapeURL(url);
            }

            String temp = ParseProtocol(url);
            bool fileProtocol = (String.Compare(m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0);
            temp = PreProcessURL(temp, fileProtocol);
            if (fileProtocol)
            {
                ParseFileURL(temp);
            }
            else
            {
                temp = ParsePort(temp);
                ParseNonFileURL(temp);
            }
        }

        public String Scheme
        {
            get
            {
                DoDeferredParse();
                return m_protocol;
            }
        }

        public String Host
        {
            get
            {
                DoDeferredParse();
                if (m_siteString != null)
                {
                    return m_siteString.ToString();
                }
                else
                {
                    return m_localSite.ToString();
                }
            }
        }

        public String Port
        {
            get
            {
                DoDeferredParse();
                if (m_port == -1)
                    return null;
                else
                    return m_port.ToString(CultureInfo.InvariantCulture);
            }
        }

        public String Directory
        {
            get
            {
                DoDeferredParse();
                return m_directory.ToString();
            }
        }

        public bool IsRelativeFileUrl
        {
            get
            {
                DoDeferredParse();
                if (String.Equals(m_protocol, "file", StringComparison.OrdinalIgnoreCase) && !m_isUncShare)
                {
                    string host = m_localSite != null ? m_localSite.ToString() : null;
                    if (host.EndsWith('*'))
                        return false;
                    string directory = m_directory != null ? m_directory.ToString() : null;
                    return host == null || host.Length < 2 || !host.EndsWith(':') || String.IsNullOrEmpty(directory);
                }

                return false;
            }
        }

        public String GetFileName()
        {
            DoDeferredParse();
            if (String.Compare(m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
                return null;
            String intermediateDirectory = this.Directory.Replace('/', '\\');
            String directory = this.Host.Replace('/', '\\');
            int directorySlashIndex = directory.IndexOf('\\');
            if (directorySlashIndex == -1)
            {
                if (directory.Length != 2 || !(directory[1] == ':' || directory[1] == '|'))
                {
                    directory = "\\\\" + directory;
                }
            }
            else if (directorySlashIndex != 2 || (directorySlashIndex == 2 && directory[1] != ':' && directory[1] != '|'))
            {
                directory = "\\\\" + directory;
            }

            directory += "\\" + intermediateDirectory;
            return directory;
        }

        public String GetDirectoryName()
        {
            DoDeferredParse();
            if (String.Compare(m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
                return null;
            String intermediateDirectory = this.Directory.Replace('/', '\\');
            int slashIndex = 0;
            for (int i = intermediateDirectory.Length; i > 0; i--)
            {
                if (intermediateDirectory[i - 1] == '\\')
                {
                    slashIndex = i;
                    break;
                }
            }

            String directory = this.Host.Replace('/', '\\');
            int directorySlashIndex = directory.IndexOf('\\');
            if (directorySlashIndex == -1)
            {
                if (directory.Length != 2 || !(directory[1] == ':' || directory[1] == '|'))
                {
                    directory = "\\\\" + directory;
                }
            }
            else if (directorySlashIndex > 2 || (directorySlashIndex == 2 && directory[1] != ':' && directory[1] != '|'))
            {
                directory = "\\\\" + directory;
            }

            directory += "\\";
            if (slashIndex > 0)
            {
                directory += intermediateDirectory.Substring(0, slashIndex);
            }

            return directory;
        }

        public override SiteString Copy()
        {
            return new URLString(m_urlOriginal, m_parsedOriginal);
        }

        public override bool IsSubsetOf(SiteString site)
        {
            if (site == null)
            {
                return false;
            }

            URLString url = site as URLString;
            if (url == null)
            {
                return false;
            }

            DoDeferredParse();
            url.DoDeferredParse();
            URLString normalUrl1 = this.SpecialNormalizeUrl();
            URLString normalUrl2 = url.SpecialNormalizeUrl();
            if (String.Compare(normalUrl1.m_protocol, normalUrl2.m_protocol, StringComparison.OrdinalIgnoreCase) == 0 && normalUrl1.m_directory.IsSubsetOf(normalUrl2.m_directory))
            {
                if (normalUrl1.m_localSite != null)
                {
                    return normalUrl1.m_localSite.IsSubsetOf(normalUrl2.m_localSite);
                }
                else
                {
                    if (normalUrl1.m_port != normalUrl2.m_port)
                        return false;
                    return normalUrl2.m_siteString != null && normalUrl1.m_siteString.IsSubsetOf(normalUrl2.m_siteString);
                }
            }
            else
            {
                return false;
            }
        }

        public override String ToString()
        {
            return m_urlOriginal;
        }

        public override bool Equals(Object o)
        {
            DoDeferredParse();
            if (o == null || !(o is URLString))
                return false;
            else
                return this.Equals((URLString)o);
        }

        public override int GetHashCode()
        {
            DoDeferredParse();
            TextInfo info = CultureInfo.InvariantCulture.TextInfo;
            int accumulator = 0;
            if (this.m_protocol != null)
                accumulator = info.GetCaseInsensitiveHashCode(this.m_protocol);
            if (this.m_localSite != null)
            {
                accumulator = accumulator ^ this.m_localSite.GetHashCode();
            }
            else
            {
                accumulator = accumulator ^ this.m_siteString.GetHashCode();
            }

            accumulator = accumulator ^ this.m_directory.GetHashCode();
            return accumulator;
        }

        public bool Equals(URLString url)
        {
            return CompareUrls(this, url);
        }

        public static bool CompareUrls(URLString url1, URLString url2)
        {
            if (url1 == null && url2 == null)
                return true;
            if (url1 == null || url2 == null)
                return false;
            url1.DoDeferredParse();
            url2.DoDeferredParse();
            URLString normalUrl1 = url1.SpecialNormalizeUrl();
            URLString normalUrl2 = url2.SpecialNormalizeUrl();
            if (String.Compare(normalUrl1.m_protocol, normalUrl2.m_protocol, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            if (String.Compare(normalUrl1.m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (!normalUrl1.m_localSite.IsSubsetOf(normalUrl2.m_localSite) || !normalUrl2.m_localSite.IsSubsetOf(normalUrl1.m_localSite))
                    return false;
            }
            else
            {
                if (String.Compare(normalUrl1.m_userpass, normalUrl2.m_userpass, StringComparison.Ordinal) != 0)
                    return false;
                if (!normalUrl1.m_siteString.IsSubsetOf(normalUrl2.m_siteString) || !normalUrl2.m_siteString.IsSubsetOf(normalUrl1.m_siteString))
                    return false;
                if (url1.m_port != url2.m_port)
                    return false;
            }

            if (!normalUrl1.m_directory.IsSubsetOf(normalUrl2.m_directory) || !normalUrl2.m_directory.IsSubsetOf(normalUrl1.m_directory))
                return false;
            return true;
        }

        internal String NormalizeUrl()
        {
            DoDeferredParse();
            StringBuilder builtUrl = StringBuilderCache.Acquire();
            if (String.Compare(m_protocol, "file", StringComparison.OrdinalIgnoreCase) == 0)
            {
                builtUrl = builtUrl.AppendFormat("FILE:///{0}/{1}", m_localSite.ToString(), m_directory.ToString());
            }
            else
            {
                builtUrl = builtUrl.AppendFormat("{0}://{1}{2}", m_protocol, m_userpass, m_siteString.ToString());
                if (m_port != -1)
                    builtUrl = builtUrl.AppendFormat("{0}", m_port);
                builtUrl = builtUrl.AppendFormat("/{0}", m_directory.ToString());
            }

            return StringBuilderCache.GetStringAndRelease(builtUrl).ToUpper(CultureInfo.InvariantCulture);
        }

        internal URLString SpecialNormalizeUrl()
        {
            DoDeferredParse();
            if (String.Compare(m_protocol, "file", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return this;
            }
            else
            {
                String localSite = m_localSite.ToString();
                if (localSite.Length == 2 && (localSite[1] == '|' || localSite[1] == ':'))
                {
                    String deviceName = null;
                    GetDeviceName(localSite, JitHelpers.GetStringHandleOnStack(ref deviceName));
                    if (deviceName != null)
                    {
                        if (deviceName.IndexOf("://", StringComparison.Ordinal) != -1)
                        {
                            URLString u = new URLString(deviceName + "/" + this.m_directory.ToString());
                            u.DoDeferredParse();
                            return u;
                        }
                        else
                        {
                            URLString u = new URLString("file://" + deviceName + "/" + this.m_directory.ToString());
                            u.DoDeferredParse();
                            return u;
                        }
                    }
                    else
                        return this;
                }
                else
                {
                    return this;
                }
            }
        }

        private static extern void GetDeviceName(String driveLetter, StringHandleOnStack retDeviceName);
    }

    internal class DirectoryString : SiteString
    {
        private bool m_checkForIllegalChars;
        private new static char[] m_separators = {'/'};
        protected static char[] m_illegalDirectoryCharacters = {'\\', ':', '*', '?', '"', '<', '>', '|'};
        public DirectoryString()
        {
            m_site = "";
            m_separatedSite = new ArrayList();
        }

        public DirectoryString(String directory, bool checkForIllegalChars)
        {
            m_site = directory;
            m_checkForIllegalChars = checkForIllegalChars;
            m_separatedSite = CreateSeparatedString(directory);
        }

        private ArrayList CreateSeparatedString(String directory)
        {
            if (directory == null || directory.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
            }

            Contract.EndContractBlock();
            ArrayList list = new ArrayList();
            String[] separatedArray = directory.Split(m_separators);
            for (int index = 0; index < separatedArray.Length; ++index)
            {
                if (separatedArray[index] == null || separatedArray[index].Equals(""))
                {
                }
                else if (separatedArray[index].Equals("*"))
                {
                    if (index != separatedArray.Length - 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }

                    list.Add(separatedArray[index]);
                }
                else if (m_checkForIllegalChars && separatedArray[index].IndexOfAny(m_illegalDirectoryCharacters) != -1)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                }
                else
                {
                    list.Add(separatedArray[index]);
                }
            }

            return list;
        }

        public virtual bool IsSubsetOf(DirectoryString operand)
        {
            return this.IsSubsetOf(operand, true);
        }

        public virtual bool IsSubsetOf(DirectoryString operand, bool ignoreCase)
        {
            if (operand == null)
            {
                return false;
            }
            else if (operand.m_separatedSite.Count == 0)
            {
                return this.m_separatedSite.Count == 0 || this.m_separatedSite.Count > 0 && String.Compare((String)this.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else if (this.m_separatedSite.Count == 0)
            {
                return String.Compare((String)operand.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else
            {
                return base.IsSubsetOf(operand, ignoreCase);
            }
        }
    }

    internal class LocalSiteString : SiteString
    {
        private new static char[] m_separators = {'/'};
        public LocalSiteString(String site)
        {
            m_site = site.Replace('|', ':');
            if (m_site.Length > 2 && m_site.IndexOf(':') != -1)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
            m_separatedSite = CreateSeparatedString(m_site);
        }

        private ArrayList CreateSeparatedString(String directory)
        {
            if (directory == null || directory.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
            }

            Contract.EndContractBlock();
            ArrayList list = new ArrayList();
            String[] separatedArray = directory.Split(m_separators);
            for (int index = 0; index < separatedArray.Length; ++index)
            {
                if (separatedArray[index] == null || separatedArray[index].Equals(""))
                {
                    if (index < 2 && directory[index] == '/')
                    {
                        list.Add("//");
                    }
                    else if (index != separatedArray.Length - 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }
                }
                else if (separatedArray[index].Equals("*"))
                {
                    if (index != separatedArray.Length - 1)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDirectoryOnUrl"));
                    }

                    list.Add(separatedArray[index]);
                }
                else
                {
                    list.Add(separatedArray[index]);
                }
            }

            return list;
        }

        public virtual bool IsSubsetOf(LocalSiteString operand)
        {
            return this.IsSubsetOf(operand, true);
        }

        public virtual bool IsSubsetOf(LocalSiteString operand, bool ignoreCase)
        {
            if (operand == null)
            {
                return false;
            }
            else if (operand.m_separatedSite.Count == 0)
            {
                return this.m_separatedSite.Count == 0 || this.m_separatedSite.Count > 0 && String.Compare((String)this.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else if (this.m_separatedSite.Count == 0)
            {
                return String.Compare((String)operand.m_separatedSite[0], "*", StringComparison.Ordinal) == 0;
            }
            else
            {
                return base.IsSubsetOf(operand, ignoreCase);
            }
        }
    }
}