using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Threading
{
    public class ThreadLocal<T> : IDisposable
    {
        private Func<T> m_valueFactory;
        static LinkedSlotVolatile[] ts_slotArray;
        static FinalizationHelper ts_finalizationHelper;
        private int m_idComplement;
        private volatile bool m_initialized;
        private static IdManager s_idManager = new IdManager();
        private LinkedSlot m_linkedSlot = new LinkedSlot(null);
        private bool m_trackAllValues;
        public ThreadLocal()
        {
            Initialize(null, false);
        }

        public ThreadLocal(bool trackAllValues)
        {
            Initialize(null, trackAllValues);
        }

        public ThreadLocal(Func<T> valueFactory)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            Initialize(valueFactory, false);
        }

        public ThreadLocal(Func<T> valueFactory, bool trackAllValues)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            Initialize(valueFactory, trackAllValues);
        }

        private void Initialize(Func<T> valueFactory, bool trackAllValues)
        {
            m_valueFactory = valueFactory;
            m_trackAllValues = trackAllValues;
            try
            {
            }
            finally
            {
                m_idComplement = ~s_idManager.GetId();
                m_initialized = true;
            }
        }

        ~ThreadLocal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            int id;
            lock (s_idManager)
            {
                id = ~m_idComplement;
                m_idComplement = 0;
                if (id < 0 || !m_initialized)
                {
                    Contract.Assert(id >= 0 || !m_initialized, "expected id >= 0 if initialized");
                    return;
                }

                m_initialized = false;
                for (LinkedSlot linkedSlot = m_linkedSlot.Next; linkedSlot != null; linkedSlot = linkedSlot.Next)
                {
                    LinkedSlotVolatile[] slotArray = linkedSlot.SlotArray;
                    if (slotArray == null)
                    {
                        continue;
                    }

                    linkedSlot.SlotArray = null;
                    slotArray[id].Value.Value = default (T);
                    slotArray[id].Value = null;
                }
            }

            m_linkedSlot = null;
            s_idManager.ReturnId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public T Value
        {
            get
            {
                LinkedSlotVolatile[] slotArray = ts_slotArray;
                LinkedSlot slot;
                int id = ~m_idComplement;
                if (slotArray != null && id >= 0 && id < slotArray.Length && (slot = slotArray[id].Value) != null && m_initialized)
                {
                    return slot.Value;
                }

                return GetValueSlow();
            }

            set
            {
                LinkedSlotVolatile[] slotArray = ts_slotArray;
                LinkedSlot slot;
                int id = ~m_idComplement;
                if (slotArray != null && id >= 0 && id < slotArray.Length && (slot = slotArray[id].Value) != null && m_initialized)
                {
                    slot.Value = value;
                }
                else
                {
                    SetValueSlow(value, slotArray);
                }
            }
        }

        private T GetValueSlow()
        {
            int id = ~m_idComplement;
            if (id < 0)
            {
                throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
            }

            Debugger.NotifyOfCrossThreadDependency();
            T value;
            if (m_valueFactory == null)
            {
                value = default (T);
            }
            else
            {
                value = m_valueFactory();
                if (IsValueCreated)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("ThreadLocal_Value_RecursiveCallsToValue"));
                }
            }

            Value = value;
            return value;
        }

        private void SetValueSlow(T value, LinkedSlotVolatile[] slotArray)
        {
            int id = ~m_idComplement;
            if (id < 0)
            {
                throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
            }

            if (slotArray == null)
            {
                slotArray = new LinkedSlotVolatile[GetNewTableSize(id + 1)];
                ts_finalizationHelper = new FinalizationHelper(slotArray, m_trackAllValues);
                ts_slotArray = slotArray;
            }

            if (id >= slotArray.Length)
            {
                GrowTable(ref slotArray, id + 1);
                ts_finalizationHelper.SlotArray = slotArray;
                ts_slotArray = slotArray;
            }

            if (slotArray[id].Value == null)
            {
                CreateLinkedSlot(slotArray, id, value);
            }
            else
            {
                LinkedSlot slot = slotArray[id].Value;
                if (!m_initialized)
                {
                    throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
                }

                slot.Value = value;
            }
        }

        private void CreateLinkedSlot(LinkedSlotVolatile[] slotArray, int id, T value)
        {
            var linkedSlot = new LinkedSlot(slotArray);
            lock (s_idManager)
            {
                if (!m_initialized)
                {
                    throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
                }

                LinkedSlot firstRealNode = m_linkedSlot.Next;
                linkedSlot.Next = firstRealNode;
                linkedSlot.Previous = m_linkedSlot;
                linkedSlot.Value = value;
                if (firstRealNode != null)
                {
                    firstRealNode.Previous = linkedSlot;
                }

                m_linkedSlot.Next = linkedSlot;
                slotArray[id].Value = linkedSlot;
            }
        }

        public IList<T> Values
        {
            get
            {
                if (!m_trackAllValues)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("ThreadLocal_ValuesNotAvailable"));
                }

                var list = GetValuesAsList();
                if (list == null)
                    throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
                return list;
            }
        }

        private List<T> GetValuesAsList()
        {
            List<T> valueList = new List<T>();
            int id = ~m_idComplement;
            if (id == -1)
            {
                return null;
            }

            for (LinkedSlot linkedSlot = m_linkedSlot.Next; linkedSlot != null; linkedSlot = linkedSlot.Next)
            {
                valueList.Add(linkedSlot.Value);
            }

            return valueList;
        }

        private int ValuesCountForDebugDisplay
        {
            get
            {
                int count = 0;
                for (LinkedSlot linkedSlot = m_linkedSlot.Next; linkedSlot != null; linkedSlot = linkedSlot.Next)
                {
                    count++;
                }

                return count;
            }
        }

        public bool IsValueCreated
        {
            get
            {
                int id = ~m_idComplement;
                if (id < 0)
                {
                    throw new ObjectDisposedException(Environment.GetResourceString("ThreadLocal_Disposed"));
                }

                LinkedSlotVolatile[] slotArray = ts_slotArray;
                return slotArray != null && id < slotArray.Length && slotArray[id].Value != null;
            }
        }

        internal T ValueForDebugDisplay
        {
            get
            {
                LinkedSlotVolatile[] slotArray = ts_slotArray;
                int id = ~m_idComplement;
                LinkedSlot slot;
                if (slotArray == null || id >= slotArray.Length || (slot = slotArray[id].Value) == null || !m_initialized)
                    return default (T);
                return slot.Value;
            }
        }

        internal List<T> ValuesForDebugDisplay
        {
            get
            {
                return GetValuesAsList();
            }
        }

        private void GrowTable(ref LinkedSlotVolatile[] table, int minLength)
        {
            Contract.Assert(table.Length < minLength);
            int newLen = GetNewTableSize(minLength);
            LinkedSlotVolatile[] newTable = new LinkedSlotVolatile[newLen];
            lock (s_idManager)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    LinkedSlot linkedSlot = table[i].Value;
                    if (linkedSlot != null && linkedSlot.SlotArray != null)
                    {
                        linkedSlot.SlotArray = newTable;
                        newTable[i] = table[i];
                    }
                }
            }

            table = newTable;
        }

        private static int GetNewTableSize(int minSize)
        {
            if ((uint)minSize > Array.MaxArrayLength)
            {
                return int.MaxValue;
            }

            Contract.Assert(minSize > 0);
            int newSize = minSize;
            newSize--;
            newSize |= newSize >> 1;
            newSize |= newSize >> 2;
            newSize |= newSize >> 4;
            newSize |= newSize >> 8;
            newSize |= newSize >> 16;
            newSize++;
            if ((uint)newSize > Array.MaxArrayLength)
            {
                newSize = Array.MaxArrayLength;
            }

            return newSize;
        }

        private struct LinkedSlotVolatile
        {
            internal volatile LinkedSlot Value;
        }

        private sealed class LinkedSlot
        {
            internal LinkedSlot(LinkedSlotVolatile[] slotArray)
            {
                SlotArray = slotArray;
            }

            internal volatile LinkedSlot Next;
            internal volatile LinkedSlot Previous;
            internal volatile LinkedSlotVolatile[] SlotArray;
            internal T Value;
        }

        private class IdManager
        {
            private int m_nextIdToTry = 0;
            private List<bool> m_freeIds = new List<bool>();
            internal int GetId()
            {
                lock (m_freeIds)
                {
                    int availableId = m_nextIdToTry;
                    while (availableId < m_freeIds.Count)
                    {
                        if (m_freeIds[availableId])
                        {
                            break;
                        }

                        availableId++;
                    }

                    if (availableId == m_freeIds.Count)
                    {
                        m_freeIds.Add(false);
                    }
                    else
                    {
                        m_freeIds[availableId] = false;
                    }

                    m_nextIdToTry = availableId + 1;
                    return availableId;
                }
            }

            internal void ReturnId(int id)
            {
                lock (m_freeIds)
                {
                    m_freeIds[id] = true;
                    if (id < m_nextIdToTry)
                        m_nextIdToTry = id;
                }
            }
        }

        private class FinalizationHelper
        {
            internal LinkedSlotVolatile[] SlotArray;
            private bool m_trackAllValues;
            internal FinalizationHelper(LinkedSlotVolatile[] slotArray, bool trackAllValues)
            {
                SlotArray = slotArray;
                m_trackAllValues = trackAllValues;
            }

            ~FinalizationHelper()
            {
                LinkedSlotVolatile[] slotArray = SlotArray;
                Contract.Assert(slotArray != null);
                for (int i = 0; i < slotArray.Length; i++)
                {
                    LinkedSlot linkedSlot = slotArray[i].Value;
                    if (linkedSlot == null)
                    {
                        continue;
                    }

                    if (m_trackAllValues)
                    {
                        linkedSlot.SlotArray = null;
                    }
                    else
                    {
                        lock (s_idManager)
                        {
                            if (linkedSlot.Next != null)
                            {
                                linkedSlot.Next.Previous = linkedSlot.Previous;
                            }

                            Contract.Assert(linkedSlot.Previous != null);
                            linkedSlot.Previous.Next = linkedSlot.Next;
                        }
                    }
                }
            }
        }
    }

    internal sealed class SystemThreading_ThreadLocalDebugView<T>
    {
        private readonly ThreadLocal<T> m_tlocal;
        public SystemThreading_ThreadLocalDebugView(ThreadLocal<T> tlocal)
        {
            m_tlocal = tlocal;
        }

        public bool IsValueCreated
        {
            get
            {
                return m_tlocal.IsValueCreated;
            }
        }

        public T Value
        {
            get
            {
                return m_tlocal.ValueForDebugDisplay;
            }
        }

        public List<T> Values
        {
            get
            {
                return m_tlocal.ValuesForDebugDisplay;
            }
        }
    }
}