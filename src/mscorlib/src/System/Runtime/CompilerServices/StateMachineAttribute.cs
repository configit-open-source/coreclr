using System;

namespace System.Runtime.CompilerServices
{
    public class StateMachineAttribute : Attribute
    {
        public Type StateMachineType
        {
            get;
            private set;
        }

        public StateMachineAttribute(Type stateMachineType)
        {
            this.StateMachineType = stateMachineType;
        }
    }
}