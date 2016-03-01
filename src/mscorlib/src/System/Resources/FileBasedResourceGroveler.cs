using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Threading;

namespace System.Resources
{
    internal class FileBasedResourceGroveler : IResourceGroveler
    {
        private ResourceManager.ResourceManagerMediator _mediator;
        public FileBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            Contract.Assert(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark)
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");
            String fileName = null;
            ResourceSet rs = null;
            try
            {
                new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted).Assert();
                String tempFileName = _mediator.GetResourceFileName(culture);
                fileName = FindResourceFile(culture, tempFileName);
                if (fileName == null)
                {
                    if (tryParents)
                    {
                        if (culture.HasInvariantCultureName)
                        {
                            throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoNeutralDisk") + Environment.NewLine + "baseName: " + _mediator.BaseNameField + "  locationInfo: " + (_mediator.LocationInfo == null ? "<null>" : _mediator.LocationInfo.FullName) + "  fileName: " + _mediator.GetResourceFileName(culture));
                        }
                    }
                }
                else
                {
                    rs = CreateResourceSet(fileName);
                }

                return rs;
            }
            finally
            {
                System.Security.CodeAccessPermission.RevertAssert();
            }
        }

        private String FindResourceFile(CultureInfo culture, String fileName)
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");
            Contract.Assert(fileName != null, "fileName shouldn't be null; check caller");
            if (_mediator.ModuleDir != null)
            {
                if (ResourceManager.DEBUG >= 3)
                    BCLDebug.Log("FindResourceFile: checking module dir: \"" + _mediator.ModuleDir + '\"');
                String path = Path.Combine(_mediator.ModuleDir, fileName);
                if (File.Exists(path))
                {
                    if (ResourceManager.DEBUG >= 3)
                        BCLDebug.Log("Found resource file in module dir!  " + path);
                    return path;
                }
            }

            if (ResourceManager.DEBUG >= 3)
                BCLDebug.Log("Couldn't find resource file in module dir, checking .\\" + fileName);
            if (File.Exists(fileName))
                return fileName;
            return null;
        }

        private ResourceSet CreateResourceSet(String file)
        {
            Contract.Assert(file != null, "file shouldn't be null; check caller");
            if (_mediator.UserResourceSet == null)
            {
                return new RuntimeResourceSet(file);
            }
            else
            {
                Object[] args = new Object[1];
                args[0] = file;
                try
                {
                    return (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResMgrBadResSet_Type", _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }
    }
}