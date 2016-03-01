using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    sealed internal class FrameworkEventSource : EventSource
    {
        public static readonly FrameworkEventSource Log = new FrameworkEventSource();
        public static class Keywords
        {
            public const EventKeywords Loader = (EventKeywords)0x0001;
            public const EventKeywords ThreadPool = (EventKeywords)0x0002;
            public const EventKeywords NetClient = (EventKeywords)0x0004;
            public const EventKeywords DynamicTypeUsage = (EventKeywords)0x0008;
            public const EventKeywords ThreadTransfer = (EventKeywords)0x0010;
        }

        public static class Tasks
        {
            public const EventTask GetResponse = (EventTask)1;
            public const EventTask GetRequestStream = (EventTask)2;
            public const EventTask ThreadTransfer = (EventTask)3;
        }

        public static class Opcodes
        {
            public const EventOpcode ReceiveHandled = (EventOpcode)11;
        }

        public static bool IsInitialized
        {
            get
            {
                return Log != null;
            }
        }

        private FrameworkEventSource(): base (new Guid(0x8e9f5090, 0x2d75, 0x4d03, 0x8a, 0x81, 0xe5, 0xaf, 0xbf, 0x85, 0xda, 0xf1), "System.Diagnostics.Eventing.FrameworkEventSource")
        {
        }

        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3, bool arg4)
        {
            if (IsEnabled())
            {
                if (arg3 == null)
                    arg3 = "";
                fixed (char *string3Bytes = arg3)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[4];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = 4;
                    WriteEventCore(eventId, 4, descrs);
                }
            }
        }

        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3)
        {
            if (IsEnabled())
            {
                if (arg3 == null)
                    arg3 = "";
                fixed (char *string3Bytes = arg3)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        private unsafe void WriteEvent(int eventId, long arg1, string arg2, bool arg3, bool arg4)
        {
            if (IsEnabled())
            {
                if (arg2 == null)
                    arg2 = "";
                fixed (char *string2Bytes = arg2)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[4];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 4;
                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = 4;
                    WriteEventCore(eventId, 4, descrs);
                }
            }
        }

        private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3)
        {
            if (IsEnabled())
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3, int arg4)
        {
            if (IsEnabled())
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[4];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                descrs[3].DataPointer = (IntPtr)(&arg4);
                descrs[3].Size = 4;
                WriteEventCore(eventId, 4, descrs);
            }
        }

        public void ResourceManagerLookupStarted(String baseName, String mainAssemblyName, String cultureName)
        {
            WriteEvent(1, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerLookingForResourceSet(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(2, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerFoundResourceSetInCache(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(3, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerFoundResourceSetInCacheUnexpected(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(4, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerStreamFound(String baseName, String mainAssemblyName, String cultureName, String loadedAssemblyName, String resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(5, baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName);
        }

        public void ResourceManagerStreamNotFound(String baseName, String mainAssemblyName, String cultureName, String loadedAssemblyName, String resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(6, baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName);
        }

        public void ResourceManagerGetSatelliteAssemblySucceeded(String baseName, String mainAssemblyName, String cultureName, String assemblyName)
        {
            if (IsEnabled())
                WriteEvent(7, baseName, mainAssemblyName, cultureName, assemblyName);
        }

        public void ResourceManagerGetSatelliteAssemblyFailed(String baseName, String mainAssemblyName, String cultureName, String assemblyName)
        {
            if (IsEnabled())
                WriteEvent(8, baseName, mainAssemblyName, cultureName, assemblyName);
        }

        public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(String baseName, String mainAssemblyName, String assemblyName, String resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(9, baseName, mainAssemblyName, assemblyName, resourceFileName);
        }

        public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(String baseName, String mainAssemblyName, String assemblyName, String resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(10, baseName, mainAssemblyName, assemblyName, resourceFileName);
        }

        public void ResourceManagerManifestResourceAccessDenied(String baseName, String mainAssemblyName, String assemblyName, String canonicalName)
        {
            if (IsEnabled())
                WriteEvent(11, baseName, mainAssemblyName, assemblyName, canonicalName);
        }

        public void ResourceManagerNeutralResourcesSufficient(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(12, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerNeutralResourceAttributeMissing(String mainAssemblyName)
        {
            if (IsEnabled())
                WriteEvent(13, mainAssemblyName);
        }

        public void ResourceManagerCreatingResourceSet(String baseName, String mainAssemblyName, String cultureName, String fileName)
        {
            if (IsEnabled())
                WriteEvent(14, baseName, mainAssemblyName, cultureName, fileName);
        }

        public void ResourceManagerNotCreatingResourceSet(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(15, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerLookupFailed(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(16, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerReleasingResources(String baseName, String mainAssemblyName)
        {
            if (IsEnabled())
                WriteEvent(17, baseName, mainAssemblyName);
        }

        public void ResourceManagerNeutralResourcesNotFound(String baseName, String mainAssemblyName, String resName)
        {
            if (IsEnabled())
                WriteEvent(18, baseName, mainAssemblyName, resName);
        }

        public void ResourceManagerNeutralResourcesFound(String baseName, String mainAssemblyName, String resName)
        {
            if (IsEnabled())
                WriteEvent(19, baseName, mainAssemblyName, resName);
        }

        public void ResourceManagerAddingCultureFromConfigFile(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(20, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerCultureNotFoundInConfigFile(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(21, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerCultureFoundInConfigFile(String baseName, String mainAssemblyName, String cultureName)
        {
            if (IsEnabled())
                WriteEvent(22, baseName, mainAssemblyName, cultureName);
        }

        public void ResourceManagerLookupStarted(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookupStarted(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerLookingForResourceSet(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookingForResourceSet(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerFoundResourceSetInCache(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerFoundResourceSetInCache(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerFoundResourceSetInCacheUnexpected(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerFoundResourceSetInCacheUnexpected(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerStreamFound(String baseName, Assembly mainAssembly, String cultureName, Assembly loadedAssembly, String resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerStreamFound(baseName, GetName(mainAssembly), cultureName, GetName(loadedAssembly), resourceFileName);
        }

        public void ResourceManagerStreamNotFound(String baseName, Assembly mainAssembly, String cultureName, Assembly loadedAssembly, String resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerStreamNotFound(baseName, GetName(mainAssembly), cultureName, GetName(loadedAssembly), resourceFileName);
        }

        public void ResourceManagerGetSatelliteAssemblySucceeded(String baseName, Assembly mainAssembly, String cultureName, String assemblyName)
        {
            if (IsEnabled())
                ResourceManagerGetSatelliteAssemblySucceeded(baseName, GetName(mainAssembly), cultureName, assemblyName);
        }

        public void ResourceManagerGetSatelliteAssemblyFailed(String baseName, Assembly mainAssembly, String cultureName, String assemblyName)
        {
            if (IsEnabled())
                ResourceManagerGetSatelliteAssemblyFailed(baseName, GetName(mainAssembly), cultureName, assemblyName);
        }

        public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(String baseName, Assembly mainAssembly, String assemblyName, String resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(baseName, GetName(mainAssembly), assemblyName, resourceFileName);
        }

        public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(String baseName, Assembly mainAssembly, String assemblyName, String resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerCaseInsensitiveResourceStreamLookupFailed(baseName, GetName(mainAssembly), assemblyName, resourceFileName);
        }

        public void ResourceManagerManifestResourceAccessDenied(String baseName, Assembly mainAssembly, String assemblyName, String canonicalName)
        {
            if (IsEnabled())
                ResourceManagerManifestResourceAccessDenied(baseName, GetName(mainAssembly), assemblyName, canonicalName);
        }

        public void ResourceManagerNeutralResourcesSufficient(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesSufficient(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerNeutralResourceAttributeMissing(Assembly mainAssembly)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourceAttributeMissing(GetName(mainAssembly));
        }

        public void ResourceManagerCreatingResourceSet(String baseName, Assembly mainAssembly, String cultureName, String fileName)
        {
            if (IsEnabled())
                ResourceManagerCreatingResourceSet(baseName, GetName(mainAssembly), cultureName, fileName);
        }

        public void ResourceManagerNotCreatingResourceSet(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerNotCreatingResourceSet(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerLookupFailed(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookupFailed(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerReleasingResources(String baseName, Assembly mainAssembly)
        {
            if (IsEnabled())
                ResourceManagerReleasingResources(baseName, GetName(mainAssembly));
        }

        public void ResourceManagerNeutralResourcesNotFound(String baseName, Assembly mainAssembly, String resName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesNotFound(baseName, GetName(mainAssembly), resName);
        }

        public void ResourceManagerNeutralResourcesFound(String baseName, Assembly mainAssembly, String resName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesFound(baseName, GetName(mainAssembly), resName);
        }

        public void ResourceManagerAddingCultureFromConfigFile(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerAddingCultureFromConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerCultureNotFoundInConfigFile(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerCultureNotFoundInConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        public void ResourceManagerCultureFoundInConfigFile(String baseName, Assembly mainAssembly, String cultureName)
        {
            if (IsEnabled())
                ResourceManagerCultureFoundInConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        private static string GetName(Assembly assembly)
        {
            if (assembly == null)
                return "<<NULL>>";
            else
                return assembly.FullName;
        }

        public void ThreadPoolEnqueueWork(long workID)
        {
            WriteEvent(30, workID);
        }

        public unsafe void ThreadPoolEnqueueWorkObject(object workID)
        {
            ThreadPoolEnqueueWork((long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref workID)));
        }

        public void ThreadPoolDequeueWork(long workID)
        {
            WriteEvent(31, workID);
        }

        public unsafe void ThreadPoolDequeueWorkObject(object workID)
        {
            ThreadPoolDequeueWork((long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref workID)));
        }

        private void GetResponseStart(long id, string uri, bool success, bool synchronous)
        {
            WriteEvent(140, id, uri, success, synchronous);
        }

        private void GetResponseStop(long id, bool success, bool synchronous, int statusCode)
        {
            WriteEvent(141, id, success, synchronous, statusCode);
        }

        private void GetRequestStreamStart(long id, string uri, bool success, bool synchronous)
        {
            WriteEvent(142, id, uri, success, synchronous);
        }

        private void GetRequestStreamStop(long id, bool success, bool synchronous)
        {
            WriteEvent(143, id, success, synchronous);
        }

        public unsafe void BeginGetResponse(object id, string uri, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetResponseStart(IdForObject(id), uri, success, synchronous);
        }

        public unsafe void EndGetResponse(object id, bool success, bool synchronous, int statusCode)
        {
            if (IsEnabled())
                GetResponseStop(IdForObject(id), success, synchronous, statusCode);
        }

        public unsafe void BeginGetRequestStream(object id, string uri, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetRequestStreamStart(IdForObject(id), uri, success, synchronous);
        }

        public unsafe void EndGetRequestStream(object id, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetRequestStreamStop(IdForObject(id), success, synchronous);
        }

        public void ThreadTransferSend(long id, int kind, string info, bool multiDequeues)
        {
            if (IsEnabled())
                WriteEvent(150, id, kind, info, multiDequeues);
        }

        public unsafe void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues)
        {
            ThreadTransferSend((long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref id)), kind, info, multiDequeues);
        }

        public void ThreadTransferReceive(long id, int kind, string info)
        {
            if (IsEnabled())
                WriteEvent(151, id, kind, info);
        }

        public unsafe void ThreadTransferReceiveObj(object id, int kind, string info)
        {
            ThreadTransferReceive((long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref id)), kind, info);
        }

        public void ThreadTransferReceiveHandled(long id, int kind, string info)
        {
            if (IsEnabled())
                WriteEvent(152, id, kind, info);
        }

        public unsafe void ThreadTransferReceiveHandledObj(object id, int kind, string info)
        {
            ThreadTransferReceive((long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref id)), kind, info);
        }

        private static long IdForObject(object obj)
        {
            return obj.GetHashCode() + 0x7FFFFFFF00000000;
        }
    }
}