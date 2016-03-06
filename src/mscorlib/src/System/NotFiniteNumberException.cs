
using System.Runtime.Serialization;

namespace System
{
    public class NotFiniteNumberException : ArithmeticException
    {
        private double _offendingNumber;
        public NotFiniteNumberException(): base (Environment.GetResourceString("Arg_NotFiniteNumberException"))
        {
            _offendingNumber = 0;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(double offendingNumber): base ()
        {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message): base (message)
        {
            _offendingNumber = 0;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message, double offendingNumber): base (message)
        {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message, double offendingNumber, Exception innerException): base (message, innerException)
        {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public double OffendingNumber
        {
            get
            {
                return _offendingNumber;
            }
        }

    }
}