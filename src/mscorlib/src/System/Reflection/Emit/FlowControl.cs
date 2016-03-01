namespace System.Reflection.Emit
{
    using System;

    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        Return = 7,
        Throw = 8
    }
}