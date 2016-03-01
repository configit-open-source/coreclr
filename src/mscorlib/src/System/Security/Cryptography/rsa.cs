using System.Diagnostics.Contracts;
using System.Security.Util;
using System.Text;

namespace System.Security.Cryptography
{
    public struct RSAParameters
    {
        public byte[] Exponent;
        public byte[] Modulus;
        public byte[] P;
        public byte[] Q;
        public byte[] DP;
        public byte[] DQ;
        public byte[] InverseQ;
        public byte[] D;
    }

    public abstract class RSA : AsymmetricAlgorithm
    {
        protected RSA()
        {
        }

        new static public RSA Create()
        {
            return Create("System.Security.Cryptography.RSA");
        }

        new static public RSA Create(String algName)
        {
            return (RSA)CryptoConfig.CreateFromName(algName);
        }

        public virtual byte[] DecryptValue(byte[] rgb)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        public virtual byte[] EncryptValue(byte[] rgb)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        public override string KeyExchangeAlgorithm
        {
            get
            {
                return "RSA";
            }
        }

        public override string SignatureAlgorithm
        {
            get
            {
                return "RSA";
            }
        }

        public override void FromXmlString(String xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException("xmlString");
            Contract.EndContractBlock();
            RSAParameters rsaParams = new RSAParameters();
            Parser p = new Parser(xmlString);
            SecurityElement topElement = p.GetTopElement();
            String modulusString = topElement.SearchForTextOfLocalName("Modulus");
            if (modulusString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "RSA", "Modulus"));
            }

            rsaParams.Modulus = Convert.FromBase64String(Utils.DiscardWhiteSpaces(modulusString));
            String exponentString = topElement.SearchForTextOfLocalName("Exponent");
            if (exponentString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "RSA", "Exponent"));
            }

            rsaParams.Exponent = Convert.FromBase64String(Utils.DiscardWhiteSpaces(exponentString));
            String pString = topElement.SearchForTextOfLocalName("P");
            if (pString != null)
                rsaParams.P = Convert.FromBase64String(Utils.DiscardWhiteSpaces(pString));
            String qString = topElement.SearchForTextOfLocalName("Q");
            if (qString != null)
                rsaParams.Q = Convert.FromBase64String(Utils.DiscardWhiteSpaces(qString));
            String dpString = topElement.SearchForTextOfLocalName("DP");
            if (dpString != null)
                rsaParams.DP = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dpString));
            String dqString = topElement.SearchForTextOfLocalName("DQ");
            if (dqString != null)
                rsaParams.DQ = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dqString));
            String inverseQString = topElement.SearchForTextOfLocalName("InverseQ");
            if (inverseQString != null)
                rsaParams.InverseQ = Convert.FromBase64String(Utils.DiscardWhiteSpaces(inverseQString));
            String dString = topElement.SearchForTextOfLocalName("D");
            if (dString != null)
                rsaParams.D = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dString));
            ImportParameters(rsaParams);
        }

        public override String ToXmlString(bool includePrivateParameters)
        {
            RSAParameters rsaParams = this.ExportParameters(includePrivateParameters);
            StringBuilder sb = new StringBuilder();
            sb.Append("<RSAKeyValue>");
            sb.Append("<Modulus>" + Convert.ToBase64String(rsaParams.Modulus) + "</Modulus>");
            sb.Append("<Exponent>" + Convert.ToBase64String(rsaParams.Exponent) + "</Exponent>");
            if (includePrivateParameters)
            {
                sb.Append("<P>" + Convert.ToBase64String(rsaParams.P) + "</P>");
                sb.Append("<Q>" + Convert.ToBase64String(rsaParams.Q) + "</Q>");
                sb.Append("<DP>" + Convert.ToBase64String(rsaParams.DP) + "</DP>");
                sb.Append("<DQ>" + Convert.ToBase64String(rsaParams.DQ) + "</DQ>");
                sb.Append("<InverseQ>" + Convert.ToBase64String(rsaParams.InverseQ) + "</InverseQ>");
                sb.Append("<D>" + Convert.ToBase64String(rsaParams.D) + "</D>");
            }

            sb.Append("</RSAKeyValue>");
            return (sb.ToString());
        }

        abstract public RSAParameters ExportParameters(bool includePrivateParameters);
        abstract public void ImportParameters(RSAParameters parameters);
    }
}