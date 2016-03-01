namespace System.Runtime.CompilerServices
{
    using System;
    using System.Diagnostics.Contracts;

    internal sealed class TypeDependencyAttribute : Attribute
    {
        private string typeName;
        public TypeDependencyAttribute(string typeName)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            Contract.EndContractBlock();
            this.typeName = typeName;
        }
    }
}