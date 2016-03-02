
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32;

namespace System.Security
{
    public sealed class SecureString : IDisposable
    {
        private SafeBSTRHandle m_buffer;
        private int m_length;
        private bool m_readOnly;
        private bool m_encrypted;
        static bool supportedOnCurrentPlatform = EncryptionSupported();
        const int BlockSize = (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE / 2;
        const int MaxLength = 65536;
        const uint ProtectionScope = Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS;
        static SecureString()
        {
        }

        unsafe static bool EncryptionSupported()
        {
            bool supported = true;
            try
            {
                Win32Native.SystemFunction041(SafeBSTRHandle.Allocate(null, (int)Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE), Win32Native.CRYPTPROTECTMEMORY_BLOCK_SIZE, Win32Native.CRYPTPROTECTMEMORY_SAME_PROCESS);
            }
            catch (EntryPointNotFoundException)
            {
                supported = false;
            }

            return supported;
        }

        internal SecureString(SecureString str)
        {
            AllocateBuffer(str.BufferLength);
            SafeBSTRHandle.Copy(str.m_buffer, this.m_buffer);
            m_length = str.m_length;
            m_encrypted = str.m_encrypted;
        }

        public SecureString()
        {
            CheckSupportedOnCurrentPlatform();
            AllocateBuffer(BlockSize);
            m_length = 0;
        }

        private unsafe void InitializeSecureString(char *value, int length)
        {
            CheckSupportedOnCurrentPlatform();
            AllocateBuffer(length);
            m_length = length;
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy(bufferPtr, (byte *)value, length * 2);
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }

            ProtectMemory();
        }

        public unsafe SecureString(char *value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (length > MaxLength)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Length"));
            }

                        InitializeSecureString(value, length);
        }

