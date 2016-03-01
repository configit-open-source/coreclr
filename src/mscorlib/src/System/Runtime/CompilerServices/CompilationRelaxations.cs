namespace System.Runtime.CompilerServices
{
    using System;

    [Flags]
    public enum CompilationRelaxations : int
    {
        NoStringInterning = 0x0008
    }

    ;
    public class CompilationRelaxationsAttribute : Attribute
    {
        private int m_relaxations;
        public CompilationRelaxationsAttribute(int relaxations)
        {
            m_relaxations = relaxations;
        }

        public CompilationRelaxationsAttribute(CompilationRelaxations relaxations)
        {
            m_relaxations = (int)relaxations;
        }

        public int CompilationRelaxations
        {
            get
            {
                return m_relaxations;
            }
        }
    }
}