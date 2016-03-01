using System.Collections;
using System.Security.Util;

namespace System.Security
{
    internal class PermissionSetEnumerator : IEnumerator
    {
        PermissionSetEnumeratorInternal enm;
        public Object Current
        {
            get
            {
                return enm.Current;
            }
        }

        public bool MoveNext()
        {
            return enm.MoveNext();
        }

        public void Reset()
        {
            enm.Reset();
        }

        internal PermissionSetEnumerator(PermissionSet permSet)
        {
            enm = new PermissionSetEnumeratorInternal(permSet);
        }
    }

    internal struct PermissionSetEnumeratorInternal
    {
        private PermissionSet m_permSet;
        private TokenBasedSetEnumerator enm;
        public Object Current
        {
            get
            {
                return enm.Current;
            }
        }

        internal PermissionSetEnumeratorInternal(PermissionSet permSet)
        {
            m_permSet = permSet;
            enm = new TokenBasedSetEnumerator(permSet.m_permSet);
        }

        public int GetCurrentIndex()
        {
            return enm.Index;
        }

        public void Reset()
        {
            enm.Reset();
        }

        public bool MoveNext()
        {
            while (enm.MoveNext())
            {
                Object obj = enm.Current;
                IPermission perm = obj as IPermission;
                if (perm != null)
                {
                    enm.Current = perm;
                    return true;
                }
            }

            return false;
        }
    }
}