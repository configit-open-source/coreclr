using System;

namespace System.Runtime.CompilerServices
{
    public sealed class FixedBufferAttribute : Attribute
    {
        private Type elementType;
        private int length;
        public FixedBufferAttribute(Type elementType, int length)
        {
            this.elementType = elementType;
            this.length = length;
        }

        public Type ElementType
        {
            get
            {
                return elementType;
            }
        }

        public int Length
        {
            get
            {
                return length;
            }
        }
    }
}