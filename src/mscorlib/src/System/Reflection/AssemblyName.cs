using System.Configuration.Assemblies;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Reflection
{
    public sealed class AssemblyName : _AssemblyName, ICloneable, ISerializable, IDeserializationCallback
    {
        private String _Name;
        private byte[] _PublicKey;
        private byte[] _PublicKeyToken;
        private CultureInfo _CultureInfo;
        private String _CodeBase;
        private Version _Version;
        private StrongNameKeyPair _StrongNameKeyPair;
        private byte[] _HashForControl;
        private AssemblyHashAlgorithm _HashAlgorithm;
        private AssemblyHashAlgorithm _HashAlgorithmForControl;
        private AssemblyVersionCompatibility _VersionCompatibility;
        private AssemblyNameFlags _Flags;
        public AssemblyName()
        {
            _HashAlgorithm = AssemblyHashAlgorithm.None;
            _VersionCompatibility = AssemblyVersionCompatibility.SameMachine;
            _Flags = AssemblyNameFlags.None;
        }

        public String Name
        {
            get
            {
                return _Name;
            }

            set
            {
                _Name = value;
            }
        }

        public Version Version
        {
            get
            {
                return _Version;
            }

            set
            {
                _Version = value;
            }
        }

        public CultureInfo CultureInfo
        {
            get
            {
                return _CultureInfo;
            }

            set
            {
                _CultureInfo = value;
            }
        }

        public String CultureName
        {
            get
            {
                return (_CultureInfo == null) ? null : _CultureInfo.Name;
            }

            set
            {
                _CultureInfo = (value == null) ? null : new CultureInfo(value);
            }
        }

        public String CodeBase
        {
            [System.Security.SecurityCritical]
            get
            {
                return _CodeBase;
            }

            [System.Security.SecurityCritical]
            set
            {
                _CodeBase = value;
            }
        }

        public String EscapedCodeBase
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_CodeBase == null)
                    return null;
                else
                    return EscapeCodeBase(_CodeBase);
            }
        }

        public ProcessorArchitecture ProcessorArchitecture
        {
            get
            {
                int x = (((int)_Flags) & 0x70) >> 4;
                if (x > 5)
                    x = 0;
                return (ProcessorArchitecture)x;
            }

            set
            {
                int x = ((int)value) & 0x07;
                if (x <= 5)
                {
                    _Flags = (AssemblyNameFlags)((int)_Flags & 0xFFFFFF0F);
                    _Flags |= (AssemblyNameFlags)(x << 4);
                }
            }
        }

        public AssemblyContentType ContentType
        {
            get
            {
                int x = (((int)_Flags) & 0x00000E00) >> 9;
                if (x > 1)
                    x = 0;
                return (AssemblyContentType)x;
            }

            set
            {
                int x = ((int)value) & 0x07;
                if (x <= 1)
                {
                    _Flags = (AssemblyNameFlags)((int)_Flags & 0xFFFFF1FF);
                    _Flags |= (AssemblyNameFlags)(x << 9);
                }
            }
        }

        public Object Clone()
        {
            AssemblyName name = new AssemblyName();
            name.Init(_Name, _PublicKey, _PublicKeyToken, _Version, _CultureInfo, _HashAlgorithm, _VersionCompatibility, _CodeBase, _Flags, _StrongNameKeyPair);
            name._HashForControl = _HashForControl;
            name._HashAlgorithmForControl = _HashAlgorithmForControl;
            return name;
        }

        static public AssemblyName GetAssemblyName(String assemblyFile)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException("assemblyFile");
            Contract.EndContractBlock();
            String fullPath = Path.GetFullPathInternal(assemblyFile);
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, fullPath).Demand();
            return nGetFileInformation(fullPath);
        }

        internal void SetHashControl(byte[] hash, AssemblyHashAlgorithm hashAlgorithm)
        {
            _HashForControl = hash;
            _HashAlgorithmForControl = hashAlgorithm;
        }

        public byte[] GetPublicKey()
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && _PublicKey == null)
                return EmptyArray<Byte>.Value;
            else
                return _PublicKey;
        }

        public void SetPublicKey(byte[] publicKey)
        {
            _PublicKey = publicKey;
            if (publicKey == null)
                _Flags &= ~AssemblyNameFlags.PublicKey;
            else
                _Flags |= AssemblyNameFlags.PublicKey;
        }

        public byte[] GetPublicKeyToken()
        {
            if (_PublicKeyToken == null)
                _PublicKeyToken = nGetPublicKeyToken();
            return _PublicKeyToken;
        }

        public void SetPublicKeyToken(byte[] publicKeyToken)
        {
            _PublicKeyToken = publicKeyToken;
        }

        public AssemblyNameFlags Flags
        {
            get
            {
                return (AssemblyNameFlags)((uint)_Flags & 0xFFFFF10F);
            }

            set
            {
                _Flags &= unchecked ((AssemblyNameFlags)0x00000EF0);
                _Flags |= (value & unchecked ((AssemblyNameFlags)0xFFFFF10F));
            }
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                return _HashAlgorithm;
            }

            set
            {
                _HashAlgorithm = value;
            }
        }

        public AssemblyVersionCompatibility VersionCompatibility
        {
            get
            {
                return _VersionCompatibility;
            }

            set
            {
                _VersionCompatibility = value;
            }
        }

        public StrongNameKeyPair KeyPair
        {
            get
            {
                return _StrongNameKeyPair;
            }

            set
            {
                _StrongNameKeyPair = value;
            }
        }

        public String FullName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String name = nToString();
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && string.IsNullOrEmpty(name))
                    return base.ToString();
                return name;
            }
        }

        public override String ToString()
        {
            String s = FullName;
            if (s == null)
                return base.ToString();
            else
                return s;
        }

        public AssemblyName(String assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            if ((assemblyName.Length == 0) || (assemblyName[0] == '\0'))
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
            _Name = assemblyName;
            nInit();
        }

        static public bool ReferenceMatchesDefinition(AssemblyName reference, AssemblyName definition)
        {
            if (Object.ReferenceEquals(reference, definition))
            {
                return true;
            }

            return ReferenceMatchesDefinitionInternal(reference, definition, true);
        }

        static internal extern bool ReferenceMatchesDefinitionInternal(AssemblyName reference, AssemblyName definition, bool parse);
        internal extern void nInit(out RuntimeAssembly assembly, bool forIntrospection, bool raiseResolveEvent);
        internal void nInit()
        {
            RuntimeAssembly dummy = null;
            nInit(out dummy, false, false);
        }

        internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
        {
            ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _Flags);
        }

        internal static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
        {
            if (((uint)flags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;
            if ((pek & System.Reflection.PortableExecutableKinds.PE32Plus) == System.Reflection.PortableExecutableKinds.PE32Plus)
            {
                switch (ifm)
                {
                    case System.Reflection.ImageFileMachine.IA64:
                        return ProcessorArchitecture.IA64;
                    case System.Reflection.ImageFileMachine.AMD64:
                        return ProcessorArchitecture.Amd64;
                    case System.Reflection.ImageFileMachine.I386:
                        if ((pek & System.Reflection.PortableExecutableKinds.ILOnly) == System.Reflection.PortableExecutableKinds.ILOnly)
                            return ProcessorArchitecture.MSIL;
                        break;
                }
            }
            else
            {
                if (ifm == System.Reflection.ImageFileMachine.I386)
                {
                    if ((pek & System.Reflection.PortableExecutableKinds.Required32Bit) == System.Reflection.PortableExecutableKinds.Required32Bit)
                        return ProcessorArchitecture.X86;
                    if ((pek & System.Reflection.PortableExecutableKinds.ILOnly) == System.Reflection.PortableExecutableKinds.ILOnly)
                        return ProcessorArchitecture.MSIL;
                    return ProcessorArchitecture.X86;
                }

                if (ifm == System.Reflection.ImageFileMachine.ARM)
                {
                    return ProcessorArchitecture.Arm;
                }
            }

            return ProcessorArchitecture.None;
        }

        internal void Init(String name, byte[] publicKey, byte[] publicKeyToken, Version version, CultureInfo cultureInfo, AssemblyHashAlgorithm hashAlgorithm, AssemblyVersionCompatibility versionCompatibility, String codeBase, AssemblyNameFlags flags, StrongNameKeyPair keyPair)
        {
            _Name = name;
            if (publicKey != null)
            {
                _PublicKey = new byte[publicKey.Length];
                Array.Copy(publicKey, _PublicKey, publicKey.Length);
            }

            if (publicKeyToken != null)
            {
                _PublicKeyToken = new byte[publicKeyToken.Length];
                Array.Copy(publicKeyToken, _PublicKeyToken, publicKeyToken.Length);
            }

            if (version != null)
                _Version = (Version)version.Clone();
            _CultureInfo = cultureInfo;
            _HashAlgorithm = hashAlgorithm;
            _VersionCompatibility = versionCompatibility;
            _CodeBase = codeBase;
            _Flags = flags;
            _StrongNameKeyPair = keyPair;
        }

        static internal extern AssemblyName nGetFileInformation(String s);
        private extern String nToString();
        private extern byte[] nGetPublicKeyToken();
        static internal extern String EscapeCodeBase(String codeBase);
    }
}