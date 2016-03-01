using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Util;
using System.Threading;

namespace System.Security
{
    [Flags]
    internal enum PermissionTokenType
    {
        Normal = 0x1,
        IUnrestricted = 0x2,
        DontKnow = 0x4,
        BuiltIn = 0x8
    }

    internal sealed class PermissionTokenKeyComparer : IEqualityComparer
    {
        private Comparer _caseSensitiveComparer;
        private TextInfo _info;
        public PermissionTokenKeyComparer()
        {
            _caseSensitiveComparer = new Comparer(CultureInfo.InvariantCulture);
            _info = CultureInfo.InvariantCulture.TextInfo;
        }

        public int Compare(Object a, Object b)
        {
            String strA = a as String;
            String strB = b as String;
            if (strA == null || strB == null)
                return _caseSensitiveComparer.Compare(a, b);
            int i = _caseSensitiveComparer.Compare(a, b);
            if (i == 0)
                return 0;
            if (SecurityManager.IsSameType(strA, strB))
                return 0;
            return i;
        }

        public new bool Equals(Object a, Object b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            return Compare(a, b) == 0;
        }

        public int GetHashCode(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Contract.EndContractBlock();
            String str = obj as String;
            if (str == null)
                return obj.GetHashCode();
            int iComma = str.IndexOf(',');
            if (iComma == -1)
                iComma = str.Length;
            int accumulator = 0;
            for (int i = 0; i < iComma; ++i)
            {
                accumulator = (accumulator << 7) ^ str[i] ^ (accumulator >> 25);
            }

            return accumulator;
        }
    }

    internal sealed class PermissionToken : ISecurityEncodable
    {
        private static readonly PermissionTokenFactory s_theTokenFactory;
        private const string c_mscorlibName = "mscorlib";
        internal int m_index;
        internal volatile PermissionTokenType m_type;
        static internal TokenBasedSet s_tokenSet = new TokenBasedSet();
        internal static bool IsMscorlibClassName(string className)
        {
            Contract.Assert(c_mscorlibName == ((RuntimeAssembly)Assembly.GetExecutingAssembly()).GetSimpleName(), "mscorlib name mismatch");
            int index = className.IndexOf(',');
            if (index == -1)
                return true;
            index = className.LastIndexOf(']');
            if (index == -1)
                index = 0;
            for (int i = index; i < className.Length; i++)
            {
                if (className[i] == 'm' || className[i] == 'M')
                {
                    if (String.Compare(className, i, c_mscorlibName, 0, c_mscorlibName.Length, StringComparison.OrdinalIgnoreCase) == 0)
                        return true;
                }
            }

            return false;
        }

        static PermissionToken()
        {
            s_theTokenFactory = new PermissionTokenFactory(4);
        }

        internal PermissionToken()
        {
        }

        internal PermissionToken(int index, PermissionTokenType type, String strTypeName)
        {
            m_index = index;
            m_type = type;
        }

        public static PermissionToken GetToken(Type cls)
        {
            if (cls == null)
                return null;
            {
                return s_theTokenFactory.GetToken(cls, null);
            }
        }

        public static PermissionToken GetToken(IPermission perm)
        {
            if (perm == null)
                return null;
            IBuiltInPermission ibPerm = perm as IBuiltInPermission;
            if (ibPerm != null)
                return s_theTokenFactory.BuiltInGetToken(ibPerm.GetTokenIndex(), perm, null);
            else
                return s_theTokenFactory.GetToken(perm.GetType(), perm);
        }

        public static PermissionToken FindTokenByIndex(int i)
        {
            return s_theTokenFactory.FindTokenByIndex(i);
        }

        public static bool IsTokenProperlyAssigned(IPermission perm, PermissionToken token)
        {
            PermissionToken heldToken = GetToken(perm);
            if (heldToken.m_index != token.m_index)
                return false;
            if (token.m_type != heldToken.m_type)
                return false;
            if (perm.GetType().Module.Assembly == Assembly.GetExecutingAssembly() && heldToken.m_index >= BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED)
                return false;
            return true;
        }
    }

    internal class PermissionTokenFactory
    {
        private volatile int m_size;
        private volatile int m_index;
        private volatile Hashtable m_tokenTable;
        private volatile Hashtable m_handleTable;
        private volatile Hashtable m_indexTable;
        private volatile PermissionToken[] m_builtIn;
        private const String s_unrestrictedPermissionInferfaceName = "System.Security.Permissions.IUnrestrictedPermission";
        internal PermissionTokenFactory(int size)
        {
            m_builtIn = new PermissionToken[BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED];
            m_size = size;
            m_index = BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED;
            m_tokenTable = null;
            m_handleTable = new Hashtable(size);
            m_indexTable = new Hashtable(size);
        }

