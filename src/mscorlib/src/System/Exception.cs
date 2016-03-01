using System.Collections;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace System
{
    public class Exception : ISerializable, _Exception
    {
        private void Init()
        {
            _message = null;
            _stackTrace = null;
            _dynamicMethods = null;
            HResult = __HResults.COR_E_EXCEPTION;
            _xcode = _COMPlusExceptionCode;
            _xptrs = (IntPtr)0;
            _watsonBuckets = null;
            _ipForWatsonBuckets = UIntPtr.Zero;
        }

        public Exception()
        {
            Init();
        }

        public Exception(String message)
        {
            Init();
            _message = message;
        }

        public Exception(String message, Exception innerException)
        {
            Init();
            _message = message;
            _innerException = innerException;
        }

        protected Exception(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            _className = info.GetString("ClassName");
            _message = info.GetString("Message");
            _data = (IDictionary)(info.GetValueNoThrow("Data", typeof (IDictionary)));
            _innerException = (Exception)(info.GetValue("InnerException", typeof (Exception)));
            _helpURL = info.GetString("HelpURL");
            _stackTraceString = info.GetString("StackTraceString");
            _remoteStackTraceString = info.GetString("RemoteStackTraceString");
            _remoteStackIndex = info.GetInt32("RemoteStackIndex");
            _exceptionMethodString = (String)(info.GetValue("ExceptionMethod", typeof (String)));
            HResult = info.GetInt32("HResult");
            _source = info.GetString("Source");
            _watsonBuckets = (Object)info.GetValueNoThrow("WatsonBuckets", typeof (byte[]));
            if (_className == null || HResult == 0)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                _remoteStackTraceString = _remoteStackTraceString + _stackTraceString;
                _stackTraceString = null;
            }
        }

        public virtual String Message
        {
            get
            {
                if (_message == null)
                {
                    if (_className == null)
                    {
                        _className = GetClassName();
                    }

                    return Environment.GetResourceString("Exception_WasThrown", _className);
                }
                else
                {
                    return _message;
                }
            }
        }

        public virtual IDictionary Data
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_data == null)
                    if (IsImmutableAgileException(this))
                        _data = new EmptyReadOnlyDictionaryInternal();
                    else
                        _data = new ListDictionaryInternal();
                return _data;
            }
        }

        private static extern bool IsImmutableAgileException(Exception e);
        internal class __RestrictedErrorObject
        {
            private object _realErrorObject;
            internal __RestrictedErrorObject(object errorObject)
            {
                _realErrorObject = errorObject;
            }

            public object RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }

        internal void AddExceptionDataForRestrictedErrorInfo(string restrictedError, string restrictedErrorReference, string restrictedCapabilitySid, object restrictedErrorObject, bool hasrestrictedLanguageErrorObject = false)
        {
            IDictionary dict = Data;
            if (dict != null)
            {
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);
                dict.Add("__RestrictedErrorObject", (restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject)));
                dict.Add("__HasRestrictedLanguageErrorObject", hasrestrictedLanguageErrorObject);
            }
        }

        internal bool TryGetRestrictedLanguageErrorObject(out object restrictedErrorObject)
        {
            restrictedErrorObject = null;
            if (Data != null && Data.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (Data.Contains("__RestrictedErrorObject"))
                {
                    __RestrictedErrorObject restrictedObject = Data["__RestrictedErrorObject"] as __RestrictedErrorObject;
                    if (restrictedObject != null)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }

                return (bool)Data["__HasRestrictedLanguageErrorObject"];
            }

            return false;
        }

        private string GetClassName()
        {
            if (_className == null)
                _className = GetType().ToString();
            return _className;
        }

        public virtual Exception GetBaseException()
        {
            Exception inner = InnerException;
            Exception back = this;
            while (inner != null)
            {
                back = inner;
                inner = inner.InnerException;
            }

            return back;
        }

        public Exception InnerException
        {
            get
            {
                return _innerException;
            }
        }

        static extern private IRuntimeMethodInfo GetMethodFromStackTrace(Object stackTrace);
        private MethodBase GetExceptionMethodFromStackTrace()
        {
            IRuntimeMethodInfo method = GetMethodFromStackTrace(_stackTrace);
            if (method == null)
                return null;
            return RuntimeType.GetMethodBase(method);
        }

        public MethodBase TargetSite
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetTargetSiteInternal();
            }
        }

        private MethodBase GetTargetSiteInternal()
        {
            if (_exceptionMethod != null)
            {
                return _exceptionMethod;
            }

            if (_stackTrace == null)
            {
                return null;
            }

            if (_exceptionMethodString != null)
            {
                _exceptionMethod = GetExceptionMethodFromString();
            }
            else
            {
                _exceptionMethod = GetExceptionMethodFromStackTrace();
            }

            return _exceptionMethod;
        }

        public virtual String StackTrace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetStackTrace(true);
            }
        }

        private string GetStackTrace(bool needFileInfo)
        {
            string stackTraceString = _stackTraceString;
            string remoteStackTraceString = _remoteStackTraceString;
            if (stackTraceString != null)
            {
                return remoteStackTraceString + stackTraceString;
            }

            if (_stackTrace == null)
            {
                return remoteStackTraceString;
            }

            String tempStackTraceString = Environment.GetStackTrace(this, needFileInfo);
            return remoteStackTraceString + tempStackTraceString;
        }

        internal void SetErrorCode(int hr)
        {
            HResult = hr;
        }

        public virtual String HelpLink
        {
            get
            {
                return _helpURL;
            }

            set
            {
                _helpURL = value;
            }
        }

        public virtual String Source
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_source == null)
                {
                    StackTrace st = new StackTrace(this, true);
                    if (st.FrameCount > 0)
                    {
                        StackFrame sf = st.GetFrame(0);
                        MethodBase method = sf.GetMethod();
                        Module module = method.Module;
                        RuntimeModule rtModule = module as RuntimeModule;
                        if (rtModule == null)
                        {
                            System.Reflection.Emit.ModuleBuilder moduleBuilder = module as System.Reflection.Emit.ModuleBuilder;
                            if (moduleBuilder != null)
                                rtModule = moduleBuilder.InternalModule;
                            else
                                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
                        }

                        _source = rtModule.GetRuntimeAssembly().GetSimpleName();
                    }
                }

                return _source;
            }

            [System.Security.SecurityCritical]
            set
            {
                _source = value;
            }
        }

        public override String ToString()
        {
            return ToString(true, true);
        }

        private String ToString(bool needFileLineInfo, bool needMessage)
        {
            String message = (needMessage ? Message : null);
            String s;
            if (message == null || message.Length <= 0)
            {
                s = GetClassName();
            }
            else
            {
                s = GetClassName() + ": " + message;
            }

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString(needFileLineInfo, needMessage) + Environment.NewLine + "   " + Environment.GetResourceString("Exception_EndOfInnerExceptionStack");
            }

            string stackTrace = GetStackTrace(needFileLineInfo);
            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }

        private String GetExceptionMethodString()
        {
            MethodBase methBase = GetTargetSiteInternal();
            if (methBase == null)
            {
                return null;
            }

            if (methBase is System.Reflection.Emit.DynamicMethod.RTDynamicMethod)
            {
                return null;
            }

            char separator = '\n';
            StringBuilder result = new StringBuilder();
            if (methBase is ConstructorInfo)
            {
                RuntimeConstructorInfo rci = (RuntimeConstructorInfo)methBase;
                Type t = rci.ReflectedType;
                result.Append((int)MemberTypes.Constructor);
                result.Append(separator);
                result.Append(rci.Name);
                if (t != null)
                {
                    result.Append(separator);
                    result.Append(t.Assembly.FullName);
                    result.Append(separator);
                    result.Append(t.FullName);
                }

                result.Append(separator);
                result.Append(rci.ToString());
            }
            else
            {
                Contract.Assert(methBase is MethodInfo, "[Exception.GetExceptionMethodString]methBase is MethodInfo");
                RuntimeMethodInfo rmi = (RuntimeMethodInfo)methBase;
                Type t = rmi.DeclaringType;
                result.Append((int)MemberTypes.Method);
                result.Append(separator);
                result.Append(rmi.Name);
                result.Append(separator);
                result.Append(rmi.Module.Assembly.FullName);
                result.Append(separator);
                if (t != null)
                {
                    result.Append(t.FullName);
                    result.Append(separator);
                }

                result.Append(rmi.ToString());
            }

            return result.ToString();
        }

        private MethodBase GetExceptionMethodFromString()
        {
            Contract.Assert(_exceptionMethodString != null, "Method string cannot be NULL!");
            String[] args = _exceptionMethodString.Split(new char[]{'\0', '\n'});
            if (args.Length != 5)
            {
                throw new SerializationException();
            }

            SerializationInfo si = new SerializationInfo(typeof (MemberInfoSerializationHolder), new FormatterConverter());
            si.AddValue("MemberType", (int)Int32.Parse(args[0], CultureInfo.InvariantCulture), typeof (Int32));
            si.AddValue("Name", args[1], typeof (String));
            si.AddValue("AssemblyName", args[2], typeof (String));
            si.AddValue("ClassName", args[3]);
            si.AddValue("Signature", args[4]);
            MethodBase result;
            StreamingContext sc = new StreamingContext(StreamingContextStates.All);
            try
            {
                result = (MethodBase)new MemberInfoSerializationHolder(si, sc).GetRealObject(sc);
            }
            catch (SerializationException)
            {
                result = null;
            }

            return result;
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();
            String tempStackTraceString = _stackTraceString;
            if (_stackTrace != null)
            {
                if (tempStackTraceString == null)
                {
                    tempStackTraceString = Environment.GetStackTrace(this, true);
                }

                if (_exceptionMethod == null)
                {
                    _exceptionMethod = GetExceptionMethodFromStackTrace();
                }
            }

            if (_source == null)
            {
                _source = Source;
            }

            info.AddValue("ClassName", GetClassName(), typeof (String));
            info.AddValue("Message", _message, typeof (String));
            info.AddValue("Data", _data, typeof (IDictionary));
            info.AddValue("InnerException", _innerException, typeof (Exception));
            info.AddValue("HelpURL", _helpURL, typeof (String));
            info.AddValue("StackTraceString", tempStackTraceString, typeof (String));
            info.AddValue("RemoteStackTraceString", _remoteStackTraceString, typeof (String));
            info.AddValue("RemoteStackIndex", _remoteStackIndex, typeof (Int32));
            info.AddValue("ExceptionMethod", GetExceptionMethodString(), typeof (String));
            info.AddValue("HResult", HResult);
            info.AddValue("Source", _source, typeof (String));
            info.AddValue("WatsonBuckets", _watsonBuckets, typeof (byte[]));
        }

        internal Exception PrepForRemoting()
        {
            String tmp = null;
            if (_remoteStackIndex == 0)
            {
                tmp = Environment.NewLine + "Server stack trace: " + Environment.NewLine + StackTrace + Environment.NewLine + Environment.NewLine + "Exception rethrown at [" + _remoteStackIndex + "]: " + Environment.NewLine;
            }
            else
            {
                tmp = StackTrace + Environment.NewLine + Environment.NewLine + "Exception rethrown at [" + _remoteStackIndex + "]: " + Environment.NewLine;
            }

            _remoteStackTraceString = tmp;
            _remoteStackIndex++;
            return this;
        }

        private void OnDeserialized(StreamingContext context)
        {
            _stackTrace = null;
            _ipForWatsonBuckets = UIntPtr.Zero;
        }

        internal void InternalPreserveStackTrace()
        {
            string tmpStackTraceString;
            if (AppDomain.IsAppXModel())
            {
                tmpStackTraceString = GetStackTrace(true);
                string source = Source;
            }
            else
            {
                tmpStackTraceString = StackTrace;
            }

            if (tmpStackTraceString != null && tmpStackTraceString.Length > 0)
            {
                _remoteStackTraceString = tmpStackTraceString + Environment.NewLine;
            }

            _stackTrace = null;
            _stackTraceString = null;
        }

        private static object s_EDILock = new object ();
        internal UIntPtr IPForWatsonBuckets
        {
            get
            {
                return _ipForWatsonBuckets;
            }
        }

        internal object WatsonBuckets
        {
            get
            {
                return _watsonBuckets;
            }
        }

        internal string RemoteStackTrace
        {
            get
            {
                return _remoteStackTraceString;
            }
        }

        private static extern void PrepareForForeignExceptionRaise();
        private static extern void GetStackTracesDeepCopy(Exception exception, out object currentStackTrace, out object dynamicMethodArray);
        internal static extern void SaveStackTracesFromDeepCopy(Exception exception, object currentStackTrace, object dynamicMethodArray);
        private static extern object CopyStackTrace(object currentStackTrace);
        private static extern object CopyDynamicMethods(object currentDynamicMethods);
        internal object DeepCopyStackTrace(object currentStackTrace)
        {
            if (currentStackTrace != null)
            {
                return CopyStackTrace(currentStackTrace);
            }
            else
            {
                return null;
            }
        }

        internal object DeepCopyDynamicMethods(object currentDynamicMethods)
        {
            if (currentDynamicMethods != null)
            {
                return CopyDynamicMethods(currentDynamicMethods);
            }
            else
            {
                return null;
            }
        }

        internal void GetStackTracesDeepCopy(out object currentStackTrace, out object dynamicMethodArray)
        {
            GetStackTracesDeepCopy(this, out currentStackTrace, out dynamicMethodArray);
        }

        internal void RestoreExceptionDispatchInfo(System.Runtime.ExceptionServices.ExceptionDispatchInfo exceptionDispatchInfo)
        {
            bool fCanProcessException = !(IsImmutableAgileException(this));
            if (fCanProcessException)
            {
                try
                {
                }
                finally
                {
                    object _stackTraceCopy = (exceptionDispatchInfo.BinaryStackTraceArray == null) ? null : DeepCopyStackTrace(exceptionDispatchInfo.BinaryStackTraceArray);
                    object _dynamicMethodsCopy = (exceptionDispatchInfo.DynamicMethodArray == null) ? null : DeepCopyDynamicMethods(exceptionDispatchInfo.DynamicMethodArray);
                    lock (Exception.s_EDILock)
                    {
                        _watsonBuckets = exceptionDispatchInfo.WatsonBuckets;
                        _ipForWatsonBuckets = exceptionDispatchInfo.IPForWatsonBuckets;
                        _remoteStackTraceString = exceptionDispatchInfo.RemoteStackTrace;
                        SaveStackTracesFromDeepCopy(this, _stackTraceCopy, _dynamicMethodsCopy);
                    }

                    _stackTraceString = null;
                    Exception.PrepareForForeignExceptionRaise();
                }
            }
        }

        private String _className;
        private MethodBase _exceptionMethod;
        private String _exceptionMethodString;
        internal String _message;
        private IDictionary _data;
        private Exception _innerException;
        private String _helpURL;
        private Object _stackTrace;
        private Object _watsonBuckets;
        private String _stackTraceString;
        private String _remoteStackTraceString;
        private int _remoteStackIndex;
        private Object _dynamicMethods;
        internal int _HResult;
        public int HResult
        {
            get
            {
                return _HResult;
            }

            protected set
            {
                _HResult = value;
            }
        }

        private String _source;
        private IntPtr _xptrs;
        private int _xcode;
        private UIntPtr _ipForWatsonBuckets;
        private const int _COMPlusExceptionCode = unchecked ((int)0xe0434352);
        internal virtual String InternalToString()
        {
            try
            {
                SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy);
                sp.Assert();
            }
            catch
            {
            }

            bool fGetFileLineInfo = true;
            fGetFileLineInfo = false;
            return ToString(fGetFileLineInfo, true);
        }

        internal bool IsTransient
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return nIsTransient(_HResult);
            }
        }

        private extern static bool nIsTransient(int hr);
        internal enum ExceptionMessageKind
        {
            ThreadAbort = 1,
            ThreadInterrupted = 2,
            OutOfMemory = 3
        }

        internal static String GetMessageFromNativeResources(ExceptionMessageKind kind)
        {
            string retMesg = null;
            GetMessageFromNativeResources(kind, JitHelpers.GetStringHandleOnStack(ref retMesg));
            return retMesg;
        }

        private static extern void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);
    }

    internal sealed class CrossAppDomainMarshaledException : SystemException
    {
        public CrossAppDomainMarshaledException(String message, int errorCode): base (message)
        {
            SetErrorCode(errorCode);
        }

        internal override String InternalToString()
        {
            return Message;
        }
    }
}