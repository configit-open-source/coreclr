
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Security.Util
{
    internal class TokenBasedSet
    {
        private int m_initSize = 24;
        private int m_increment = 8;
        private Object[] m_objSet;
        private volatile Object m_Obj;
        private volatile Object[] m_Set;
        private int m_cElt;
        private volatile int m_maxIndex;
        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserializedInternal();
        }

        private void OnDeserializedInternal()
        {
            if (m_objSet != null)
            {
                if (m_cElt == 1)
                    m_Obj = m_objSet[m_maxIndex];
                else
                    m_Set = m_objSet;
                m_objSet = null;
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != 0)
            {
                if (m_cElt == 1)
                {
                    m_objSet = new Object[m_maxIndex + 1];
                    m_objSet[m_maxIndex] = m_Obj;
                }
                else if (m_cElt > 0)
                {
                    m_objSet = m_Set;
                }
            }
        }

        private void OnSerialized(StreamingContext ctx)
        {
            if ((ctx.State & ~(StreamingContextStates.Clone | StreamingContextStates.CrossAppDomain)) != 0)
            {
                m_objSet = null;
            }
        }

        internal bool MoveNext(ref TokenBasedSetEnumerator e)
        {
            switch (m_cElt)
            {
                case 0:
                    return false;
                case 1:
                    if (e.Index == -1)
                    {
                        e.Index = m_maxIndex;
                        e.Current = m_Obj;
                        return true;
                    }
                    else
                    {
                        e.Index = (short)(m_maxIndex + 1);
                        e.Current = null;
                        return false;
                    }

                default:
                    while (++e.Index <= m_maxIndex)
                    {
                        e.Current = Volatile.Read(ref m_Set[e.Index]);
                        if (e.Current != null)
                            return true;
                    }

                    e.Current = null;
                    return false;
            }
        }

        internal TokenBasedSet()
        {
            Reset();
        }

        internal TokenBasedSet(TokenBasedSet tbSet)
        {
            if (tbSet == null)
            {
                Reset();
                return;
            }

            if (tbSet.m_cElt > 1)
            {
                Object[] aObj = tbSet.m_Set;
                int aLen = aObj.Length;
                Object[] aNew = new Object[aLen];
                System.Array.Copy(aObj, 0, aNew, 0, aLen);
                m_Set = aNew;
            }
            else
            {
                m_Obj = tbSet.m_Obj;
            }

            m_cElt = tbSet.m_cElt;
            m_maxIndex = tbSet.m_maxIndex;
        }

        internal void Reset()
        {
            m_Obj = null;
            m_Set = null;
            m_cElt = 0;
            m_maxIndex = -1;
        }

        internal void SetItem(int index, Object item)
        {
            Object[] aObj = null;
            if (item == null)
            {
                RemoveItem(index);
                return;
            }

            switch (m_cElt)
            {
                case 0:
                    m_cElt = 1;
                    m_maxIndex = (short)index;
                    m_Obj = item;
                    break;
                case 1:
                    if (index == m_maxIndex)
                    {
                        m_Obj = item;
                    }
                    else
                    {
                        Object objSaved = m_Obj;
                        int iMax = Math.Max(m_maxIndex, index);
                        aObj = new Object[iMax + 1];
                        aObj[m_maxIndex] = objSaved;
                        aObj[index] = item;
                        m_maxIndex = (short)iMax;
                        m_cElt = 2;
                        m_Set = aObj;
                        m_Obj = null;
                    }

                    break;
                default:
                    aObj = m_Set;
                    if (index >= aObj.Length)
                    {
                        Object[] newset = new Object[index + 1];
                        System.Array.Copy(aObj, 0, newset, 0, m_maxIndex + 1);
                        m_maxIndex = (short)index;
                        newset[index] = item;
                        m_Set = newset;
                        m_cElt++;
                    }
                    else
                    {
                        if (aObj[index] == null)
                            m_cElt++;
                        aObj[index] = item;
                        if (index > m_maxIndex)
                            m_maxIndex = (short)index;
                    }

                    break;
            }
        }

        internal Object GetItem(int index)
        {
            switch (m_cElt)
            {
                case 0:
                    return null;
                case 1:
                    if (index == m_maxIndex)
                        return m_Obj;
                    else
                        return null;
                default:
                    if (index < m_Set.Length)
                        return Volatile.Read(ref m_Set[index]);
                    else
                        return null;
            }
        }

        internal Object RemoveItem(int index)
        {
            Object ret = null;
            switch (m_cElt)
            {
                case 0:
                    ret = null;
                    break;
                case 1:
                    if (index != m_maxIndex)
                    {
                        ret = null;
                    }
                    else
                    {
                        ret = m_Obj;
                        Reset();
                    }

                    break;
                default:
                    if (index < m_Set.Length && (ret = Volatile.Read(ref m_Set[index])) != null)
                    {
                        Volatile.Write(ref m_Set[index], null);
                        m_cElt--;
                        if (index == m_maxIndex)
                            ResetMaxIndex(m_Set);
                        if (m_cElt == 1)
                        {
                            m_Obj = Volatile.Read(ref m_Set[m_maxIndex]);
                            m_Set = null;
                        }
                    }

                    break;
            }

            return ret;
        }

        private void ResetMaxIndex(Object[] aObj)
        {
            int i;
            for (i = aObj.Length - 1; i >= 0; i--)
            {
                if (aObj[i] != null)
                {
                    m_maxIndex = (short)i;
                    return;
                }
            }

            m_maxIndex = -1;
        }

        internal int GetStartingIndex()
        {
            if (m_cElt <= 1)
                return m_maxIndex;
            return 0;
        }

        internal int GetCount()
        {
            return m_cElt;
        }

        internal int GetMaxUsedIndex()
        {
            return m_maxIndex;
        }

        internal bool FastIsEmpty()
        {
            return m_cElt == 0;
        }

        internal TokenBasedSet SpecialUnion(TokenBasedSet other)
        {
            OnDeserializedInternal();
            TokenBasedSet unionSet = new TokenBasedSet();
            int maxMax;
            if (other != null)
            {
                other.OnDeserializedInternal();
                maxMax = this.GetMaxUsedIndex() > other.GetMaxUsedIndex() ? this.GetMaxUsedIndex() : other.GetMaxUsedIndex();
            }
            else
                maxMax = this.GetMaxUsedIndex();
            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.GetItem(i);
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = (other != null) ? other.GetItem(i) : null;
                IPermission otherPerm = otherObj as IPermission;
                if (thisObj == null && otherObj == null)
                    continue;
                if (thisObj == null)
                {
                    PermissionToken token = PermissionToken.GetToken(otherPerm);
                    if (token == null)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
                    }

                    unionSet.SetItem(token.m_index, otherPerm);
                }
                else if (otherObj == null)
                {
                    PermissionToken token = PermissionToken.GetToken(thisPerm);
                    if (token == null)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
                    }

                    unionSet.SetItem(token.m_index, thisPerm);
                }
                else
                {
                                    }
            }

            return unionSet;
        }

        internal void SpecialSplit(ref TokenBasedSet unrestrictedPermSet, ref TokenBasedSet normalPermSet, bool ignoreTypeLoadFailures)
        {
            int maxIndex = GetMaxUsedIndex();
            for (int i = GetStartingIndex(); i <= maxIndex; ++i)
            {
                Object obj = GetItem(i);
                if (obj != null)
                {
                    IPermission perm = obj as IPermission;
                    PermissionToken token = PermissionToken.GetToken(perm);
                    if (perm == null || token == null)
                        continue;
                    if (perm is IUnrestrictedPermission)
                    {
                        if (unrestrictedPermSet == null)
                            unrestrictedPermSet = new TokenBasedSet();
                        unrestrictedPermSet.SetItem(token.m_index, perm);
                    }
                    else
                    {
                        if (normalPermSet == null)
                            normalPermSet = new TokenBasedSet();
                        normalPermSet.SetItem(token.m_index, perm);
                    }
                }
            }
        }
    }
}