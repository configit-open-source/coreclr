namespace System.Threading
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;
    using System.Collections.Concurrent;

    public struct NativeOverlapped
    {
        public IntPtr InternalLow;
        public IntPtr InternalHigh;
        public int OffsetLow;
        public int OffsetHigh;
        public IntPtr EventHandle;
    }

    unsafe internal class _IOCompletionCallback
    {
        IOCompletionCallback _ioCompletionCallback;
        ExecutionContext _executionContext;
        uint _errorCode;
        uint _numBytes;
        NativeOverlapped*_pOVERLAP;
        static _IOCompletionCallback()
        {
        }

        internal _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ref StackCrawlMark stackMark)
        {
            _ioCompletionCallback = ioCompletionCallback;
            _executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
        }

        static internal ContextCallback _ccb = new ContextCallback(IOCompletionCallback_Context);
        static internal void IOCompletionCallback_Context(Object state)
        {
            _IOCompletionCallback helper = (_IOCompletionCallback)state;
            Contract.Assert(helper != null, "_IOCompletionCallback cannot be null");
            helper._ioCompletionCallback(helper._errorCode, helper._numBytes, helper._pOVERLAP);
        }

        static unsafe internal void PerformIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped*pOVERLAP)
        {
            Overlapped overlapped;
            _IOCompletionCallback helper;
            do
            {
                overlapped = OverlappedData.GetOverlappedFromNative(pOVERLAP).m_overlapped;
                helper = overlapped.iocbHelper;
                if (helper == null || helper._executionContext == null || helper._executionContext.IsDefaultFTContext(true))
                {
                    IOCompletionCallback callback = overlapped.UserCallback;
                    callback(errorCode, numBytes, pOVERLAP);
                }
                else
                {
                    helper._errorCode = errorCode;
                    helper._numBytes = numBytes;
                    helper._pOVERLAP = pOVERLAP;
                    using (ExecutionContext executionContext = helper._executionContext.CreateCopy())
                        ExecutionContext.Run(executionContext, _ccb, helper, true);
                }

                OverlappedData.CheckVMForIOPacket(out pOVERLAP, out errorCode, out numBytes);
            }
            while (pOVERLAP != null);
        }
    }

    sealed internal class OverlappedData
    {
        internal IAsyncResult m_asyncResult;
        internal IOCompletionCallback m_iocb;
        internal _IOCompletionCallback m_iocbHelper;
        internal Overlapped m_overlapped;
        private Object m_userObject;
        private IntPtr m_pinSelf;
        private IntPtr m_userObjectInternal;
        private int m_AppDomainId;
        private byte m_isArray;
        private byte m_toBeCleaned;
        internal NativeOverlapped m_nativeOverlapped;
        internal OverlappedData()
        {
        }

        internal void ReInitialize()
        {
            m_asyncResult = null;
            m_iocb = null;
            m_iocbHelper = null;
            m_overlapped = null;
            m_userObject = null;
            Contract.Assert(m_pinSelf.IsNull(), "OverlappedData has not been freed: m_pinSelf");
            m_pinSelf = (IntPtr)0;
            m_userObjectInternal = (IntPtr)0;
            Contract.Assert(m_AppDomainId == 0 || m_AppDomainId == AppDomain.CurrentDomain.Id, "OverlappedData is not in the current domain");
            m_AppDomainId = 0;
            m_nativeOverlapped.EventHandle = (IntPtr)0;
            m_isArray = 0;
            m_nativeOverlapped.InternalLow = (IntPtr)0;
            m_nativeOverlapped.InternalHigh = (IntPtr)0;
        }

        unsafe internal NativeOverlapped*Pack(IOCompletionCallback iocb, Object userData)
        {
            if (!m_pinSelf.IsNull())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_Overlapped_Pack"));
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (iocb != null)
            {
                m_iocbHelper = new _IOCompletionCallback(iocb, ref stackMark);
                m_iocb = iocb;
            }
            else
            {
                m_iocbHelper = null;
                m_iocb = null;
            }

            m_userObject = userData;
            if (m_userObject != null)
            {
                if (m_userObject.GetType() == typeof (Object[]))
                {
                    m_isArray = 1;
                }
                else
                {
                    m_isArray = 0;
                }
            }

            return AllocateNativeOverlapped();
        }

        unsafe internal NativeOverlapped*UnsafePack(IOCompletionCallback iocb, Object userData)
        {
            if (!m_pinSelf.IsNull())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_Overlapped_Pack"));
            }

            m_userObject = userData;
            if (m_userObject != null)
            {
                if (m_userObject.GetType() == typeof (Object[]))
                {
                    m_isArray = 1;
                }
                else
                {
                    m_isArray = 0;
                }
            }

            m_iocb = iocb;
            m_iocbHelper = null;
            return AllocateNativeOverlapped();
        }

        internal IntPtr UserHandle
        {
            get
            {
                return m_nativeOverlapped.EventHandle;
            }

            set
            {
                m_nativeOverlapped.EventHandle = value;
            }
        }

        unsafe private extern NativeOverlapped*AllocateNativeOverlapped();
        unsafe internal static extern void FreeNativeOverlapped(NativeOverlapped*nativeOverlappedPtr);
        unsafe internal static extern OverlappedData GetOverlappedFromNative(NativeOverlapped*nativeOverlappedPtr);
        unsafe internal static extern void CheckVMForIOPacket(out NativeOverlapped*pOVERLAP, out uint errorCode, out uint numBytes);
    }

    public class Overlapped
    {
        private OverlappedData m_overlappedData;
        private static PinnableBufferCache s_overlappedDataCache = new PinnableBufferCache("System.Threading.OverlappedData", () => new OverlappedData());
        public Overlapped()
        {
            m_overlappedData = (OverlappedData)s_overlappedDataCache.Allocate();
            m_overlappedData.m_overlapped = this;
        }

        public Overlapped(int offsetLo, int offsetHi, IntPtr hEvent, IAsyncResult ar)
        {
            m_overlappedData = (OverlappedData)s_overlappedDataCache.Allocate();
            m_overlappedData.m_overlapped = this;
            m_overlappedData.m_nativeOverlapped.OffsetLow = offsetLo;
            m_overlappedData.m_nativeOverlapped.OffsetHigh = offsetHi;
            m_overlappedData.UserHandle = hEvent;
            m_overlappedData.m_asyncResult = ar;
        }

        public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult ar): this (offsetLo, offsetHi, new IntPtr(hEvent), ar)
        {
        }

        public IAsyncResult AsyncResult
        {
            get
            {
                return m_overlappedData.m_asyncResult;
            }

            set
            {
                m_overlappedData.m_asyncResult = value;
            }
        }

        public int OffsetLow
        {
            get
            {
                return m_overlappedData.m_nativeOverlapped.OffsetLow;
            }

            set
            {
                m_overlappedData.m_nativeOverlapped.OffsetLow = value;
            }
        }

        public int OffsetHigh
        {
            get
            {
                return m_overlappedData.m_nativeOverlapped.OffsetHigh;
            }

            set
            {
                m_overlappedData.m_nativeOverlapped.OffsetHigh = value;
            }
        }

        public int EventHandle
        {
            get
            {
                return m_overlappedData.UserHandle.ToInt32();
            }

            set
            {
                m_overlappedData.UserHandle = new IntPtr(value);
            }
        }

        public IntPtr EventHandleIntPtr
        {
            get
            {
                return m_overlappedData.UserHandle;
            }

            set
            {
                m_overlappedData.UserHandle = value;
            }
        }

        internal _IOCompletionCallback iocbHelper
        {
            get
            {
                return m_overlappedData.m_iocbHelper;
            }
        }

        internal IOCompletionCallback UserCallback
        {
            [System.Security.SecurityCritical]
            get
            {
                return m_overlappedData.m_iocb;
            }
        }

        unsafe public NativeOverlapped*Pack(IOCompletionCallback iocb)
        {
            return Pack(iocb, null);
        }

        unsafe public NativeOverlapped*Pack(IOCompletionCallback iocb, Object userData)
        {
            return m_overlappedData.Pack(iocb, userData);
        }

        unsafe public NativeOverlapped*UnsafePack(IOCompletionCallback iocb)
        {
            return UnsafePack(iocb, null);
        }

        unsafe public NativeOverlapped*UnsafePack(IOCompletionCallback iocb, Object userData)
        {
            return m_overlappedData.UnsafePack(iocb, userData);
        }

        unsafe public static Overlapped Unpack(NativeOverlapped*nativeOverlappedPtr)
        {
            if (nativeOverlappedPtr == null)
                throw new ArgumentNullException("nativeOverlappedPtr");
            Contract.EndContractBlock();
            Overlapped overlapped = OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr).m_overlapped;
            return overlapped;
        }

        unsafe public static void Free(NativeOverlapped*nativeOverlappedPtr)
        {
            if (nativeOverlappedPtr == null)
                throw new ArgumentNullException("nativeOverlappedPtr");
            Contract.EndContractBlock();
            Overlapped overlapped = OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr).m_overlapped;
            OverlappedData.FreeNativeOverlapped(nativeOverlappedPtr);
            OverlappedData overlappedData = overlapped.m_overlappedData;
            overlapped.m_overlappedData = null;
            overlappedData.ReInitialize();
            s_overlappedDataCache.Free(overlappedData);
        }
    }
}