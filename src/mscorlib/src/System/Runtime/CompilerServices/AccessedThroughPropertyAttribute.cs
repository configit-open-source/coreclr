namespace System.Runtime.CompilerServices
{
    using System;

    public sealed class AccessedThroughPropertyAttribute : Attribute
    {
        private readonly string propertyName;
        public AccessedThroughPropertyAttribute(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public string PropertyName
        {
            get
            {
                return propertyName;
            }
        }
    }
}