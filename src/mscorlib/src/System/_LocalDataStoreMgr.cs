namespace System
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    public sealed class LocalDataStoreSlot
    {
        private LocalDataStoreMgr m_mgr;
        private int m_slot;
        private long m_cookie;
        internal LocalDataStoreSlot(LocalDataStoreMgr mgr, int slot, long cookie)
        {
            m_mgr = mgr;
            m_slot = slot;
            m_cookie = cookie;
        }

        internal LocalDataStoreMgr Manager
        {
            get
            {
                return m_mgr;
            }
        }

        internal int Slot
        {
            get
            {
                return m_slot;
            }
        }

        internal long Cookie
        {
            get
            {
                return m_cookie;
            }
        }

        ~LocalDataStoreSlot()
        {
            LocalDataStoreMgr mgr = m_mgr;
            if (mgr == null)
                return;
            int slot = m_slot;
            m_slot = -1;
            mgr.FreeDataSlot(slot, m_cookie);
        }
    }

    sealed internal class LocalDataStoreMgr
    {
        private const int InitialSlotTableSize = 64;
        private const int SlotTableDoubleThreshold = 512;
        private const int LargeSlotTableSizeIncrease = 128;
        public LocalDataStoreHolder CreateLocalDataStore()
        {
            LocalDataStore store = new LocalDataStore(this, m_SlotInfoTable.Length);
            LocalDataStoreHolder holder = new LocalDataStoreHolder(store);
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                m_ManagedLocalDataStores.Add(store);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }

            return holder;
        }

        public void DeleteLocalDataStore(LocalDataStore store)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                m_ManagedLocalDataStores.Remove(store);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        public LocalDataStoreSlot AllocateDataSlot()
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                LocalDataStoreSlot slot;
                int slotTableSize = m_SlotInfoTable.Length;
                int availableSlot = m_FirstAvailableSlot;
                while (availableSlot < slotTableSize)
                {
                    if (!m_SlotInfoTable[availableSlot])
                        break;
                    availableSlot++;
                }

                if (availableSlot >= slotTableSize)
                {
                    int newSlotTableSize;
                    if (slotTableSize < SlotTableDoubleThreshold)
                    {
                        newSlotTableSize = slotTableSize * 2;
                    }
                    else
                    {
                        newSlotTableSize = slotTableSize + LargeSlotTableSizeIncrease;
                    }

                    bool[] newSlotInfoTable = new bool[newSlotTableSize];
                    Array.Copy(m_SlotInfoTable, newSlotInfoTable, slotTableSize);
                    m_SlotInfoTable = newSlotInfoTable;
                }

                m_SlotInfoTable[availableSlot] = true;
                slot = new LocalDataStoreSlot(this, availableSlot, checked (m_CookieGenerator++));
                m_FirstAvailableSlot = availableSlot + 1;
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        public LocalDataStoreSlot AllocateNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                LocalDataStoreSlot slot = AllocateDataSlot();
                m_KeyToSlotMap.Add(name, slot);
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        public LocalDataStoreSlot GetNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                LocalDataStoreSlot slot = m_KeyToSlotMap.GetValueOrDefault(name);
                if (null == slot)
                    return AllocateNamedDataSlot(name);
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        public void FreeNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                m_KeyToSlotMap.Remove(name);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        internal void FreeDataSlot(int slot, long cookie)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                for (int i = 0; i < m_ManagedLocalDataStores.Count; i++)
                {
                    ((LocalDataStore)m_ManagedLocalDataStores[i]).FreeData(slot, cookie);
                }

                m_SlotInfoTable[slot] = false;
                if (slot < m_FirstAvailableSlot)
                    m_FirstAvailableSlot = slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        public void ValidateSlot(LocalDataStoreSlot slot)
        {
            if (slot == null || slot.Manager != this)
                throw new ArgumentException(Environment.GetResourceString("Argument_ALSInvalidSlot"));
            Contract.EndContractBlock();
        }

        internal int GetSlotTableLength()
        {
            return m_SlotInfoTable.Length;
        }

        private bool[] m_SlotInfoTable = new bool[InitialSlotTableSize];
        private int m_FirstAvailableSlot;
        private List<LocalDataStore> m_ManagedLocalDataStores = new List<LocalDataStore>();
        private Dictionary<String, LocalDataStoreSlot> m_KeyToSlotMap = new Dictionary<String, LocalDataStoreSlot>();
        private long m_CookieGenerator;
    }
}