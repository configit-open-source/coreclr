namespace System.Runtime.CompilerServices
{
    public static class FormattableStringFactory
    {
        public static FormattableString Create(string format, params object[] arguments)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            return new ConcreteFormattableString(format, arguments);
        }

        private sealed class ConcreteFormattableString : FormattableString
        {
            private readonly string _format;
            private readonly object[] _arguments;
            internal ConcreteFormattableString(string format, object[] arguments)
            {
                _format = format;
                _arguments = arguments;
            }

            public override string Format
            {
                get
                {
                    return _format;
                }
            }

            public override object[] GetArguments()
            {
                return _arguments;
            }

            public override int ArgumentCount
            {
                get
                {
                    return _arguments.Length;
                }
            }

            public override object GetArgument(int index)
            {
                return _arguments[index];
            }

            public override string ToString(IFormatProvider formatProvider)
            {
                return string.Format(formatProvider, _format, _arguments);
            }
        }
    }
}