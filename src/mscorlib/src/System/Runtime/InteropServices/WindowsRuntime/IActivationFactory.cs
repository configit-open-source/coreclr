using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public interface IActivationFactory
    {
        object ActivateInstance();
    }
}