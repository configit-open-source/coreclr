namespace System.Diagnostics.Tracing
{
    [Flags]
    public enum EventFieldTags
    {
        None = 0
    }

    public class EventFieldAttribute : Attribute
    {
        public EventFieldTags Tags
        {
            get;
            set;
        }

        internal string Name
        {
            get;
            set;
        }

        public EventFieldFormat Format
        {
            get;
            set;
        }
    }
}