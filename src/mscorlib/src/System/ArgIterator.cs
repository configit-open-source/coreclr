namespace System
{
    public struct ArgIterator
    {
        private extern ArgIterator(IntPtr arglist);
        public ArgIterator(RuntimeArgumentHandle arglist): this (arglist.Value)
        {
        }

        private unsafe extern ArgIterator(IntPtr arglist, void *ptr);
        public unsafe ArgIterator(RuntimeArgumentHandle arglist, void *ptr): this (arglist.Value, ptr)
        {
        }

        public TypedReference GetNextArg()
        {
            TypedReference result = new TypedReference();
            unsafe
            {
                FCallGetNextArg(&result);
            }

            return result;
        }

        private unsafe extern void FCallGetNextArg(void *result);
        public TypedReference GetNextArg(RuntimeTypeHandle rth)
        {
            if (sigPtr != IntPtr.Zero)
            {
                return GetNextArg();
            }
            else
            {
                if (ArgPtr == IntPtr.Zero)
                    throw new ArgumentNullException();
                TypedReference result = new TypedReference();
                unsafe
                {
                    InternalGetNextArg(&result, rth.GetRuntimeType());
                }

                return result;
            }
        }

        private unsafe extern void InternalGetNextArg(void *result, RuntimeType rt);
        public void End()
        {
        }

        public extern int GetRemainingCount();
        private extern unsafe void *_GetNextArgType();
        public unsafe RuntimeTypeHandle GetNextArgType()
        {
            return new RuntimeTypeHandle(Type.GetTypeFromHandleUnsafe((IntPtr)_GetNextArgType()));
        }

        public override int GetHashCode()
        {
            return ValueType.GetHashCodeOfPtr(ArgCookie);
        }

        public override bool Equals(Object o)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NYI"));
        }

        private IntPtr ArgCookie;
        private IntPtr sigPtr;
        private IntPtr sigPtrLen;
        private IntPtr ArgPtr;
        private int RemainingArgs;
    }
}