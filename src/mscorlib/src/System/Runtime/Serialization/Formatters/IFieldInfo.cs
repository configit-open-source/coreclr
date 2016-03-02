namespace System.Runtime.Serialization.Formatters
{
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