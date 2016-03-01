namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public interface ITypeLib2 : ITypeLib
    {
        new int GetTypeInfoCount();
        new void GetTypeInfo(int index, out ITypeInfo ppTI);
        new void GetTypeInfoType(int index, out TYPEKIND pTKind);
        new void GetTypeInfoOfGuid(ref Guid guid, out ITypeInfo ppTInfo);
        new void GetLibAttr(out IntPtr ppTLibAttr);
        new void GetTypeComp(out ITypeComp ppTComp);
        new void GetDocumentation(int index, out String strName, out String strDocString, out int dwHelpContext, out String strHelpFile);
        new bool IsName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal);
        new void FindName([MarshalAs(UnmanagedType.LPWStr)] String szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] ITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref Int16 pcFound);
        new void ReleaseTLibAttr(IntPtr pTLibAttr);
        void GetCustData(ref Guid guid, out Object pVarVal);
        void GetDocumentation2(int index, out String pbstrHelpString, out int pdwHelpStringContext, out String pbstrHelpStringDll);
        void GetLibStatistics(IntPtr pcUniqueNames, out int pcchUniqueNames);
        void GetAllCustData(IntPtr pCustData);
    }
}