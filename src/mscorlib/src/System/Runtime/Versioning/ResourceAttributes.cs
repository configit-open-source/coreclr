
using System.Globalization;
using System.Text;

using Microsoft.Win32;

namespace System.Runtime.Versioning
{
    public sealed class ResourceConsumptionAttribute : Attribute
    {
        private ResourceScope _consumptionScope;
        private ResourceScope _resourceScope;
        public ResourceConsumptionAttribute(ResourceScope resourceScope)
        {
            _resourceScope = resourceScope;
            _consumptionScope = _resourceScope;
        }

        public ResourceConsumptionAttribute(ResourceScope resourceScope, ResourceScope consumptionScope)
        {
            _resourceScope = resourceScope;
            _consumptionScope = consumptionScope;
        }

        public ResourceScope ResourceScope
        {
            get
            {
                return _resourceScope;
            }
        }

        public ResourceScope ConsumptionScope
        {
            get
            {
                return _consumptionScope;
            }
        }
    }

    public sealed class ResourceExposureAttribute : Attribute
    {
        private ResourceScope _resourceExposureLevel;
        public ResourceExposureAttribute(ResourceScope exposureLevel)
        {
            _resourceExposureLevel = exposureLevel;
        }

        public ResourceScope ResourceExposureLevel
        {
            get
            {
                return _resourceExposureLevel;
            }
        }
    }

    [Flags]
    public enum ResourceScope
    {
        None = 0,
        Machine = 0x1,
        Process = 0x2,
        AppDomain = 0x4,
        Library = 0x8,
        Private = 0x10,
        Assembly = 0x20
    }

    [Flags]
    internal enum SxSRequirements
    {
        None = 0,
        AppDomainID = 0x1,
        ProcessID = 0x2,
        CLRInstanceID = 0x4,
        AssemblyName = 0x8,
        TypeName = 0x10
    }

    public static class VersioningHelper
    {
        private const ResourceScope ResTypeMask = ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library;
        private const ResourceScope VisibilityMask = ResourceScope.Private | ResourceScope.Assembly;
        private static extern int GetRuntimeId();
        public static String MakeVersionSafeName(String name, ResourceScope from, ResourceScope to)
        {
            return MakeVersionSafeName(name, from, to, null);
        }

        public static String MakeVersionSafeName(String name, ResourceScope from, ResourceScope to, Type type)
        {
            ResourceScope fromResType = from & ResTypeMask;
            ResourceScope toResType = to & ResTypeMask;
            if (fromResType > toResType)
                throw new ArgumentException(Environment.GetResourceString("Argument_ResourceScopeWrongDirection", fromResType, toResType), "from");
            SxSRequirements requires = GetRequirements(to, from);
            if ((requires & (SxSRequirements.AssemblyName | SxSRequirements.TypeName)) != 0 && type == null)
                throw new ArgumentNullException("type", Environment.GetResourceString("ArgumentNull_TypeRequiredByResourceScope"));
            StringBuilder safeName = new StringBuilder(name);
            char separator = '_';
            if ((requires & SxSRequirements.ProcessID) != 0)
            {
                safeName.Append(separator);
                safeName.Append('p');
                safeName.Append(Win32Native.GetCurrentProcessId());
            }

            if ((requires & SxSRequirements.CLRInstanceID) != 0)
            {
                String clrID = GetCLRInstanceString();
                safeName.Append(separator);
                safeName.Append('r');
                safeName.Append(clrID);
            }

            if ((requires & SxSRequirements.AppDomainID) != 0)
            {
                safeName.Append(separator);
                safeName.Append("ad");
                safeName.Append(AppDomain.CurrentDomain.Id);
            }

            if ((requires & SxSRequirements.TypeName) != 0)
            {
                safeName.Append(separator);
                safeName.Append(type.Name);
            }

            if ((requires & SxSRequirements.AssemblyName) != 0)
            {
                safeName.Append(separator);
                safeName.Append(type.Assembly.FullName);
            }

            return safeName.ToString();
        }

        private static String GetCLRInstanceString()
        {
            int id = GetRuntimeId();
            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static SxSRequirements GetRequirements(ResourceScope consumeAsScope, ResourceScope calleeScope)
        {
            SxSRequirements requires = SxSRequirements.None;
            switch (calleeScope & ResTypeMask)
            {
                case ResourceScope.Machine:
                    switch (consumeAsScope & ResTypeMask)
                    {
                        case ResourceScope.Machine:
                            break;
                        case ResourceScope.Process:
                            requires |= SxSRequirements.ProcessID;
                            break;
                        case ResourceScope.AppDomain:
                            requires |= SxSRequirements.AppDomainID | SxSRequirements.CLRInstanceID | SxSRequirements.ProcessID;
                            break;
                        default:
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadResourceScopeTypeBits", consumeAsScope), "consumeAsScope");
                    }

                    break;
                case ResourceScope.Process:
                    if ((consumeAsScope & ResourceScope.AppDomain) != 0)
                        requires |= SxSRequirements.AppDomainID | SxSRequirements.CLRInstanceID;
                    break;
                case ResourceScope.AppDomain:
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadResourceScopeTypeBits", calleeScope), "calleeScope");
            }

            switch (calleeScope & VisibilityMask)
            {
                case ResourceScope.None:
                    switch (consumeAsScope & VisibilityMask)
                    {
                        case ResourceScope.None:
                            break;
                        case ResourceScope.Assembly:
                            requires |= SxSRequirements.AssemblyName;
                            break;
                        case ResourceScope.Private:
                            requires |= SxSRequirements.TypeName | SxSRequirements.AssemblyName;
                            break;
                        default:
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadResourceScopeVisibilityBits", consumeAsScope), "consumeAsScope");
                    }

                    break;
                case ResourceScope.Assembly:
                    if ((consumeAsScope & ResourceScope.Private) != 0)
                        requires |= SxSRequirements.TypeName;
                    break;
                case ResourceScope.Private:
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadResourceScopeVisibilityBits", calleeScope), "calleeScope");
            }

            if (consumeAsScope == calleeScope)
            {
                            }

            return requires;
        }
    }
}