using System;

namespace System.Runtime.Versioning
{
    [Flags]
    public enum ComponentGuaranteesOptions
    {
        None = 0,
        Exchange = 0x1,
        Stable = 0x2,
        SideBySide = 0x4
    }

    public sealed class ComponentGuaranteesAttribute : Attribute
    {
        private ComponentGuaranteesOptions _guarantees;
        public ComponentGuaranteesAttribute(ComponentGuaranteesOptions guarantees)
        {
            _guarantees = guarantees;
        }

        public ComponentGuaranteesOptions Guarantees
        {
            get
            {
                return _guarantees;
            }
        }
    }
}