using System;
using System.Reflection;

using Microsoft.Reflection;

namespace System.Diagnostics.Tracing.Internal
{
    internal static class Environment
    {
        public static readonly string NewLine = System.Environment.NewLine;
        public static int TickCount
        {
            get
            {
                return System.Environment.TickCount;
            }
        }

        public static string GetResourceString(string key, params object[] args)
        {
            string fmt = rm.GetString(key);
            if (fmt != null)
                return string.Format(fmt, args);
            string sargs = String.Empty;
            foreach (var arg in args)
            {
                if (sargs != String.Empty)
                    sargs += ", ";
                sargs += arg.ToString();
            }

            return key + " (" + sargs + ")";
        }

        public static string GetRuntimeResourceString(string key, params object[] args)
        {
            return GetResourceString(key, args);
        }

        private static System.Resources.ResourceManager rm = new System.Resources.ResourceManager("Microsoft.Diagnostics.Tracing.Messages", typeof (Environment).Assembly());
    }
}

namespace Microsoft.Reflection
{
    static class ReflectionExtensions
    {
        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool IsAbstract(this Type type)
        {
            return type.IsAbstract;
        }

        public static bool IsSealed(this Type type)
        {
            return type.IsSealed;
        }

        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static Type BaseType(this Type type)
        {
            return type.BaseType;
        }

        public static Assembly Assembly(this Type type)
        {
            return type.Assembly;
        }

        public static TypeCode GetTypeCode(this Type type)
        {
            return Type.GetTypeCode(type);
        }

        public static bool ReflectionOnly(this Assembly assm)
        {
            return assm.ReflectionOnly;
        }
    }
}