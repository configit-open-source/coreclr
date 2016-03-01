using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Diagnostics.Contracts
{
    public sealed class PureAttribute : Attribute
    {
    }

    public sealed class ContractClassAttribute : Attribute
    {
        private Type _typeWithContracts;
        public ContractClassAttribute(Type typeContainingContracts)
        {
            _typeWithContracts = typeContainingContracts;
        }

        public Type TypeContainingContracts
        {
            get
            {
                return _typeWithContracts;
            }
        }
    }

    public sealed class ContractClassForAttribute : Attribute
    {
        private Type _typeIAmAContractFor;
        public ContractClassForAttribute(Type typeContractsAreFor)
        {
            _typeIAmAContractFor = typeContractsAreFor;
        }

        public Type TypeContractsAreFor
        {
            get
            {
                return _typeIAmAContractFor;
            }
        }
    }

    public sealed class ContractInvariantMethodAttribute : Attribute
    {
    }

    public sealed class ContractReferenceAssemblyAttribute : Attribute
    {
    }

    public sealed class ContractRuntimeIgnoredAttribute : Attribute
    {
    }

    public sealed class ContractVerificationAttribute : Attribute
    {
        private bool _value;
        public ContractVerificationAttribute(bool value)
        {
            _value = value;
        }

        public bool Value
        {
            get
            {
                return _value;
            }
        }
    }

    public sealed class ContractPublicPropertyNameAttribute : Attribute
    {
        private String _publicName;
        public ContractPublicPropertyNameAttribute(String name)
        {
            _publicName = name;
        }

        public String Name
        {
            get
            {
                return _publicName;
            }
        }
    }

    public sealed class ContractArgumentValidatorAttribute : Attribute
    {
    }

    public sealed class ContractAbbreviatorAttribute : Attribute
    {
    }

    public sealed class ContractOptionAttribute : Attribute
    {
        private String _category;
        private String _setting;
        private bool _enabled;
        private String _value;
        public ContractOptionAttribute(String category, String setting, bool enabled)
        {
            _category = category;
            _setting = setting;
            _enabled = enabled;
        }

        public ContractOptionAttribute(String category, String setting, String value)
        {
            _category = category;
            _setting = setting;
            _value = value;
        }

        public String Category
        {
            get
            {
                return _category;
            }
        }

        public String Setting
        {
            get
            {
                return _setting;
            }
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
        }

        public String Value
        {
            get
            {
                return _value;
            }
        }
    }

    public static partial class Contract
    {
        public static void Assume(bool condition)
        {
            if (!condition)
            {
                ReportFailure(ContractFailureKind.Assume, null, null, null);
            }
        }

        public static void Assume(bool condition, String userMessage)
        {
            if (!condition)
            {
                ReportFailure(ContractFailureKind.Assume, userMessage, null, null);
            }
        }

        public static void Assert(bool condition)
        {
            if (!condition)
                ReportFailure(ContractFailureKind.Assert, null, null, null);
        }

        public static void Assert(bool condition, String userMessage)
        {
            if (!condition)
                ReportFailure(ContractFailureKind.Assert, userMessage, null, null);
        }

        public static void Requires(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
        }

        public static void Requires(bool condition, String userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
        }

        public static void Requires<TException>(bool condition)where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
        }

        public static void Requires<TException>(bool condition, String userMessage)where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
        }

        public static void Ensures(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
        }

        public static void Ensures(bool condition, String userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
        }

        public static void EnsuresOnThrow<TException>(bool condition)where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
        }

        public static void EnsuresOnThrow<TException>(bool condition, String userMessage)where TException : Exception
        {
            AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
        }

        public static T Result<T>()
        {
            return default (T);
        }

        public static T ValueAtReturn<T>(out T value)
        {
            value = default (T);
            return value;
        }

        public static T OldValue<T>(T value)
        {
            return default (T);
        }

        public static void Invariant(bool condition)
        {
            AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
        }

        public static void Invariant(bool condition, String userMessage)
        {
            AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
        }

        public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
                throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();
            for (int i = fromInclusive; i < toExclusive; i++)
                if (!predicate(i))
                    return false;
            return true;
        }

        public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();
            foreach (T t in collection)
                if (!predicate(t))
                    return false;
            return true;
        }

        public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
        {
            if (fromInclusive > toExclusive)
                throw new ArgumentException(Environment.GetResourceString("Argument_ToExclusiveLessThanFromExclusive"));
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();
            for (int i = fromInclusive; i < toExclusive; i++)
                if (predicate(i))
                    return true;
            return false;
        }

        public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            Contract.EndContractBlock();
            foreach (T t in collection)
                if (predicate(t))
                    return true;
            return false;
        }

        public static void EndContractBlock()
        {
        }

        static partial void ReportFailure(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException);
        static partial void AssertMustUseRewriter(ContractFailureKind kind, String contractKind);
    }

    public enum ContractFailureKind
    {
        Precondition,
        Postcondition,
        PostconditionOnException,
        Invariant,
        Assert,
        Assume
    }
}

namespace System.Diagnostics.Contracts.Internal
{
    public static class ContractHelper
    {
        public static string RaiseContractFailedEvent(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            return System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
        }

        public static void TriggerFailure(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            System.Runtime.CompilerServices.ContractHelper.TriggerFailure(kind, displayMessage, userMessage, conditionText, innerException);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public static partial class ContractHelper
    {
        public static string RaiseContractFailedEvent(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException)
        {
            var resultFailureMessage = "Contract failed";
            RaiseContractFailedEventImplementation(failureKind, userMessage, conditionText, innerException, ref resultFailureMessage);
            return resultFailureMessage;
        }

        public static void TriggerFailure(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException)
        {
            TriggerFailureImplementation(kind, displayMessage, userMessage, conditionText, innerException);
        }

        static partial void RaiseContractFailedEventImplementation(ContractFailureKind failureKind, String userMessage, String conditionText, Exception innerException, ref string resultFailureMessage);
        static partial void TriggerFailureImplementation(ContractFailureKind kind, String displayMessage, String userMessage, String conditionText, Exception innerException);
    }
}