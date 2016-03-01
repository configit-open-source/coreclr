namespace System.Runtime.CompilerServices
{
    public sealed class IteratorStateMachineAttribute : StateMachineAttribute
    {
        public IteratorStateMachineAttribute(Type stateMachineType): base (stateMachineType)
        {
        }
    }
}