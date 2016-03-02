namespace System.Runtime.InteropServices
{
    public enum CustomQueryInterfaceResult
    {
        Handled = 0,
        NotHandled = 1,
        Failed = 2
    }

    public interface ICustomQueryInterface
    {
        CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv);
    }
}