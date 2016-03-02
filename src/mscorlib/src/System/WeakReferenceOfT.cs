
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
    public sealed class WeakReference<T> : ISerializable where T : class
    {
        internal IntPtr m_handle;
        public WeakReference(T target): this (target, false)
        {
        }

        public WeakReference(T target, bool trackResurrection)
        {
            Create(target, trackResurrection);
        }

        internal WeakReference(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        T target = (T)info.GetValue("TrackedObject", typeof (T));
            bool trackResurrection = info.GetBoolean("TrackResurrection");
            Create(target, trackResurrection);
        }

        public bool TryGetTarget(out T target)
        {
            T o = this.Target;
            target = o;
            return o != null;
        }

        public void SetTarget(T target)
        {
            this.Target = target;
        }

        private extern T Target
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            set;
        }

        extern ~WeakReference();
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        info.AddValue("TrackedObject", this.Target, typeof (T));
            info.AddValue("TrackResurrection", IsTrackResurrection());
        }

        private extern void Create(T target, bool trackResurrection);
        private extern bool IsTrackResurrection();
    }
}