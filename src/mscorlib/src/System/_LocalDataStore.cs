namespace System
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    sealed internal class LocalDataStoreHolder
    {
        private LocalDataStore m_Store;
        public LocalDataStoreHolder(LocalDataStore store)
        {
            m_Store = store;
        }

        ~LocalDataStoreHolder()
        {
            LocalDataStore store = m_Store;
            if (store == null)
                return;
            store.Dispose();
        }

        public LocalDataStore Store
        {
            get
            {
                return m_Store;
            }
        }
    }

    sealed internal class LocalDataStoreElement
    {
        private Object m_value;
        private long m_cookie;
        public LocalDataStoreElement(long cookie)
        {
            m_cookie = cookie;
        }

        public Object Value
        {
            get
            {
                return m_value;
            }

            set
            {
                m_value = value;
            }
        }

        public long Cookie
        {
            get
            {
                return m_cookie;
            }
        }
    }

    sealed internal class LocalDataStore
    {
        private LocalDataStoreElement[] m_DataTable;
        private LocalDataStoreMgr m_Manager;
        public LocalDataStore(LocalDataStoreMgr mgr, int InitialCapacity)
        {
            m_Manager = mgr;
            m_DataTable = new LocalDataStoreElement[InitialCapacity];
        }

        internal void Dispose()
        {
            m_Manager.DeleteLocalDataStore(this);
        }

        public Object GetData(LocalDataStoreSlot slot)
        {
            m_Manager.ValidateSlot(slot);
            int slotIdx = slot.Slot;
            if (slotIdx >= 0)
            {
                if (slotIdx >= m_DataTable.Length)
                    return null;
                LocalDataStoreElement element = m_DataTable[slotIdx];
                if (element == null)
                    return null;
                if (element.Cookie == slot.Cookie)
                    return element.Value;
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
        }

        public void SetData(LocalDataStoreSlot slot, Object data)
        {
            m_Manager.ValidateSlot(slot);
            int slotIdx = slot.Slot;
            if (slotIdx >= 0)
            {
                LocalDataStoreElement element = (slotIdx < m_DataTable.Length) ? m_DataTable[slotIdx] : null;
                if (element == null)
                {
                    element = PopulateElement(slot);
                }

                if (element.Cookie == slot.Cookie)
                {
                    element.Value = data;
                    return;
                }
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
        }

        internal void FreeData(int slot, long cookie)
        {
            if (slot >= m_DataTable.Length)
                return;
            LocalDataStoreElement element = m_DataTable[slot];
            if (element != null && element.Cookie == cookie)
                m_DataTable[slot] = null;
        }

        private LocalDataStoreElement PopulateElement(LocalDataStoreSlot slot)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(m_Manager, ref tookLock);
                int slotIdx = slot.Slot;
                if (slotIdx < 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
                if (slotIdx >= m_DataTable.Length)
                {
                    int capacity = m_Manager.GetSlotTableLength();
                    Contract.Assert(capacity >= m_DataTable.Length, "LocalDataStore corrupted: capacity >= m_DataTable.Length");
                    LocalDataStoreElement[] NewDataTable = new LocalDataStoreElement[capacity];
                    Array.Copy(m_DataTable, NewDataTable, m_DataTable.Length);
                    m_DataTable = NewDataTable;
                }

                Contract.Assert(slotIdx < m_DataTable.Length, "LocalDataStore corrupted: slotIdx < m_DataTable.Length");
                if (m_DataTable[slotIdx] == null)
                    m_DataTable[slotIdx] = new LocalDataStoreElement(slot.Cookie);
                return m_DataTable[slotIdx];
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(m_Manager);
            }
        }
    }
}