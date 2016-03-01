namespace System.Runtime.Serialization.Formatters
{
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System;

    public interface IFieldInfo
    {
        String[] FieldNames
        {
            [System.Security.SecurityCritical]
            get;
            [System.Security.SecurityCritical]
            set;
        }

        Type[] FieldTypes
        {
            [System.Security.SecurityCritical]
            get;
            [System.Security.SecurityCritical]
            set;
        }
    }
}