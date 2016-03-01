namespace System.Runtime.Serialization
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;

    public sealed class OptionalFieldAttribute : Attribute
    {
        int versionAdded = 1;
        public OptionalFieldAttribute()
        {
        }

        public int VersionAdded
        {
            get
            {
                return this.versionAdded;
            }

            set
            {
                if (value < 1)
                    throw new ArgumentException(Environment.GetResourceString("Serialization_OptionalFieldVersionValue"));
                Contract.EndContractBlock();
                this.versionAdded = value;
            }
        }
    }

    public sealed class OnSerializingAttribute : Attribute
    {
    }

    public sealed class OnSerializedAttribute : Attribute
    {
    }

    public sealed class OnDeserializingAttribute : Attribute
    {
    }

    public sealed class OnDeserializedAttribute : Attribute
    {
    }
}