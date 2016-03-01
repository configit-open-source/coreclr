namespace System.Resources
{
    using System;
    using System.IO;

    public interface IResourceWriter : IDisposable
    {
        void AddResource(String name, String value);
        void AddResource(String name, Object value);
        void AddResource(String name, byte[] value);
        void Close();
        void Generate();
    }
}