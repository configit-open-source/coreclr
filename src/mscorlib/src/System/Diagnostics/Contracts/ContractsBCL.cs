using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Security;

namespace System.Diagnostics.Contracts
{
    public static partial class Contract
    {
        private static bool _assertingMustUseRewriter;
        static partial void AssertMustUseRewriter(ContractFailureKind kind, String contractKind)
        {
            if (_assertingMustUseRewriter)
                System.Diagnostics.Assert.Fail("Asserting that we must use the rewriter went reentrant.", "Didn't rewrite this mscorlib?");
            _assertingMustUseRewriter = true;
            Assembly thisAssembly = typeof (Contract).Assembly;
            StackTrace stack = new StackTrace();
            Assembly probablyNotRewritten = null;
            for (int i = 0; i < stack.FrameCount; i++)
            {
                Assembly caller = stack.GetFrame(i).GetMethod().DeclaringType.Assembly;
                if (caller != thisAssembly)
                {
                    probablyNotRewritten = caller;
                    break;
                }
            }

            if (probablyNotRewritten == null)
                probablyNotRewritten = thisAssembly;
            String simpleName = probablyNotRewritten.GetName().Name;
            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(kind, Environment.GetResourceString("MustUseCCRewrite", contractKind, simpleName), null, null, null);
            _assertingMustUseRewriter = false;
        }

        static partial void ReportFailure(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", failureKind), "failureKind");
            Contract.EndContractBlock();
            var displayMessage = System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
            if (displayMessage == null)
                return;
            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(failureKind, displayMessage, userMessage, conditionText, innerException);
        }

        public static event EventHandler<ContractFailedEventArgs> ContractFailed
        {
            [SecurityCritical]
            add
            {
                System.Runtime.CompilerServices.ContractHelper.InternalContractFailed += value;
            }

            [SecurityCritical]
            remove
            {
                System.Runtime.CompilerServices.ContractHelper.InternalContractFailed -= value;
            }
        }
    }

    public sealed class ContractFailedEventArgs : EventArgs
    {
        private ContractFailureKind _failureKind;
        private String _message;
        private String _condition;
        private Exception _originalException;
        private bool _handled;
        private bool _unwind;
        internal Exception thrownDuringHandler;
        public ContractFailedEventArgs(ContractFailureKind failureKind, String message, String condition, Exception originalException)
        {
            Contract.Requires(originalException == null || failureKind == ContractFailureKind.PostconditionOnException);
            _failureKind = failureKind;
            _message = message;
            _condition = condition;
            _originalException = originalException;
        }

        public String Message
        {
            get
            {
                return _message;
            }
        }

        public String Condition
        {
            get
            {
                return _condition;
            }
        }

        public ContractFailureKind FailureKind
        {
            get
            {
                return _failureKind;
            }
        }

        public Exception OriginalException
        {
            get
            {
                return _originalException;
            }
        }

        public bool Handled
        {
            get
            {
                return _handled;
            }
        }

        public void SetHandled()
        {
            _handled = true;
        }

        public bool Unwind
        {
            get
            {
                return _unwind;
            }
        }

        public void SetUnwind()
        {
            _unwind = true;
        }
    }

    internal sealed class ContractException : Exception
    {
        readonly ContractFailureKind _Kind;
        readonly string _UserMessage;
        readonly string _Condition;
        public ContractFailureKind Kind
        {
            get
            {
                return _Kind;
            }
        }

        public string Failure
        {
            get
            {
                return this.Message;
            }
        }

        public string UserMessage
        {
            get
            {
                return _UserMessage;
            }
        }

        public string Condition
        {
            get
            {
                return _Condition;
            }
        }

        private ContractException()
        {
            HResult = System.Runtime.CompilerServices.ContractHelper.COR_E_CODECONTRACTFAILED;
        }

        public ContractException(ContractFailureKind kind, string failure, string userMessage, string condition, Exception innerException): base (failure, innerException)
        {
            HResult = System.Runtime.CompilerServices.ContractHelper.COR_E_CODECONTRACTFAILED;
            this._Kind = kind;
            this._UserMessage = userMessage;
            this._Condition = condition;
        }

