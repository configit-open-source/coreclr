namespace System
{
    using System;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

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

            Contract.EndContractBlock();
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
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            get;
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
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

            Contract.EndContractBlock();
            info.AddValue("TrackedObject", this.Target, typeof (T));
            info.AddValue("TrackResurrection", IsTrackResurrection());
        }

        private extern void Create(T target, bool trackResurrection);
        private extern bool IsTrackResurrection();
    }
}