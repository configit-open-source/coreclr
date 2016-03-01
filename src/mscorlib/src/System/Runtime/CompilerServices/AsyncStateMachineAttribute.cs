using System;

namespace System.Runtime.CompilerServices
{
    public sealed class AsyncStateMachineAttribute : StateMachineAttribute
    {
        public AsyncStateMachineAttribute(Type stateMachineType): base (stateMachineType)
        {
        }
    }
}