namespace System.Runtime.CompilerServices
{
    public sealed class RequiredAttributeAttribute : Attribute
    {
        private Type requiredContract;
        public RequiredAttributeAttribute(Type requiredContract)
        {
            this.requiredContract = requiredContract;
        }

        public Type RequiredContract
        {
            get
            {
                return this.requiredContract;
            }
        }
    }
}