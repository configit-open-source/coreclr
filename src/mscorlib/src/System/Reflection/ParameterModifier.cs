

namespace System.Reflection
{
    public struct ParameterModifier
    {
        private bool[] _byRef;
        public ParameterModifier(int parameterCount)
        {
            if (parameterCount <= 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_ParmArraySize"));
                        _byRef = new bool[parameterCount];
        }

        internal bool[] IsByRefArray
        {
            get
            {
                return _byRef;
            }
        }

        public bool this[int index]
        {
            get
            {
                return _byRef[index];
            }

            set
            {
                _byRef[index] = value;
            }
        }
    }
}