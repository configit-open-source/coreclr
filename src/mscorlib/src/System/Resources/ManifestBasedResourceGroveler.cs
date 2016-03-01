using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Microsoft.Win32;

namespace System.Resources
{
    internal class ManifestBasedResourceGroveler : IResourceGroveler
    {
        private ResourceManager.ResourceManagerMediator _mediator;
        public ManifestBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            Contract.Requires(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark)
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");
            Contract.Assert(localResourceSets != null, "localResourceSets shouldn't be null; check caller");
            ResourceSet rs = null;
            Stream stream = null;
            RuntimeAssembly satellite = null;
            CultureInfo lookForCulture = UltimateFallbackFixup(culture);
            if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
                satellite = _mediator.MainAssembly;
            }
            else if (!lookForCulture.HasInvariantCultureName && !_mediator.TryLookingForSatellite(lookForCulture))
            {
                satellite = null;
            }
            else
            {
                satellite = GetSatelliteAssembly(lookForCulture, ref stackMark);
                if (satellite == null)
                {
                    bool raiseException = (culture.HasInvariantCultureName && (_mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite));
                    if (raiseException)
                    {
                        HandleSatelliteMissing();
                    }
                }
            }

            String fileName = _mediator.GetResourceFileName(lookForCulture);
            if (satellite != null)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(culture.Name, out rs))
                    {
                    }
                }

                stream = GetManifestResourceStream(satellite, fileName, ref stackMark);
            }

            if (createIfNotExists && stream != null && rs == null)
            {
                rs = CreateResourceSet(stream, satellite);
            }
            else if (stream == null && tryParents)
            {
                bool raiseException = culture.HasInvariantCultureName;
                if (raiseException)
                {
                    HandleResourceStreamMissing(fileName);
                }
            }

            return rs;
        }

        private CultureInfo UltimateFallbackFixup(CultureInfo lookForCulture)
        {
            CultureInfo returnCulture = lookForCulture;
            if (lookForCulture.Name == _mediator.NeutralResourcesCulture.Name && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
                returnCulture = CultureInfo.InvariantCulture;
            }
            else if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite)
            {
                returnCulture = _mediator.NeutralResourcesCulture;
            }

            return returnCulture;
        }

        internal static CultureInfo GetNeutralResourcesLanguage(Assembly a, ref UltimateResourceFallbackLocation fallbackLocation)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                fallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
                return CultureInfo.InvariantCulture;
            }

            Contract.Assert(a != null, "assembly != null");
            string cultureName = null;
            short fallback = 0;
            if (GetNeutralResourcesLanguageAttribute(((RuntimeAssembly)a).GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref cultureName), out fallback))
            {
                if ((UltimateResourceFallbackLocation)fallback < UltimateResourceFallbackLocation.MainAssembly || (UltimateResourceFallbackLocation)fallback > UltimateResourceFallbackLocation.Satellite)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", fallback));
                }

                fallbackLocation = (UltimateResourceFallbackLocation)fallback;
            }
            else
            {
                fallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
                return CultureInfo.InvariantCulture;
            }

            try
            {
                CultureInfo c = CultureInfo.GetCultureInfo(cultureName);
                return c;
            }
            catch (ArgumentException e)
            {
                if (a == typeof (Object).Assembly)
                {
                    Contract.Assert(false, "mscorlib's NeutralResourcesLanguageAttribute is a malformed culture name! name: \"" + cultureName + "\"  Exception: " + e);
                    return CultureInfo.InvariantCulture;
                }

                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_Asm_Culture", a.ToString(), cultureName), e);
            }
        }

        internal ResourceSet CreateResourceSet(Stream store, Assembly assembly)
        {
            Contract.Assert(store != null, "I need a Stream!");
            if (store.CanSeek && store.Length > 4)
            {
                long startPos = store.Position;
                BinaryReader br = new BinaryReader(store);
                int bytes = br.ReadInt32();
                if (bytes == ResourceManager.MagicNumber)
                {
                    int resMgrHeaderVersion = br.ReadInt32();
                    String readerTypeName = null, resSetTypeName = null;
                    if (resMgrHeaderVersion == ResourceManager.HeaderVersionNumber)
                    {
                        br.ReadInt32();
                        readerTypeName = br.ReadString();
                        resSetTypeName = br.ReadString();
                    }
                    else if (resMgrHeaderVersion > ResourceManager.HeaderVersionNumber)
                    {
                        int numBytesToSkip = br.ReadInt32();
                        long endPosition = br.BaseStream.Position + numBytesToSkip;
                        readerTypeName = br.ReadString();
                        resSetTypeName = br.ReadString();
                        br.BaseStream.Seek(endPosition, SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ObsoleteResourcesFile", _mediator.MainAssembly.GetSimpleName()));
                    }

                    store.Position = startPos;
                    if (CanUseDefaultResourceClasses(readerTypeName, resSetTypeName))
                    {
                        RuntimeResourceSet rs;
                        rs = new RuntimeResourceSet(store);
                        return rs;
                    }
                    else
                    {
                        Type readerType = Type.GetType(readerTypeName, true);
                        Object[] args = new Object[1];
                        args[0] = store;
                        IResourceReader reader = (IResourceReader)Activator.CreateInstance(readerType, args);
                        Object[] resourceSetArgs = new Object[1];
                        resourceSetArgs[0] = reader;
                        Type resSetType;
                        if (_mediator.UserResourceSet == null)
                        {
                            Contract.Assert(resSetTypeName != null, "We should have a ResourceSet type name from the custom resource file here.");
                            resSetType = Type.GetType(resSetTypeName, true, false);
                        }
                        else
                            resSetType = _mediator.UserResourceSet;
                        ResourceSet rs = (ResourceSet)Activator.CreateInstance(resSetType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, resourceSetArgs, null, null);
                        return rs;
                    }
                }
                else
                {
                    store.Position = startPos;
                }
            }

            if (_mediator.UserResourceSet == null)
            {
                return new RuntimeResourceSet(store);
            }
            else
            {
                Object[] args = new Object[2];
                args[0] = store;
                args[1] = assembly;
                try
                {
                    ResourceSet rs = null;
                    try
                    {
                        rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
                        return rs;
                    }
                    catch (MissingMethodException)
                    {
                    }

                    args = new Object[1];
                    args[0] = store;
                    rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
                    return rs;
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResMgrBadResSet_Type", _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }

        private Stream GetManifestResourceStream(RuntimeAssembly satellite, String fileName, ref StackCrawlMark stackMark)
        {
            Contract.Requires(satellite != null, "satellite shouldn't be null; check caller");
            Contract.Requires(fileName != null, "fileName shouldn't be null; check caller");
            bool canSkipSecurityCheck = (_mediator.MainAssembly == satellite) && (_mediator.CallingAssembly == _mediator.MainAssembly);
            Stream stream = satellite.GetManifestResourceStream(_mediator.LocationInfo, fileName, canSkipSecurityCheck, ref stackMark);
            if (stream == null)
            {
                stream = CaseInsensitiveManifestResourceStreamLookup(satellite, fileName);
            }

            return stream;
        }

        private Stream CaseInsensitiveManifestResourceStreamLookup(RuntimeAssembly satellite, String name)
        {
            Contract.Requires(satellite != null, "satellite shouldn't be null; check caller");
            Contract.Requires(name != null, "name shouldn't be null; check caller");
            StringBuilder sb = new StringBuilder();
            if (_mediator.LocationInfo != null)
            {
                String nameSpace = _mediator.LocationInfo.Namespace;
                if (nameSpace != null)
                {
                    sb.Append(nameSpace);
                    if (name != null)
                        sb.Append(Type.Delimiter);
                }
            }

            sb.Append(name);
            String givenName = sb.ToString();
            CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;
            String canonicalName = null;
            foreach (String existingName in satellite.GetManifestResourceNames())
            {
                if (comparer.Compare(existingName, givenName, CompareOptions.IgnoreCase) == 0)
                {
                    if (canonicalName == null)
                    {
                        canonicalName = existingName;
                    }
                    else
                    {
                        throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_MultipleBlobs", givenName, satellite.ToString()));
                    }
                }
            }

            if (canonicalName == null)
            {
                return null;
            }

            bool canSkipSecurityCheck = _mediator.MainAssembly == satellite && _mediator.CallingAssembly == _mediator.MainAssembly;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Stream s = satellite.GetManifestResourceStream(canonicalName, ref stackMark, canSkipSecurityCheck);
            return s;
        }

        private RuntimeAssembly GetSatelliteAssembly(CultureInfo lookForCulture, ref StackCrawlMark stackMark)
        {
            if (!_mediator.LookedForSatelliteContractVersion)
            {
                _mediator.SatelliteContractVersion = _mediator.ObtainSatelliteContractVersion(_mediator.MainAssembly);
                _mediator.LookedForSatelliteContractVersion = true;
            }

            RuntimeAssembly satellite = null;
            String satAssemblyName = GetSatelliteAssemblyName();
            try
            {
                satellite = _mediator.MainAssembly.InternalGetSatelliteAssembly(satAssemblyName, lookForCulture, _mediator.SatelliteContractVersion, false, ref stackMark);
            }
            catch (FileLoadException fle)
            {
                int hr = fle._HResult;
                if (hr != Win32Native.MakeHRFromErrorCode(Win32Native.ERROR_ACCESS_DENIED))
                {
                    Contract.Assert(false, "[This assert catches satellite assembly build/deployment problems - report this message to your build lab & loc engineer]" + Environment.NewLine + "GetSatelliteAssembly failed for culture " + lookForCulture.Name + " and version " + (_mediator.SatelliteContractVersion == null ? _mediator.MainAssembly.GetVersion().ToString() : _mediator.SatelliteContractVersion.ToString()) + " of assembly " + _mediator.MainAssembly.GetSimpleName() + " with error code 0x" + hr.ToString("X", CultureInfo.InvariantCulture) + Environment.NewLine + "Exception: " + fle);
                }
            }
            catch (BadImageFormatException bife)
            {
                Contract.Assert(false, "[This assert catches satellite assembly build/deployment problems - report this message to your build lab & loc engineer]" + Environment.NewLine + "GetSatelliteAssembly failed for culture " + lookForCulture.Name + " and version " + (_mediator.SatelliteContractVersion == null ? _mediator.MainAssembly.GetVersion().ToString() : _mediator.SatelliteContractVersion.ToString()) + " of assembly " + _mediator.MainAssembly.GetSimpleName() + Environment.NewLine + "Exception: " + bife);
            }

            return satellite;
        }

        private bool CanUseDefaultResourceClasses(String readerTypeName, String resSetTypeName)
        {
            Contract.Assert(readerTypeName != null, "readerTypeName shouldn't be null; check caller");
            Contract.Assert(resSetTypeName != null, "resSetTypeName shouldn't be null; check caller");
            if (_mediator.UserResourceSet != null)
                return false;
            AssemblyName mscorlib = new AssemblyName(ResourceManager.MscorlibName);
            if (readerTypeName != null)
            {
                if (!ResourceManager.CompareNames(readerTypeName, ResourceManager.ResReaderTypeName, mscorlib))
                    return false;
            }

            if (resSetTypeName != null)
            {
                if (!ResourceManager.CompareNames(resSetTypeName, ResourceManager.ResSetTypeName, mscorlib))
                    return false;
            }

            return true;
        }

        private String GetSatelliteAssemblyName()
        {
            String satAssemblyName = _mediator.MainAssembly.GetSimpleName();
            satAssemblyName += ".resources";
            return satAssemblyName;
        }

        private void HandleSatelliteMissing()
        {
            String satAssemName = _mediator.MainAssembly.GetSimpleName() + ".resources.dll";
            if (_mediator.SatelliteContractVersion != null)
            {
                satAssemName += ", Version=" + _mediator.SatelliteContractVersion.ToString();
            }

            AssemblyName an = new AssemblyName();
            an.SetPublicKey(_mediator.MainAssembly.GetPublicKey());
            byte[] token = an.GetPublicKeyToken();
            int iLen = token.Length;
            StringBuilder publicKeyTok = new StringBuilder(iLen * 2);
            for (int i = 0; i < iLen; i++)
            {
                publicKeyTok.Append(token[i].ToString("x", CultureInfo.InvariantCulture));
            }

            satAssemName += ", PublicKeyToken=" + publicKeyTok;
            String missingCultureName = _mediator.NeutralResourcesCulture.Name;
            if (missingCultureName.Length == 0)
            {
                missingCultureName = "<invariant>";
            }

            throw new MissingSatelliteAssemblyException(Environment.GetResourceString("MissingSatelliteAssembly_Culture_Name", _mediator.NeutralResourcesCulture, satAssemName), missingCultureName);
        }

        private void HandleResourceStreamMissing(String fileName)
        {
            if (_mediator.MainAssembly == typeof (Object).Assembly && _mediator.BaseName.Equals("mscorlib"))
            {
                Contract.Assert(false, "Couldn't get mscorlib" + ResourceManager.ResFileExtension + " from mscorlib's assembly" + Environment.NewLine + Environment.NewLine + "Are you building the runtime on your machine?  Chances are the BCL directory didn't build correctly.  Type 'build -c' in the BCL directory.  If you get build errors, look at buildd.log.  If you then can't figure out what's wrong (and you aren't changing the assembly-related metadata code), ask a BCL dev.\n\nIf you did NOT build the runtime, you shouldn't be seeing this and you've found a bug.");
                string mesgFailFast = "mscorlib" + ResourceManager.ResFileExtension + " couldn't be found!  Large parts of the BCL won't work!";
                System.Environment.FailFast(mesgFailFast);
            }

            String resName = String.Empty;
            if (_mediator.LocationInfo != null && _mediator.LocationInfo.Namespace != null)
                resName = _mediator.LocationInfo.Namespace + Type.Delimiter;
            resName += fileName;
            throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoNeutralAsm", resName, _mediator.MainAssembly.GetSimpleName()));
        }

        internal static extern bool GetNeutralResourcesLanguageAttribute(RuntimeAssembly assemblyHandle, StringHandleOnStack cultureName, out short fallbackLocation);
    }
}