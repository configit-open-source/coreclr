using System.Security.Permissions;
using System.Diagnostics.Contracts;

namespace System.Threading
{
    public enum LazyThreadSafetyMode
    {
        None,
        PublicationOnly,
        ExecutionAndPublication
    }

    public static class LazyInitializer
    {
        public static T EnsureInitialized<T>(ref T target)where T : class
        {
            if (Volatile.Read<T>(ref target) != null)
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, LazyHelpers<T>.s_activatorFactorySelector);
        }

        public static T EnsureInitialized<T>(ref T target, Func<T> valueFactory)where T : class
        {
            if (Volatile.Read<T>(ref target) != null)
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, valueFactory);
        }

        private static T EnsureInitializedCore<T>(ref T target, Func<T> valueFactory)where T : class
        {
            T value = valueFactory();
            if (value == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Lazy_StaticInit_InvalidOperation"));
            }

            Interlocked.CompareExchange(ref target, value, null);
            Contract.Assert(target != null);
            return target;
        }

        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock)
        {
            if (Volatile.Read(ref initialized))
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, LazyHelpers<T>.s_activatorFactorySelector);
        }

        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
        {
            if (Volatile.Read(ref initialized))
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, valueFactory);
        }

        private static T EnsureInitializedCore<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
        {
            object slock = syncLock;
            if (slock == null)
            {
                object newLock = new object ();
                slock = Interlocked.CompareExchange(ref syncLock, newLock, null);
                if (slock == null)
                {
                    slock = newLock;
                }
            }

            lock (slock)
            {
                if (!Volatile.Read(ref initialized))
                {
                    target = valueFactory();
                    Volatile.Write(ref initialized, true);
                }
            }

            return target;
        }
    }

    static class LazyHelpers<T>
    {
        internal static Func<T> s_activatorFactorySelector = new Func<T>(ActivatorFactorySelector);
        private static T ActivatorFactorySelector()
        {
            try
            {
                return (T)Activator.CreateInstance(typeof (T));
            }
            catch (MissingMethodException)
            {
                throw new MissingMemberException(Environment.GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT"));
            }
        }
    }
}