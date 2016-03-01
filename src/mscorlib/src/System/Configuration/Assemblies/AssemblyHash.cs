namespace System.Configuration.Assemblies
{
    using System;

    public struct AssemblyHash : ICloneable
    {
        private AssemblyHashAlgorithm _Algorithm;
        private byte[] _Value;
        public static readonly AssemblyHash Empty = new AssemblyHash(AssemblyHashAlgorithm.None, null);
        public AssemblyHash(byte[] value)
        {
            _Algorithm = AssemblyHashAlgorithm.SHA1;
            _Value = null;
            if (value != null)
            {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }

        public AssemblyHash(AssemblyHashAlgorithm algorithm, byte[] value)
        {
            _Algorithm = algorithm;
            _Value = null;
            if (value != null)
            {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }

        public AssemblyHashAlgorithm Algorithm
        {
            get
            {
                return _Algorithm;
            }

            set
            {
                _Algorithm = value;
            }
        }

        public byte[] GetValue()
        {
            return _Value;
        }

        public void SetValue(byte[] value)
        {
            _Value = value;
        }

        public Object Clone()
        {
            return new AssemblyHash(_Algorithm, _Value);
        }
    }
}