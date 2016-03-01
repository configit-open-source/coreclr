namespace System.Runtime.InteropServices
{
    using System;
    using System.Reflection;

    internal interface UCOMIExpando : UCOMIReflect
    {
        FieldInfo AddField(String name);
        PropertyInfo AddProperty(String name);
        MethodInfo AddMethod(String name, Delegate method);
        void RemoveMember(MemberInfo m);
    }
}