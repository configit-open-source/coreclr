namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    public struct FILETIME
    {
        public int dwLowDateTime;
        public int dwHighDateTime;
    }

    public interface IMoniker
    {
        void GetClassID(out Guid pClassID);
        int IsDirty();
        void Load(IStream pStm);
        void Save(IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
        void GetSizeMax(out Int64 pcbSize);
        void BindToObject(IBindCtx pbc, IMoniker pmkToLeft, [In()] ref Guid riidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult);
        void BindToStorage(IBindCtx pbc, IMoniker pmkToLeft, [In()] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out Object ppvObj);
        void Reduce(IBindCtx pbc, int dwReduceHowFar, ref IMoniker ppmkToLeft, out IMoniker ppmkReduced);
        void ComposeWith(IMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out IMoniker ppmkComposite);
        void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out IEnumMoniker ppenumMoniker);
        int IsEqual(IMoniker pmkOtherMoniker);
        void Hash(out int pdwHash);
        int IsRunning(IBindCtx pbc, IMoniker pmkToLeft, IMoniker pmkNewlyRunning);
        void GetTimeOfLastChange(IBindCtx pbc, IMoniker pmkToLeft, out FILETIME pFileTime);
        void Inverse(out IMoniker ppmk);
        void CommonPrefixWith(IMoniker pmkOther, out IMoniker ppmkPrefix);
        void RelativePathTo(IMoniker pmkOther, out IMoniker ppmkRelPath);
        void GetDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out String ppszDisplayName);
        void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] String pszDisplayName, out int pchEaten, out IMoniker ppmkOut);
        int IsSystemMoniker(out int pdwMksys);
    }
}