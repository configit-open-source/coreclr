namespace System.Runtime.InteropServices.ComTypes
{
    public interface ITypeInfo2 : ITypeInfo
    {
        new void GetTypeAttr(out IntPtr ppTypeAttr);
        new void GetTypeComp(out ITypeComp ppTComp);
        new void GetFuncDesc(int index, out IntPtr ppFuncDesc);
        new void GetVarDesc(int index, out IntPtr ppVarDesc);
        new void GetNames(int memid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] String[] rgBstrNames, int cMaxNames, out int pcNames);
        new void GetRefTypeOfImplType(int index, out int href);
        new void GetImplTypeFlags(int index, out IMPLTYPEFLAGS pImplTypeFlags);
        new void GetIDsOfNames([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1), In] String[] rgszNames, int cNames, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] int[] pMemId);
        new void Invoke([MarshalAs(UnmanagedType.IUnknown)] Object pvInstance, int memid, Int16 wFlags, ref DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out int puArgErr);
        new void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        new void GetDllEntry(int memid, INVOKEKIND invKind, IntPtr pBstrDllName, IntPtr pBstrName, IntPtr pwOrdinal);
        new void GetRefTypeInfo(int hRef, out ITypeInfo ppTI);
        new void AddressOfMember(int memid, INVOKEKIND invKind, out IntPtr ppv);
        new void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] Object pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown), Out] out Object ppvObj);
        new void GetMops(int memid, out String pBstrMops);
        new void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);
        new void ReleaseTypeAttr(IntPtr pTypeAttr);
        new void ReleaseFuncDesc(IntPtr pFuncDesc);
        new void ReleaseVarDesc(IntPtr pVarDesc);
        void GetTypeKind(out TYPEKIND pTypeKind);
        void GetTypeFlags(out int pTypeFlags);
        void GetFuncIndexOfMemId(int memid, INVOKEKIND invKind, out int pFuncIndex);
        void GetVarIndexOfMemId(int memid, out int pVarIndex);
        void GetCustData(ref Guid guid, out Object pVarVal);
        void GetFuncCustData(int index, ref Guid guid, out Object pVarVal);
        void GetParamCustData(int indexFunc, int indexParam, ref Guid guid, out Object pVarVal);
        void GetVarCustData(int index, ref Guid guid, out Object pVarVal);
        void GetImplTypeCustData(int index, ref Guid guid, out Object pVarVal);
        void GetDocumentation2(int memid, out String pbstrHelpString, out int pdwHelpStringContext, out String pbstrHelpStringDll);
        void GetAllCustData(IntPtr pCustData);
        void GetAllFuncCustData(int index, IntPtr pCustData);
        void GetAllParamCustData(int indexFunc, int indexParam, IntPtr pCustData);
        void GetAllVarCustData(int index, IntPtr pCustData);
        void GetAllImplTypeCustData(int index, IntPtr pCustData);
    }
}