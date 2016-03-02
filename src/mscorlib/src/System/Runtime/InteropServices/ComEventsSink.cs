namespace System.Runtime.InteropServices
{
    internal class ComEventsSink : NativeMethods.IDispatch, ICustomQueryInterface
    {
        private Guid _iidSourceItf;
        private ComTypes.IConnectionPoint _connectionPoint;
        private int _cookie;
        private ComEventsMethod _methods;
        private ComEventsSink _next;
        internal ComEventsSink(object rcw, Guid iid)
        {
            _iidSourceItf = iid;
            this.Advise(rcw);
        }

        internal static ComEventsSink Find(ComEventsSink sinks, ref Guid iid)
        {
            ComEventsSink sink = sinks;
            while (sink != null && sink._iidSourceItf != iid)
            {
                sink = sink._next;
            }

            return sink;
        }

        internal static ComEventsSink Add(ComEventsSink sinks, ComEventsSink sink)
        {
            sink._next = sinks;
            return sink;
        }

        internal static ComEventsSink RemoveAll(ComEventsSink sinks)
        {
            while (sinks != null)
            {
                sinks.Unadvise();
                sinks = sinks._next;
            }

            return null;
        }

        internal static ComEventsSink Remove(ComEventsSink sinks, ComEventsSink sink)
        {
            BCLDebug.Assert(sinks != null, "removing event sink from empty sinks collection");
            BCLDebug.Assert(sink != null, "specify event sink is null");
            if (sink == sinks)
            {
                sinks = sinks._next;
            }
            else
            {
                ComEventsSink current = sinks;
                while (current != null && current._next != sink)
                    current = current._next;
                if (current != null)
                {
                    current._next = sink._next;
                }
            }

            sink.Unadvise();
            return sinks;
        }

        public ComEventsMethod RemoveMethod(ComEventsMethod method)
        {
            _methods = ComEventsMethod.Remove(_methods, method);
            return _methods;
        }

        public ComEventsMethod FindMethod(int dispid)
        {
            return ComEventsMethod.Find(_methods, dispid);
        }

        public ComEventsMethod AddMethod(int dispid)
        {
            ComEventsMethod method = new ComEventsMethod(dispid);
            _methods = ComEventsMethod.Add(_methods, method);
            return method;
        }

        void NativeMethods.IDispatch.GetTypeInfoCount(out uint pctinfo)
        {
            pctinfo = 0;
        }

        void NativeMethods.IDispatch.GetTypeInfo(uint iTInfo, int lcid, out IntPtr info)
        {
            throw new NotImplementedException();
        }

        void NativeMethods.IDispatch.GetIDsOfNames(ref Guid iid, string[] names, uint cNames, int lcid, int[] rgDispId)
        {
            throw new NotImplementedException();
        }

        private const VarEnum VT_BYREF_VARIANT = VarEnum.VT_BYREF | VarEnum.VT_VARIANT;
        private const VarEnum VT_TYPEMASK = (VarEnum)0x0fff;
        private const VarEnum VT_BYREF_TYPEMASK = VT_TYPEMASK | VarEnum.VT_BYREF;
        private static unsafe Variant*GetVariant(Variant*pSrc)
        {
            if (pSrc->VariantType == VT_BYREF_VARIANT)
            {
                Variant*pByRefVariant = (Variant*)pSrc->AsByRefVariant;
                if ((pByRefVariant->VariantType & VT_BYREF_TYPEMASK) == VT_BYREF_VARIANT)
                    return (Variant*)pByRefVariant;
            }

            return pSrc;
        }

        unsafe void NativeMethods.IDispatch.Invoke(int dispid, ref Guid riid, int lcid, ComTypes.INVOKEKIND wFlags, ref ComTypes.DISPPARAMS pDispParams, IntPtr pvarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            ComEventsMethod method = FindMethod(dispid);
            if (method == null)
                return;
            object[] args = new object[pDispParams.cArgs];
            int[] byrefsMap = new int[pDispParams.cArgs];
            bool[] usedArgs = new bool[pDispParams.cArgs];
            Variant*pvars = (Variant*)pDispParams.rgvarg;
            int *pNamedArgs = (int *)pDispParams.rgdispidNamedArgs;
            int i;
            int pos;
            for (i = 0; i < pDispParams.cNamedArgs; i++)
            {
                pos = pNamedArgs[i];
                Variant*pvar = GetVariant(&pvars[i]);
                args[pos] = pvar->ToObject();
                usedArgs[pos] = true;
                if (pvar->IsByRef)
                {
                    byrefsMap[pos] = i;
                }
                else
                {
                    byrefsMap[pos] = -1;
                }
            }

            pos = 0;
            for (; i < pDispParams.cArgs; i++)
            {
                while (usedArgs[pos])
                {
                    ++pos;
                }

                Variant*pvar = GetVariant(&pvars[pDispParams.cArgs - 1 - i]);
                args[pos] = pvar->ToObject();
                if (pvar->IsByRef)
                    byrefsMap[pos] = pDispParams.cArgs - 1 - i;
                else
                    byrefsMap[pos] = -1;
                pos++;
            }

            object result;
            result = method.Invoke(args);
            if (pvarResult != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(result, pvarResult);
            }

            for (i = 0; i < pDispParams.cArgs; i++)
            {
                int idxToPos = byrefsMap[i];
                if (idxToPos == -1)
                    continue;
                GetVariant(&pvars[idxToPos])->CopyFromIndirect(args[i]);
            }
        }

        static Guid IID_IManagedObject = new Guid("{C3FCC19E-A970-11D2-8B5A-00A0C9B7C9C4}");
        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (iid == this._iidSourceItf || iid == typeof (NativeMethods.IDispatch).GUID)
            {
                ppv = Marshal.GetComInterfaceForObject(this, typeof (NativeMethods.IDispatch), CustomQueryInterfaceMode.Ignore);
                return CustomQueryInterfaceResult.Handled;
            }
            else if (iid == IID_IManagedObject)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        private void Advise(object rcw)
        {
            BCLDebug.Assert(_connectionPoint == null, "comevent sink is already advised");
            ComTypes.IConnectionPointContainer cpc = (ComTypes.IConnectionPointContainer)rcw;
            ComTypes.IConnectionPoint cp;
            cpc.FindConnectionPoint(ref _iidSourceItf, out cp);
            object sinkObject = this;
            cp.Advise(sinkObject, out _cookie);
            _connectionPoint = cp;
        }

        private void Unadvise()
        {
            BCLDebug.Assert(_connectionPoint != null, "can not unadvise from empty connection point");
            try
            {
                _connectionPoint.Unadvise(_cookie);
                Marshal.ReleaseComObject(_connectionPoint);
            }
            catch (System.Exception)
            {
            }
            finally
            {
                _connectionPoint = null;
            }
        }
    }

    ;
}