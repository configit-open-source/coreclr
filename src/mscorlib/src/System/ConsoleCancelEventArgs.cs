namespace System
{
    public delegate void ConsoleCancelEventHandler(Object sender, ConsoleCancelEventArgs e);
    public sealed class ConsoleCancelEventArgs : EventArgs
    {
        private ConsoleSpecialKey _type;
        private bool _cancel;
        internal ConsoleCancelEventArgs(ConsoleSpecialKey type)
        {
            _type = type;
            _cancel = false;
        }

        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value;
            }
        }

        public ConsoleSpecialKey SpecialKey
        {
            get
            {
                return _type;
            }
        }
    }
}