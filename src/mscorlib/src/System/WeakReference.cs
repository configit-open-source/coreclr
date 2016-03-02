
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
    public class WeakReference : ISerializable
    {
        internal IntPtr m_handle;
        protected WeakReference()
        {
                        throw new NotImplementedException();
        }

        public WeakReference(Object target): this (target, false)
        {
        }

        public WeakReference(Object target, bool trackResurrection)
        {
            Create(target, trackResurrection);
        }

        protected WeakReference(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        Object target = info.GetValue("TrackedObject", typeof (Object));
            bool trackResurrection = info.GetBoolean("TrackResurrection");
            Create(target, trackResurrection);
        }

        public extern virtual bool IsAlive
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            get;
        }

        public virtual bool TrackResurrection
        {
            get
            {
                return IsTrackResurrection();
            }
        }

        public extern virtual Object Target
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            set;
        }

        extern ~WeakReference();
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        info.AddValue("TrackedObject", Target, typeof (Object));
            info.AddValue("TrackResurrection", IsTrackResurrection());
        }

        private extern void Create(Object target, bool trackResurrection);
        private extern bool IsTrackResurrection();
    }
}