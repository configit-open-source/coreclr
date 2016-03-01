namespace System.Security.Cryptography
{
    public abstract class MaskGenerationMethod
    {
        abstract public byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
    }
}