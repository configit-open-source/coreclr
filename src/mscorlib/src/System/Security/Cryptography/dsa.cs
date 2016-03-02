
using System.Security.Util;
using System.Text;

namespace System.Security.Cryptography
{
    public struct DSAParameters
    {
        public byte[] P;
        public byte[] Q;
        public byte[] G;
        public byte[] Y;
        public byte[] J;
        public byte[] X;
        public byte[] Seed;
        public int Counter;
    }

    public abstract class DSA : AsymmetricAlgorithm
    {
        protected DSA()
        {
        }

        new static public DSA Create()
        {
            return Create("System.Security.Cryptography.DSA");
        }

        new static public DSA Create(String algName)
        {
            return (DSA)CryptoConfig.CreateFromName(algName);
        }

        abstract public byte[] CreateSignature(byte[] rgbHash);
        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
        public override void FromXmlString(String xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException("xmlString");
                        DSAParameters dsaParams = new DSAParameters();
            Parser p = new Parser(xmlString);
            SecurityElement topElement = p.GetTopElement();
            String pString = topElement.SearchForTextOfLocalName("P");
            if (pString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "P"));
            }

            dsaParams.P = Convert.FromBase64String(Utils.DiscardWhiteSpaces(pString));
            String qString = topElement.SearchForTextOfLocalName("Q");
            if (qString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "Q"));
            }

            dsaParams.Q = Convert.FromBase64String(Utils.DiscardWhiteSpaces(qString));
            String gString = topElement.SearchForTextOfLocalName("G");
            if (gString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "G"));
            }

            dsaParams.G = Convert.FromBase64String(Utils.DiscardWhiteSpaces(gString));
            String yString = topElement.SearchForTextOfLocalName("Y");
            if (yString == null)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "Y"));
            }

            dsaParams.Y = Convert.FromBase64String(Utils.DiscardWhiteSpaces(yString));
            String jString = topElement.SearchForTextOfLocalName("J");
            if (jString != null)
                dsaParams.J = Convert.FromBase64String(Utils.DiscardWhiteSpaces(jString));
            String xString = topElement.SearchForTextOfLocalName("X");
            if (xString != null)
                dsaParams.X = Convert.FromBase64String(Utils.DiscardWhiteSpaces(xString));
            String seedString = topElement.SearchForTextOfLocalName("Seed");
            String pgenCounterString = topElement.SearchForTextOfLocalName("PgenCounter");
            if ((seedString != null) && (pgenCounterString != null))
            {
                dsaParams.Seed = Convert.FromBase64String(Utils.DiscardWhiteSpaces(seedString));
                dsaParams.Counter = Utils.ConvertByteArrayToInt(Convert.FromBase64String(Utils.DiscardWhiteSpaces(pgenCounterString)));
            }
            else if ((seedString != null) || (pgenCounterString != null))
            {
                if (seedString == null)
                {
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "Seed"));
                }
                else
                {
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString", "DSA", "PgenCounter"));
                }
            }

            ImportParameters(dsaParams);
        }

        public override String ToXmlString(bool includePrivateParameters)
        {
            DSAParameters dsaParams = this.ExportParameters(includePrivateParameters);
            StringBuilder sb = new StringBuilder();
            sb.Append("<DSAKeyValue>");
            sb.Append("<P>" + Convert.ToBase64String(dsaParams.P) + "</P>");
            sb.Append("<Q>" + Convert.ToBase64String(dsaParams.Q) + "</Q>");
            sb.Append("<G>" + Convert.ToBase64String(dsaParams.G) + "</G>");
            sb.Append("<Y>" + Convert.ToBase64String(dsaParams.Y) + "</Y>");
            if (dsaParams.J != null)
            {
                sb.Append("<J>" + Convert.ToBase64String(dsaParams.J) + "</J>");
            }

            if ((dsaParams.Seed != null))
            {
                sb.Append("<Seed>" + Convert.ToBase64String(dsaParams.Seed) + "</Seed>");
                sb.Append("<PgenCounter>" + Convert.ToBase64String(Utils.ConvertIntToByteArray(dsaParams.Counter)) + "</PgenCounter>");
            }

            if (includePrivateParameters)
            {
                sb.Append("<X>" + Convert.ToBase64String(dsaParams.X) + "</X>");
            }

            sb.Append("</DSAKeyValue>");
            return (sb.ToString());
        }

        abstract public DSAParameters ExportParameters(bool includePrivateParameters);
        abstract public void ImportParameters(DSAParameters parameters);
    }
}