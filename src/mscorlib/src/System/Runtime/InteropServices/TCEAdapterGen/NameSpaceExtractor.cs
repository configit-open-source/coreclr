namespace System.Runtime.InteropServices.TCEAdapterGen
{
    internal static class NameSpaceExtractor
    {
        private static char NameSpaceSeperator = '.';
        public static String ExtractNameSpace(String FullyQualifiedTypeName)
        {
            int TypeNameStartPos = FullyQualifiedTypeName.LastIndexOf(NameSpaceSeperator);
            if (TypeNameStartPos == -1)
                return "";
            else
                return FullyQualifiedTypeName.Substring(0, TypeNameStartPos);
        }
    }
}