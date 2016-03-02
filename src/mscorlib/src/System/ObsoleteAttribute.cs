namespace System
{
    public sealed class ObsoleteAttribute : Attribute
    {
        private String _message;
        private bool _error;
        public ObsoleteAttribute()
        {
            _message = null;
            _error = false;
        }

        public ObsoleteAttribute(String message)
        {
            _message = message;
            _error = false;
        }

        public ObsoleteAttribute(String message, bool error)
        {
            _message = message;
            _error = error;
        }

        public String Message
        {
            get
            {
                return _message;
            }
        }

        public bool IsError
        {
            get
            {
                return _error;
            }
        }
    }
}