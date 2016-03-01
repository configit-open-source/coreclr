namespace System.Reflection
{
    using System;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Diagnostics.Contracts;

    public sealed class Pointer : ISerializable
    {
        unsafe private void *_ptr;
        private RuntimeType _ptrType;
        private Pointer()
        {
        }

        private unsafe Pointer(SerializationInfo info, StreamingContext context)
        {
            _ptr = ((IntPtr)(info.GetValue("_ptr", typeof (IntPtr)))).ToPointer();
            _ptrType = (RuntimeType)info.GetValue("_ptrType", typeof (RuntimeType));
        }

        public static unsafe Object Box(void *ptr, Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (!type.IsPointer)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
            Contract.EndContractBlock();
            RuntimeType rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
            Pointer x = new Pointer();
            x._ptr = ptr;
            x._ptrType = rt;
            return x;
        }

        public static unsafe void *Unbox(Object ptr)
        {
            if (!(ptr is Pointer))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
            return ((Pointer)ptr)._ptr;
        }

        internal RuntimeType GetPointerType()
        {
            return _ptrType;
        }

        internal unsafe Object GetPointerValue()
        {
            return (IntPtr)_ptr;
        }
    }
}