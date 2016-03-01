using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    internal class TypeNameBuilder
    {
        internal enum Format
        {
            ToString,
            FullName,
            AssemblyQualifiedName
        }

        private static extern IntPtr CreateTypeNameBuilder();
        private static extern void ReleaseTypeNameBuilder(IntPtr pAQN);
        private static extern void OpenGenericArguments(IntPtr tnb);
        private static extern void CloseGenericArguments(IntPtr tnb);
        private static extern void OpenGenericArgument(IntPtr tnb);
        private static extern void CloseGenericArgument(IntPtr tnb);
        private static extern void AddName(IntPtr tnb, string name);
        private static extern void AddPointer(IntPtr tnb);
        private static extern void AddByRef(IntPtr tnb);
        private static extern void AddSzArray(IntPtr tnb);
        private static extern void AddArray(IntPtr tnb, int rank);
        private static extern void AddAssemblySpec(IntPtr tnb, string assemblySpec);
        private static extern void ToString(IntPtr tnb, StringHandleOnStack retString);
        private static extern void Clear(IntPtr tnb);
        internal static string ToString(Type type, Format format)
        {
            if (format == Format.FullName || format == Format.AssemblyQualifiedName)
            {
                if (!type.IsGenericTypeDefinition && type.ContainsGenericParameters)
                    return null;
            }

            TypeNameBuilder tnb = new TypeNameBuilder(CreateTypeNameBuilder());
            tnb.Clear();
            tnb.ConstructAssemblyQualifiedNameWorker(type, format);
            string toString = tnb.ToString();
            tnb.Dispose();
            return toString;
        }

        private IntPtr m_typeNameBuilder;
        private TypeNameBuilder(IntPtr typeNameBuilder)
        {
            m_typeNameBuilder = typeNameBuilder;
        }

        internal void Dispose()
        {
            ReleaseTypeNameBuilder(m_typeNameBuilder);
        }

        private void AddElementType(Type elementType)
        {
            if (elementType.HasElementType)
                AddElementType(elementType.GetElementType());
            if (elementType.IsPointer)
                AddPointer();
            else if (elementType.IsByRef)
                AddByRef();
            else if (elementType.IsSzArray)
                AddSzArray();
            else if (elementType.IsArray)
                AddArray(elementType.GetArrayRank());
        }

        private void ConstructAssemblyQualifiedNameWorker(Type type, Format format)
        {
            Type rootType = type;
            while (rootType.HasElementType)
                rootType = rootType.GetElementType();
            List<Type> nestings = new List<Type>();
            for (Type t = rootType; t != null; t = t.IsGenericParameter ? null : t.DeclaringType)
                nestings.Add(t);
            for (int i = nestings.Count - 1; i >= 0; i--)
            {
                Type enclosingType = nestings[i];
                string name = enclosingType.Name;
                if (i == nestings.Count - 1 && enclosingType.Namespace != null && enclosingType.Namespace.Length != 0)
                    name = enclosingType.Namespace + "." + name;
                AddName(name);
            }

            if (rootType.IsGenericType && (!rootType.IsGenericTypeDefinition || format == Format.ToString))
            {
                Type[] genericArguments = rootType.GetGenericArguments();
                OpenGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Format genericArgumentsFormat = format == Format.FullName ? Format.AssemblyQualifiedName : format;
                    OpenGenericArgument();
                    ConstructAssemblyQualifiedNameWorker(genericArguments[i], genericArgumentsFormat);
                    CloseGenericArgument();
                }

                CloseGenericArguments();
            }

            AddElementType(type);
            if (format == Format.AssemblyQualifiedName)
                AddAssemblySpec(type.Module.Assembly.FullName);
        }

        private void OpenGenericArguments()
        {
            OpenGenericArguments(m_typeNameBuilder);
        }

        private void CloseGenericArguments()
        {
            CloseGenericArguments(m_typeNameBuilder);
        }

        private void OpenGenericArgument()
        {
            OpenGenericArgument(m_typeNameBuilder);
        }

        private void CloseGenericArgument()
        {
            CloseGenericArgument(m_typeNameBuilder);
        }

        private void AddName(string name)
        {
            AddName(m_typeNameBuilder, name);
        }

        private void AddPointer()
        {
            AddPointer(m_typeNameBuilder);
        }

        private void AddByRef()
        {
            AddByRef(m_typeNameBuilder);
        }

        private void AddSzArray()
        {
            AddSzArray(m_typeNameBuilder);
        }

        private void AddArray(int rank)
        {
            AddArray(m_typeNameBuilder, rank);
        }

        private void AddAssemblySpec(string assemblySpec)
        {
            AddAssemblySpec(m_typeNameBuilder, assemblySpec);
        }

        public override string ToString()
        {
            string ret = null;
            ToString(m_typeNameBuilder, JitHelpers.GetStringHandleOnStack(ref ret));
            return ret;
        }

        private void Clear()
        {
            Clear(m_typeNameBuilder);
        }
    }
}