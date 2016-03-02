

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface ICustomProperty
    {
        Type Type
        {
            
            get;
        }

        string Name
        {
            
            get;
        }

        object GetValue(object target);
        void SetValue(object target, object value);
        object GetValue(object target, object indexValue);
        void SetValue(object target, object value, object indexValue);
        bool CanWrite
        {
            
            get;
        }

        bool CanRead
        {
            
            get;
        }
    }
}