        private ContractException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context): base (info, context)
        {
            _Kind = (ContractFailureKind)info.GetInt32("Kind");
            _UserMessage = info.GetString("UserMessage");
            _Condition = info.GetString("Condition");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Kind", _Kind);
            info.AddValue("UserMessage", _UserMessage);
            info.AddValue("Condition", _Condition);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public static partial class ContractHelper
    {
        private static volatile EventHandler<ContractFailedEventArgs> contractFailedEvent;
        private static readonly Object lockObject = new Object();
        internal const int COR_E_CODECONTRACTFAILED = unchecked ((int)0x80131542);
        internal static event EventHandler<ContractFailedEventArgs> InternalContractFailed
        {
            [SecurityCritical]
            add
            {
                System.Runtime.CompilerServices.RuntimeHelpers.PrepareContractedDelegate(value);
                lock (lockObject)
                {
                    contractFailedEvent += value;
                }
            }

            [SecurityCritical]
            remove
            {
                lock (lockObject)
                {
                    contractFailedEvent -= value;
                }
            }
        }

        static partial void RaiseContractFailedEventImplementation(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException, ref string resultFailureMessage)
        {
            if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", failureKind), "failureKind");
            Contract.EndContractBlock();
            string returnValue;
            String displayMessage = "contract failed.";
            ContractFailedEventArgs eventArgs = null;
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                displayMessage = GetDisplayMessage(failureKind, userMessage, conditionText);
                EventHandler<ContractFailedEventArgs> contractFailedEventLocal = contractFailedEvent;
                if (contractFailedEventLocal != null)
                {
                    eventArgs = new ContractFailedEventArgs(failureKind, displayMessage, conditionText, innerException);
                    foreach (EventHandler<ContractFailedEventArgs> handler in contractFailedEventLocal.GetInvocationList())
                    {
                        try
                        {
                            handler(null, eventArgs);
                        }
                        catch (Exception e)
                        {
                            eventArgs.thrownDuringHandler = e;
                            eventArgs.SetUnwind();
                        }
                    }

                    if (eventArgs.Unwind)
                    {
                        if (innerException == null)
                        {
                            innerException = eventArgs.thrownDuringHandler;
                        }

                        throw new ContractException(failureKind, displayMessage, userMessage, conditionText, innerException);
                    }
                }
            }
            finally
            {
                if (eventArgs != null && eventArgs.Handled)
                {
                    returnValue = null;
                }
                else
                {
                    returnValue = displayMessage;
                }
            }

            resultFailureMessage = returnValue;
        }

        static partial void TriggerFailureImplementation(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            if (!Environment.UserInteractive)
            {
                throw new ContractException(kind, displayMessage, userMessage, conditionText, innerException);
            }

            String windowTitle = Environment.GetResourceString(GetResourceNameForFailure(kind));
            const int numStackFramesToSkip = 2;
            System.Diagnostics.Assert.Fail(conditionText, displayMessage, windowTitle, COR_E_CODECONTRACTFAILED, StackTrace.TraceFormat.Normal, numStackFramesToSkip);
        }

        private static String GetResourceNameForFailure(ContractFailureKind failureKind)
        {
            String resourceName = null;
            switch (failureKind)
            {
                case ContractFailureKind.Assert:
                    resourceName = "AssertionFailed";
                    break;
                case ContractFailureKind.Assume:
                    resourceName = "AssumptionFailed";
                    break;
                case ContractFailureKind.Precondition:
                    resourceName = "PreconditionFailed";
                    break;
                case ContractFailureKind.Postcondition:
                    resourceName = "PostconditionFailed";
                    break;
                case ContractFailureKind.Invariant:
                    resourceName = "InvariantFailed";
                    break;
                case ContractFailureKind.PostconditionOnException:
                    resourceName = "PostconditionOnExceptionFailed";
                    break;
                default:
                    Contract.Assume(false, "Unreachable code");
                    resourceName = "AssumptionFailed";
                    break;
            }

            return resourceName;
        }

        private static String GetDisplayMessage(ContractFailureKind failureKind, String userMessage, String conditionText)
        {
            String resourceName = GetResourceNameForFailure(failureKind);
            String failureMessage;
            if (!String.IsNullOrEmpty(conditionText))
            {
                resourceName += "_Cnd";
                failureMessage = Environment.GetResourceString(resourceName, conditionText);
            }
            else
            {
                failureMessage = Environment.GetResourceString(resourceName);
            }

            if (!String.IsNullOrEmpty(userMessage))
            {
                return failureMessage + "  " + userMessage;
            }
            else
            {
                return failureMessage;
            }
        }
    }
}