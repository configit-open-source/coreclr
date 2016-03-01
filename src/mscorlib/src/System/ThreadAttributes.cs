namespace System
{
    public sealed class STAThreadAttribute : Attribute
    {
        public STAThreadAttribute()
        {
        }
    }

    public sealed class MTAThreadAttribute : Attribute
    {
        public MTAThreadAttribute()
        {
        }
    }
}