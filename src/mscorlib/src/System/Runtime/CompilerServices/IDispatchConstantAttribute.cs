using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    public sealed class IDispatchConstantAttribute : CustomConstantAttribute
    {
        public IDispatchConstantAttribute()
        {
        }

        public override Object Value
        {
            get
            {
                return new DispatchWrapper(null);
            }
        }
    }
}