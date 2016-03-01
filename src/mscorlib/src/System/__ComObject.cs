using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace System
{
    internal class __ComObject : MarshalByRefObject
    {
        private Hashtable m_ObjectToDataMap;
        protected __ComObject()
        {
        }

        public override string ToString()
        {
            if (AppDomain.IsAppXModel())
            {
                IStringable stringableType = this as IStringable;
                if (stringableType != null)
                {
                    return stringableType.ToString();
                }
            }

            return base.ToString();
        }

        internal IntPtr GetIUnknown(out bool fIsURTAggregated)
        {
            fIsURTAggregated = !GetType().IsDefined(typeof (ComImportAttribute), false);
            return System.Runtime.InteropServices.Marshal.GetIUnknownForObject(this);
        }

        internal Object GetData(Object key)
        {
            Object data = null;
            lock (this)
            {
                if (m_ObjectToDataMap != null)
                {
                    data = m_ObjectToDataMap[key];
                }
            }

            return data;
        }

        internal bool SetData(Object key, Object data)
        {
            bool bAdded = false;
            lock (this)
            {
                if (m_ObjectToDataMap == null)
                    m_ObjectToDataMap = new Hashtable();
                if (m_ObjectToDataMap[key] == null)
                {
                    m_ObjectToDataMap[key] = data;
                    bAdded = true;
                }
            }

            return bAdded;
        }

        internal void ReleaseAllData()
        {
            lock (this)
            {
                if (m_ObjectToDataMap != null)
                {
                    foreach (Object o in m_ObjectToDataMap.Values)
                    {
                        IDisposable DisposableObj = o as IDisposable;
                        if (DisposableObj != null)
                            DisposableObj.Dispose();
                        __ComObject ComObj = o as __ComObject;
                        if (ComObj != null)
                            Marshal.ReleaseComObject(ComObj);
                    }

                    m_ObjectToDataMap = null;
                }
            }
        }

        internal Object GetEventProvider(RuntimeType t)
        {
            Object EvProvider = GetData(t);
            if (EvProvider == null)
                EvProvider = CreateEventProvider(t);
            return EvProvider;
        }

        internal int ReleaseSelf()
        {
            return Marshal.InternalReleaseComObject(this);
        }

        internal void FinalReleaseSelf()
        {
            Marshal.InternalFinalReleaseComObject(this);
        }

        private Object CreateEventProvider(RuntimeType t)
        {
            Object EvProvider = Activator.CreateInstance(t, Activator.ConstructorDefault | BindingFlags.NonPublic, null, new Object[]{this}, null);
            if (!SetData(t, EvProvider))
            {
                IDisposable DisposableEvProv = EvProvider as IDisposable;
                if (DisposableEvProv != null)
                    DisposableEvProv.Dispose();
                EvProvider = GetData(t);
            }

            return EvProvider;
        }
    }
}