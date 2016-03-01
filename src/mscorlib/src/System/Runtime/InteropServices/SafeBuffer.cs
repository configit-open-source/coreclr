namespace System.Runtime.InteropServices
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.Contracts;

    public abstract unsafe class SafeBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static readonly UIntPtr Uninitialized = (UIntPtr.Size == 4) ? ((UIntPtr)UInt32.MaxValue) : ((UIntPtr)UInt64.MaxValue);
        private UIntPtr _numBytes;
        protected SafeBuffer(bool ownsHandle): base (ownsHandle)
        {
            _numBytes = Uninitialized;
        }

        public void Initialize(ulong numBytes)
        {
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (IntPtr.Size == 4 && numBytes > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_AddressSpace"));
            Contract.EndContractBlock();
            if (numBytes >= (ulong)Uninitialized)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_UIntPtrMax-1"));
            _numBytes = (UIntPtr)numBytes;
        }

        public void Initialize(uint numElements, uint sizeOfEachElement)
        {
            if (numElements < 0)
                throw new ArgumentOutOfRangeException("numElements", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (sizeOfEachElement < 0)
                throw new ArgumentOutOfRangeException("sizeOfEachElement", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (IntPtr.Size == 4 && numElements * sizeOfEachElement > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_AddressSpace"));
            Contract.EndContractBlock();
            if (numElements * sizeOfEachElement >= (ulong)Uninitialized)
                throw new ArgumentOutOfRangeException("numElements", Environment.GetResourceString("ArgumentOutOfRange_UIntPtrMax-1"));
            _numBytes = checked ((UIntPtr)(numElements * sizeOfEachElement));
        }

        public void Initialize<T>(uint numElements)where T : struct
        {
            Initialize(numElements, Marshal.AlignedSizeOf<T>());
        }

        public void AcquirePointer(ref byte *pointer)
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            pointer = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                bool junk = false;
                DangerousAddRef(ref junk);
                pointer = (byte *)handle;
            }
        }

        public void ReleasePointer()
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            DangerousRelease();
        }

        public T Read<T>(ulong byteOffset)where T : struct
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            uint sizeofT = Marshal.SizeOfType(typeof (T));
            byte *ptr = (byte *)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);
            T value;
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                GenericPtrToStructure<T>(ptr, out value, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }

            return value;
        }

        public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count)where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            uint sizeofT = Marshal.SizeOfType(typeof (T));
            uint alignedSizeofT = Marshal.AlignedSizeOf<T>();
            byte *ptr = (byte *)handle + byteOffset;
            SpaceCheck(ptr, checked ((ulong)(alignedSizeofT * count)));
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                for (int i = 0; i < count; i++)
                    unsafe
                    {
                        GenericPtrToStructure<T>(ptr + alignedSizeofT * i, out array[i + index], sizeofT);
                    }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        public void Write<T>(ulong byteOffset, T value)where T : struct
        {
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            uint sizeofT = Marshal.SizeOfType(typeof (T));
            byte *ptr = (byte *)handle + byteOffset;
            SpaceCheck(ptr, sizeofT);
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                GenericStructureToPtr(ref value, ptr, sizeofT);
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count)where T : struct
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (_numBytes == Uninitialized)
                throw NotInitialized();
            uint sizeofT = Marshal.SizeOfType(typeof (T));
            uint alignedSizeofT = Marshal.AlignedSizeOf<T>();
            byte *ptr = (byte *)handle + byteOffset;
            SpaceCheck(ptr, checked ((ulong)(alignedSizeofT * count)));
            bool mustCallRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustCallRelease);
                for (int i = 0; i < count; i++)
                    unsafe
                    {
                        GenericStructureToPtr(ref array[i + index], ptr + alignedSizeofT * i, sizeofT);
                    }
            }
            finally
            {
                if (mustCallRelease)
                    DangerousRelease();
            }
        }

        public ulong ByteLength
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                if (_numBytes == Uninitialized)
                    throw NotInitialized();
                return (ulong)_numBytes;
            }
        }

        private void SpaceCheck(byte *ptr, ulong sizeInBytes)
        {
            if ((ulong)_numBytes < sizeInBytes)
                NotEnoughRoom();
            if ((ulong)(ptr - (byte *)handle) > ((ulong)_numBytes) - sizeInBytes)
                NotEnoughRoom();
        }

        private static void NotEnoughRoom()
        {
            throw new ArgumentException(Environment.GetResourceString("Arg_BufferTooSmall"));
        }

        private static InvalidOperationException NotInitialized()
        {
            Contract.Assert(false, "Uninitialized SafeBuffer!  Someone needs to call Initialize before using this instance!");
            return new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MustCallInitialize"));
        }

        internal static void GenericPtrToStructure<T>(byte *ptr, out T structure, uint sizeofT)where T : struct
        {
            structure = default (T);
            PtrToStructureNative(ptr, __makeref (structure), sizeofT);
        }

        private static extern void PtrToStructureNative(byte *ptr, TypedReference structure, uint sizeofT);
        internal static void GenericStructureToPtr<T>(ref T structure, byte *ptr, uint sizeofT)where T : struct
        {
            StructureToPtrNative(__makeref (structure), ptr, sizeofT);
        }

        private static extern void StructureToPtrNative(TypedReference structure, byte *ptr, uint sizeofT);
    }
}