        internal PermissionToken FindTokenByIndex(int i)
        {
            PermissionToken token;
            if (i < BuiltInPermissionIndex.NUM_BUILTIN_NORMAL + BuiltInPermissionIndex.NUM_BUILTIN_UNRESTRICTED)
            {
                token = BuiltInGetToken(i, null, null);
            }
            else
            {
                token = (PermissionToken)m_indexTable[i];
            }

            return token;
        }

        internal PermissionToken GetToken(Type cls, IPermission perm)
        {
            Contract.Assert(cls != null, "Must pass in valid type");
            IntPtr typePtr = cls.TypeHandle.Value;
            object tok = m_handleTable[typePtr];
            if (tok == null)
            {
                String typeStr = cls.AssemblyQualifiedName;
                tok = m_tokenTable != null ? m_tokenTable[typeStr] : null;
                if (tok == null)
                {
                    lock (this)
                    {
                        if (m_tokenTable != null)
                        {
                            tok = m_tokenTable[typeStr];
                        }
                        else
                            m_tokenTable = new Hashtable(m_size, 1.0f, new PermissionTokenKeyComparer());
                        if (tok == null)
                        {
                            if (perm != null)
                            {
                                tok = new PermissionToken(m_index++, PermissionTokenType.IUnrestricted, typeStr);
                            }
                            else
                            {
                                if (cls.GetInterface(s_unrestrictedPermissionInferfaceName) != null)
                                    tok = new PermissionToken(m_index++, PermissionTokenType.IUnrestricted, typeStr);
                                else
                                    tok = new PermissionToken(m_index++, PermissionTokenType.Normal, typeStr);
                            }

                            m_tokenTable.Add(typeStr, tok);
                            m_indexTable.Add(m_index - 1, tok);
                            PermissionToken.s_tokenSet.SetItem(((PermissionToken)tok).m_index, tok);
                        }

                        if (!m_handleTable.Contains(typePtr))
                            m_handleTable.Add(typePtr, tok);
                    }
                }
                else
                {
                    lock (this)
                    {
                        if (!m_handleTable.Contains(typePtr))
                            m_handleTable.Add(typePtr, tok);
                    }
                }
            }

            if ((((PermissionToken)tok).m_type & PermissionTokenType.DontKnow) != 0)
            {
                if (perm != null)
                {
                    Contract.Assert(!(perm is IBuiltInPermission), "This should not be called for built-ins");
                    ((PermissionToken)tok).m_type = PermissionTokenType.IUnrestricted;
                }
                else
                {
                    Contract.Assert(cls.GetInterface("System.Security.Permissions.IBuiltInPermission") == null, "This shoudl not be called for built-ins");
                    if (cls.GetInterface(s_unrestrictedPermissionInferfaceName) != null)
                        ((PermissionToken)tok).m_type = PermissionTokenType.IUnrestricted;
                    else
                        ((PermissionToken)tok).m_type = PermissionTokenType.Normal;
                }
            }

            return (PermissionToken)tok;
        }

        internal PermissionToken GetToken(String typeStr)
        {
            Object tok = null;
            tok = m_tokenTable != null ? m_tokenTable[typeStr] : null;
            if (tok == null)
            {
                lock (this)
                {
                    if (m_tokenTable != null)
                    {
                        tok = m_tokenTable[typeStr];
                    }
                    else
                        m_tokenTable = new Hashtable(m_size, 1.0f, new PermissionTokenKeyComparer());
                    if (tok == null)
                    {
                        tok = new PermissionToken(m_index++, PermissionTokenType.DontKnow, typeStr);
                        m_tokenTable.Add(typeStr, tok);
                        m_indexTable.Add(m_index - 1, tok);
                        PermissionToken.s_tokenSet.SetItem(((PermissionToken)tok).m_index, tok);
                    }
                }
            }

            return (PermissionToken)tok;
        }

        internal PermissionToken BuiltInGetToken(int index, IPermission perm, Type cls)
        {
            PermissionToken token = Volatile.Read(ref m_builtIn[index]);
            if (token == null)
            {
                lock (this)
                {
                    token = m_builtIn[index];
                    if (token == null)
                    {
                        PermissionTokenType permType = PermissionTokenType.DontKnow;
                        if (perm != null)
                        {
                            permType = PermissionTokenType.IUnrestricted;
                        }
                        else if (cls != null)
                        {
                            permType = PermissionTokenType.IUnrestricted;
                        }

                        token = new PermissionToken(index, permType | PermissionTokenType.BuiltIn, null);
                        Volatile.Write(ref m_builtIn[index], token);
                        PermissionToken.s_tokenSet.SetItem(token.m_index, token);
                    }
                }
            }

            if ((token.m_type & PermissionTokenType.DontKnow) != 0)
            {
                token.m_type = PermissionTokenType.BuiltIn;
                if (perm != null)
                {
                    token.m_type |= PermissionTokenType.IUnrestricted;
                }
                else if (cls != null)
                {
                    token.m_type |= PermissionTokenType.IUnrestricted;
                }
                else
                {
                    token.m_type |= PermissionTokenType.DontKnow;
                }
            }

            return token;
        }
    }
}