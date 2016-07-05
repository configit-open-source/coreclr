// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** __ComObject is the root class for all COM wrappers.  This class
** defines only the basics. This class is used for wrapping COM objects
** accessed from COM+
**
** 
===========================================================*/
namespace System {
    
    using System;
    using System.Collections;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Security.Permissions;

    internal class __ComObject : MarshalByRefObject
    {
        private Hashtable m_ObjectToDataMap;

        /*============================================================
        ** default constructor
        ** can't instantiate this directly
        =============================================================*/
        protected __ComObject ()
        {
        }

        //====================================================================
        // Overrides ToString() to make sure we call to IStringable if the 
        // COM object implements it in the case of weakly typed RCWs
        //====================================================================
        public override string ToString()
        {
               
            return base.ToString();
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal IntPtr GetIUnknown(out bool fIsURTAggregated)
        {
            throw new NotImplementedException();
        }

        //====================================================================
        // This method retrieves the data associated with the specified
        // key if any such data exists for the current __ComObject.
        //====================================================================
        internal Object GetData(Object key)
        {
            Object data = null;

            // Synchronize access to the map.
            lock(this)
            {
                // If the map hasn't been allocated, then there can be no data for the specified key.
                if (m_ObjectToDataMap != null)
                {
                    // Look up the data in the map.
                    data = m_ObjectToDataMap[key];
                }
            }

            return data;
        }
        
        //====================================================================
        // This method sets the data for the specified key on the current 
        // __ComObject.
        //====================================================================
        internal bool SetData(Object key, Object data)
        {
            bool bAdded = false;

            // Synchronize access to the map.
            lock(this)
            {
                // If the map hasn't been allocated yet, allocate it.
                if (m_ObjectToDataMap == null)
                    m_ObjectToDataMap = new Hashtable();

                // If there isn't already data in the map then add it.
                if (m_ObjectToDataMap[key] == null)
                {
                    m_ObjectToDataMap[key] = data;
                    bAdded = true;
                }
            }

            return bAdded;
        }

        //====================================================================
        // This method is called from within the EE and releases all the 
        // cached data for the __ComObject.
        //====================================================================
        [System.Security.SecurityCritical]  // auto-generated
        internal void ReleaseAllData()
        {
          throw new NotImplementedException();
        }

        //====================================================================
        // This method is called from within the EE and is used to handle
        // calls on methods of event interfaces.
        //====================================================================
        [System.Security.SecurityCritical]  // auto-generated
        internal Object GetEventProvider(RuntimeType t)
        {
            // Check to see if we already have a cached event provider for this type.
            Object EvProvider = GetData(t);

            // If we don't then we need to create one.
            if (EvProvider == null)
                EvProvider = CreateEventProvider(t);

            return EvProvider;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal int ReleaseSelf()
        {
            throw new NotImplementedException();
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void FinalReleaseSelf()
        {
          throw new NotImplementedException();
        }

        [System.Security.SecurityCritical]  // auto-generated
#if !FEATURE_CORECLR
        [ReflectionPermissionAttribute(SecurityAction.Assert, MemberAccess=true)]
#endif
        private Object CreateEventProvider(RuntimeType t)
        {
            // Create the event provider for the specified type.
            Object EvProvider = Activator.CreateInstance(t, Activator.ConstructorDefault | BindingFlags.NonPublic, null, new Object[]{this}, null);

            // Attempt to cache the wrapper on the object.
            if (!SetData(t, EvProvider))
            {
                // Dispose the event provider if it implements IDisposable.
                IDisposable DisposableEvProv = EvProvider as IDisposable;
                if (DisposableEvProv != null)
                    DisposableEvProv.Dispose();

                // Another thead already cached the wrapper so use that one instead.
                EvProvider = GetData(t);
            }

            return EvProvider;
        }
    }
}
