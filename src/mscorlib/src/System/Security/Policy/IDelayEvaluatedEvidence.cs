namespace System.Security.Policy
{
    internal interface IDelayEvaluatedEvidence
    {
        bool IsVerified
        {
            [System.Security.SecurityCritical]
            get;
        }

        bool WasUsed
        {
            get;
        }

        void MarkUsed();
    }
}