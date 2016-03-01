
using System.Security;
using System.Security.Permissions;

namespace System.IO
{
    internal class FileSecurityState : SecurityState
    {
        private static readonly char[] m_illegalCharacters = {'?', '*'};
        private FileSecurityStateAccess m_access;
        private String m_userPath;
        private String m_canonicalizedPath;
        private FileSecurityState()
        {
        }

        internal FileSecurityState(FileSecurityStateAccess access, String path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            VerifyAccess(access);
            m_access = access;
            m_userPath = path;
            if (path.Equals(String.Empty, StringComparison.OrdinalIgnoreCase))
            {
                m_canonicalizedPath = String.Empty;
            }
            else
            {
                VerifyPath(path);
                m_canonicalizedPath = System.IO.Path.GetFullPathInternal(path);
            }
        }

        internal FileSecurityState(FileSecurityStateAccess access, String path, String canonicalizedPath)
        {
            VerifyAccess(access);
            VerifyPath(path);
            VerifyPath(canonicalizedPath);
            m_access = access;
            m_userPath = path;
            m_canonicalizedPath = canonicalizedPath;
        }

        internal FileSecurityStateAccess Access
        {
            get
            {
                return m_access;
            }
        }

        public String Path
        {
            [System.Runtime.CompilerServices.FriendAccessAllowed]
            get
            {
                return m_canonicalizedPath;
            }
        }

        public override void EnsureState()
        {
            if (String.Empty.Equals(m_canonicalizedPath))
                return;
            if (!IsStateAvailable())
            {
                throw new SecurityException(Environment.GetResourceString("FileSecurityState_OperationNotPermitted", (m_userPath == null) ? String.Empty : m_userPath));
            }
        }

        internal static FileSecurityStateAccess ToFileSecurityState(FileIOPermissionAccess access)
        {
                        return (FileSecurityStateAccess)access;
        }

        private static void VerifyAccess(FileSecurityStateAccess access)
        {
            if ((access & ~FileSecurityStateAccess.AllAccess) != 0)
                throw new ArgumentOutOfRangeException("access", Environment.GetResourceString("Arg_EnumIllegalVal"));
        }

        private static void VerifyPath(String path)
        {
            if (path != null)
            {
                path = path.Trim();
                if (path.Length > 2 && path.IndexOf(':', 2) != -1)
                    throw new NotSupportedException(Environment.GetResourceString("Argument_PathFormatNotSupported"));
                System.IO.Path.CheckInvalidPathChars(path);
                if (path.IndexOfAny(m_illegalCharacters) != -1)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
            }
        }
    }
}