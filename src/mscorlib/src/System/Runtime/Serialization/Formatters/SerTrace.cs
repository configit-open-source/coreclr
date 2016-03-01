namespace System.Runtime.Serialization.Formatters
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    public sealed class InternalRM
    {
        public static void InfoSoap(params Object[] messages)
        {
            BCLDebug.Trace("SOAP", messages);
        }

        public static bool SoapCheckEnabled()
        {
            return BCLDebug.CheckEnabled("SOAP");
        }
    }

    public sealed class InternalST
    {
        private InternalST()
        {
        }

        public static void InfoSoap(params Object[] messages)
        {
            BCLDebug.Trace("SOAP", messages);
        }

        public static bool SoapCheckEnabled()
        {
            return BCLDebug.CheckEnabled("Soap");
        }

        public static void Soap(params Object[] messages)
        {
            if (!(messages[0] is String))
                messages[0] = (messages[0].GetType()).Name + " ";
            else
                messages[0] = messages[0] + " ";
            BCLDebug.Trace("SOAP", messages);
        }

        public static void SoapAssert(bool condition, String message)
        {
            Contract.Assert(condition, message);
        }

        public static void SerializationSetValue(FieldInfo fi, Object target, Object value)
        {
            if (fi == null)
                throw new ArgumentNullException("fi");
            if (target == null)
                throw new ArgumentNullException("target");
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            FormatterServices.SerializationSetValue(fi, target, value);
        }

        public static Assembly LoadAssemblyFromString(String assemblyString)
        {
            return FormatterServices.LoadAssemblyFromString(assemblyString);
        }
    }

    internal static class SerTrace
    {
        internal static void InfoLog(params Object[] messages)
        {
            BCLDebug.Trace("BINARY", messages);
        }

        internal static void Log(params Object[] messages)
        {
            if (!(messages[0] is String))
                messages[0] = (messages[0].GetType()).Name + " ";
            else
                messages[0] = messages[0] + " ";
            BCLDebug.Trace("BINARY", messages);
        }
    }
}