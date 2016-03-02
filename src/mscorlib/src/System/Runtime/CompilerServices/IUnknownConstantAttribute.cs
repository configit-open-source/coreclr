using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    public sealed class IUnknownConstantAttribute : CustomConstantAttribute
    {
        public IUnknownConstantAttribute()
        {
        }

        public override Object Value
        {
            get
            {
                return new UnknownWrapper(null);
            }
        }
    }
}