        public int Length
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                EnsureNotDisposed();
                return m_length;
            }
        }

        public void AppendChar(char c)
        {
            EnsureNotDisposed();
            EnsureNotReadOnly();
            EnsureCapacity(m_length + 1);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                UnProtectMemory();
                m_buffer.Write<char>((uint)m_length * sizeof (char), c);
                m_length++;
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                ProtectMemory();
            }
        }

        public void Clear()
        {
            EnsureNotDisposed();
            EnsureNotReadOnly();
            m_length = 0;
            m_buffer.ClearBuffer();
            m_encrypted = false;
        }

        public SecureString Copy()
        {
            EnsureNotDisposed();
            return new SecureString(this);
        }

        public void Dispose()
        {
            if (m_buffer != null && !m_buffer.IsInvalid)
            {
                m_buffer.Close();
                m_buffer = null;
            }
        }

        public void InsertAt(int index, char c)
        {
            if (index < 0 || index > m_length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }

                        EnsureNotDisposed();
            EnsureNotReadOnly();
            EnsureCapacity(m_length + 1);
            unsafe
            {
                byte *bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr);
                    char *pBuffer = (char *)bufferPtr;
                    for (int i = m_length; i > index; i--)
                    {
                        pBuffer[i] = pBuffer[i - 1];
                    }

                    pBuffer[index] = c;
                    ++m_length;
                }
                catch (Exception)
                {
                    ProtectMemory();
                    throw;
                }
                finally
                {
                    ProtectMemory();
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer();
                }
            }
        }

        public bool IsReadOnly()
        {
            EnsureNotDisposed();
            return m_readOnly;
        }

        public void MakeReadOnly()
        {
            EnsureNotDisposed();
            m_readOnly = true;
        }

        public void RemoveAt(int index)
        {
            EnsureNotDisposed();
            EnsureNotReadOnly();
            if (index < 0 || index >= m_length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }

            unsafe
            {
                byte *bufferPtr = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    UnProtectMemory();
                    m_buffer.AcquirePointer(ref bufferPtr);
                    char *pBuffer = (char *)bufferPtr;
                    for (int i = index; i < m_length - 1; i++)
                    {
                        pBuffer[i] = pBuffer[i + 1];
                    }

                    pBuffer[--m_length] = (char)0;
                }
                catch (Exception)
                {
                    ProtectMemory();
                    throw;
                }
                finally
                {
                    ProtectMemory();
                    if (bufferPtr != null)
                        m_buffer.ReleasePointer();
                }
            }
        }

        public void SetAt(int index, char c)
        {
            if (index < 0 || index >= m_length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexString"));
            }

                                    EnsureNotDisposed();
            EnsureNotReadOnly();
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                UnProtectMemory();
                m_buffer.Write<char>((uint)index * sizeof (char), c);
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                ProtectMemory();
            }
        }

        private int BufferLength
        {
            [System.Security.SecurityCritical]
            get
            {
                                return m_buffer.Length;
            }
        }

        private void AllocateBuffer(int size)
        {
            uint alignedSize = GetAlignedSize(size);
            m_buffer = SafeBSTRHandle.Allocate(null, alignedSize);
            if (m_buffer.IsInvalid)
            {
                throw new OutOfMemoryException();
            }
        }

        private void CheckSupportedOnCurrentPlatform()
        {
            if (!supportedOnCurrentPlatform)
            {
                throw new NotSupportedException(Environment.GetResourceString("Arg_PlatformSecureString"));
            }

                    }

        private void EnsureCapacity(int capacity)
        {
            if (capacity > MaxLength)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            }

                        if (capacity <= m_buffer.Length)
            {
                return;
            }

            SafeBSTRHandle newBuffer = SafeBSTRHandle.Allocate(null, GetAlignedSize(capacity));
            if (newBuffer.IsInvalid)
            {
                throw new OutOfMemoryException();
            }

            SafeBSTRHandle.Copy(m_buffer, newBuffer);
            m_buffer.Close();
            m_buffer = newBuffer;
        }

        private void EnsureNotDisposed()
        {
            if (m_buffer == null)
            {
                throw new ObjectDisposedException(null);
            }

                    }

        private void EnsureNotReadOnly()
        {
            if (m_readOnly)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }

                    }

        private static uint GetAlignedSize(int size)
        {
                        uint alignedSize = ((uint)size / BlockSize) * BlockSize;
            if ((size % BlockSize != 0) || size == 0)
            {
                alignedSize += BlockSize;
            }

            return alignedSize;
        }

        private unsafe int GetAnsiByteCount()
        {
            const uint CP_ACP = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?';
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                m_buffer.AcquirePointer(ref bufferPtr);
                return Win32Native.WideCharToMultiByte(CP_ACP, flgs, (char *)bufferPtr, m_length, null, 0, IntPtr.Zero, new IntPtr((void *)&DefaultCharUsed));
            }
            finally
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
        }

        private unsafe void GetAnsiBytes(byte *ansiStrPtr, int byteCount)
        {
            const uint CP_ACP = 0;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
            uint flgs = WC_NO_BEST_FIT_CHARS;
            uint DefaultCharUsed = (uint)'?';
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                m_buffer.AcquirePointer(ref bufferPtr);
                Win32Native.WideCharToMultiByte(CP_ACP, flgs, (char *)bufferPtr, m_length, ansiStrPtr, byteCount - 1, IntPtr.Zero, new IntPtr((void *)&DefaultCharUsed));
                *(ansiStrPtr + byteCount - 1) = (byte)0;
            }
            finally
            {
                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }
        }

        private void ProtectMemory()
        {
                                    if (m_length == 0 || m_encrypted)
            {
                return;
            }

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                int status = Win32Native.SystemFunction040(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                if (status < 0)
                {
                    throw new CryptographicException(Win32Native.RtlNtStatusToDosError(status));
                }

                m_encrypted = true;
            }
        }

        internal unsafe IntPtr ToBSTR()
        {
            EnsureNotDisposed();
            int length = m_length;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    ptr = Win32Native.SysAllocStringLen(null, length);
                }

                if (ptr == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy((byte *)ptr.ToPointer(), bufferPtr, length * 2);
                result = ptr;
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                ProtectMemory();
                if (result == IntPtr.Zero)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)(length * 2));
                        Win32Native.SysFreeString(ptr);
                    }
                }

                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }

            return result;
        }

        internal unsafe IntPtr ToUniStr(bool allocateFromHeap)
        {
            EnsureNotDisposed();
            int length = m_length;
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    if (allocateFromHeap)
                    {
                        ptr = Marshal.AllocHGlobal((length + 1) * 2);
                    }
                    else
                    {
                        ptr = Marshal.AllocCoTaskMem((length + 1) * 2);
                    }
                }

                if (ptr == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                UnProtectMemory();
                m_buffer.AcquirePointer(ref bufferPtr);
                Buffer.Memcpy((byte *)ptr.ToPointer(), bufferPtr, length * 2);
                char *endptr = (char *)ptr.ToPointer();
                *(endptr + length) = '\0';
                result = ptr;
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                ProtectMemory();
                if (result == IntPtr.Zero)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)(length * 2));
                        if (allocateFromHeap)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else
                        {
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }
                }

                if (bufferPtr != null)
                    m_buffer.ReleasePointer();
            }

            return result;
        }

        internal unsafe IntPtr ToAnsiStr(bool allocateFromHeap)
        {
            EnsureNotDisposed();
            IntPtr ptr = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;
            int byteCount = 0;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                UnProtectMemory();
                byteCount = GetAnsiByteCount() + 1;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    if (allocateFromHeap)
                    {
                        ptr = Marshal.AllocHGlobal(byteCount);
                    }
                    else
                    {
                        ptr = Marshal.AllocCoTaskMem(byteCount);
                    }
                }

                if (ptr == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }

                GetAnsiBytes((byte *)ptr.ToPointer(), byteCount);
                result = ptr;
            }
            catch (Exception)
            {
                ProtectMemory();
                throw;
            }
            finally
            {
                ProtectMemory();
                if (result == IntPtr.Zero)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Win32Native.ZeroMemory(ptr, (UIntPtr)byteCount);
                        if (allocateFromHeap)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else
                        {
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }
                }
            }

            return result;
        }

        private void UnProtectMemory()
        {
                                    if (m_length == 0)
            {
                return;
            }

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                if (m_encrypted)
                {
                    int status = Win32Native.SystemFunction041(m_buffer, (uint)m_buffer.Length * 2, ProtectionScope);
                    if (status < 0)
                    {
                        throw new CryptographicException(Win32Native.RtlNtStatusToDosError(status));
                    }

                    m_encrypted = false;
                }
            }
        }
    }

    internal sealed class SafeBSTRHandle : SafeBuffer
    {
        internal SafeBSTRHandle(): base (true)
        {
        }

        internal static SafeBSTRHandle Allocate(String src, uint len)
        {
            SafeBSTRHandle bstr = SysAllocStringLen(src, len);
            bstr.Initialize(len * sizeof (char));
            return bstr;
        }

        private static extern SafeBSTRHandle SysAllocStringLen(String src, uint len);
        override protected bool ReleaseHandle()
        {
            Win32Native.ZeroMemory(handle, (UIntPtr)(Win32Native.SysStringLen(handle) * 2));
            Win32Native.SysFreeString(handle);
            return true;
        }

        internal unsafe void ClearBuffer()
        {
            byte *bufferPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                AcquirePointer(ref bufferPtr);
                Win32Native.ZeroMemory((IntPtr)bufferPtr, (UIntPtr)(Win32Native.SysStringLen((IntPtr)bufferPtr) * 2));
            }
            finally
            {
                if (bufferPtr != null)
                    ReleasePointer();
            }
        }

        internal unsafe int Length
        {
            get
            {
                return (int)Win32Native.SysStringLen(this);
            }
        }

        internal unsafe static void Copy(SafeBSTRHandle source, SafeBSTRHandle target)
        {
            byte *sourcePtr = null, targetPtr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                source.AcquirePointer(ref sourcePtr);
                target.AcquirePointer(ref targetPtr);
                                Buffer.Memcpy(targetPtr, sourcePtr, (int)Win32Native.SysStringLen((IntPtr)sourcePtr) * 2);
            }
            finally
            {
                if (sourcePtr != null)
                    source.ReleasePointer();
                if (targetPtr != null)
                    target.ReleasePointer();
            }
        }
    }
}