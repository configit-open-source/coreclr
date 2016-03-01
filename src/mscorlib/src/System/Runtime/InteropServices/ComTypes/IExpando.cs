namespace System.Runtime.InteropServices.ComTypes
{
    using System;
    using System.Reflection;

    internal interface IExpando : IReflect
    {
        FieldInfo AddField(String name);
        PropertyInfo AddProperty(String name);
        MethodInfo AddMethod(String name, Delegate method);
        void RemoveMember(MemberInfo m);
    }
}