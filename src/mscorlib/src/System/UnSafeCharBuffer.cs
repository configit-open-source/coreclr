namespace System
{
    using System.Security;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    unsafe internal struct UnSafeCharBuffer
    {
        char *m_buffer;
        int m_totalSize;
        int m_length;
        public UnSafeCharBuffer(char *buffer, int bufferSize)
        {
            Contract.Assert(buffer != null, "buffer pointer can't be null.");
            Contract.Assert(bufferSize >= 0, "buffer size can't be negative.");
            m_buffer = buffer;
            m_totalSize = bufferSize;
            m_length = 0;
        }

        public void AppendString(string stringToAppend)
        {
            if (String.IsNullOrEmpty(stringToAppend))
            {
                return;
            }

            if ((m_totalSize - m_length) < stringToAppend.Length)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (char *pointerToString = stringToAppend)
            {
                Buffer.Memcpy((byte *)(m_buffer + m_length), (byte *)pointerToString, stringToAppend.Length * sizeof (char));
            }

            m_length += stringToAppend.Length;
            Contract.Assert(m_length <= m_totalSize, "Buffer has been overflowed!");
        }

        public int Length
        {
            get
            {
                return m_length;
            }
        }
    }
}