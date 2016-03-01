namespace System.Diagnostics
{
    using System.Text;
    using System.Threading;
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal class StackFrameHelper
    {
        private Thread targetThread;
        private int[] rgiOffset;
        private int[] rgiILOffset;
        private MethodBase[] rgMethodBase;
        private Object dynamicMethods;
        private IntPtr[] rgMethodHandle;
        private String[] rgFilename;
        private int[] rgiLineNumber;
        private int[] rgiColumnNumber;
        private bool[] rgiLastFrameFromForeignExceptionStackTrace;
        private int iFrameCount;
        private bool fNeedFileInfo;
        public StackFrameHelper(bool fNeedFileLineColInfo, Thread target)
        {
            targetThread = target;
            rgMethodBase = null;
            rgMethodHandle = null;
            rgiOffset = null;
            rgiILOffset = null;
            rgFilename = null;
            rgiLineNumber = null;
            rgiColumnNumber = null;
            dynamicMethods = null;
            rgiLastFrameFromForeignExceptionStackTrace = null;
            iFrameCount = 0;
            fNeedFileInfo = fNeedFileLineColInfo;
        }

        public virtual MethodBase GetMethodBase(int i)
        {
            IntPtr mh = rgMethodHandle[i];
            if (mh.IsNull())
                return null;
            IRuntimeMethodInfo mhReal = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(mh, this));
            return RuntimeType.GetMethodBase(mhReal);
        }

        public virtual int GetOffset(int i)
        {
            return rgiOffset[i];
        }

        public virtual int GetILOffset(int i)
        {
            return rgiILOffset[i];
        }

        public virtual String GetFilename(int i)
        {
            return rgFilename[i];
        }

        public virtual int GetLineNumber(int i)
        {
            return rgiLineNumber[i];
        }

        public virtual int GetColumnNumber(int i)
        {
            return rgiColumnNumber[i];
        }

        public virtual bool IsLastFrameFromForeignExceptionStackTrace(int i)
        {
            return (rgiLastFrameFromForeignExceptionStackTrace == null) ? false : rgiLastFrameFromForeignExceptionStackTrace[i];
        }

        public virtual int GetNumberOfFrames()
        {
            return iFrameCount;
        }

        public virtual void SetNumberOfFrames(int i)
        {
            iFrameCount = i;
        }

        void OnSerializing(StreamingContext context)
        {
            rgMethodBase = (rgMethodHandle == null) ? null : new MethodBase[rgMethodHandle.Length];
            if (rgMethodHandle != null)
            {
                for (int i = 0; i < rgMethodHandle.Length; i++)
                {
                    if (!rgMethodHandle[i].IsNull())
                        rgMethodBase[i] = RuntimeType.GetMethodBase(new RuntimeMethodInfoStub(rgMethodHandle[i], this));
                }
            }
        }

        void OnSerialized(StreamingContext context)
        {
            rgMethodBase = null;
        }

        void OnDeserialized(StreamingContext context)
        {
            rgMethodHandle = (rgMethodBase == null) ? null : new IntPtr[rgMethodBase.Length];
            if (rgMethodBase != null)
            {
                for (int i = 0; i < rgMethodBase.Length; i++)
                {
                    if (rgMethodBase[i] != null)
                        rgMethodHandle[i] = rgMethodBase[i].MethodHandle.Value;
                }
            }

            rgMethodBase = null;
        }
    }

    public class StackTrace
    {
        private StackFrame[] frames;
        private int m_iNumOfFrames;
        public const int METHODS_TO_SKIP = 0;
        private int m_iMethodsToSkip;
        public StackTrace()
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, null);
        }

        public StackTrace(bool fNeedFileInfo)
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }

        public StackTrace(int skipFrames)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, false, null, null);
        }

        public StackTrace(int skipFrames, bool fNeedFileInfo)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, fNeedFileInfo, null, null);
        }

        public StackTrace(Exception e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, false, null, e);
        }

        public StackTrace(Exception e, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, fNeedFileInfo, null, e);
        }

        public StackTrace(Exception e, int skipFrames)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, false, null, e);
        }

        public StackTrace(Exception e, int skipFrames, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException("skipFrames", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(skipFrames + METHODS_TO_SKIP, fNeedFileInfo, null, e);
        }

        public StackTrace(StackFrame frame)
        {
            frames = new StackFrame[1];
            frames[0] = frame;
            m_iMethodsToSkip = 0;
            m_iNumOfFrames = 1;
        }

        public StackTrace(Thread targetThread, bool needFileInfo)
        {
            m_iNumOfFrames = 0;
            m_iMethodsToSkip = 0;
            CaptureStackTrace(METHODS_TO_SKIP, needFileInfo, targetThread, null);
        }

        internal static extern void GetStackFramesInternal(StackFrameHelper sfh, int iSkip, Exception e);
        internal static int CalculateFramesToSkip(StackFrameHelper StackF, int iNumFrames)
        {
            int iRetVal = 0;
            String PackageName = "System.Diagnostics";
            for (int i = 0; i < iNumFrames; i++)
            {
                MethodBase mb = StackF.GetMethodBase(i);
                if (mb != null)
                {
                    Type t = mb.DeclaringType;
                    if (t == null)
                        break;
                    String ns = t.Namespace;
                    if (ns == null)
                        break;
                    if (String.Compare(ns, PackageName, StringComparison.Ordinal) != 0)
                        break;
                }

                iRetVal++;
            }

            return iRetVal;
        }

        private void CaptureStackTrace(int iSkip, bool fNeedFileInfo, Thread targetThread, Exception e)
        {
            m_iMethodsToSkip += iSkip;
            StackFrameHelper StackF = new StackFrameHelper(fNeedFileInfo, targetThread);
            GetStackFramesInternal(StackF, 0, e);
            m_iNumOfFrames = StackF.GetNumberOfFrames();
            if (m_iMethodsToSkip > m_iNumOfFrames)
                m_iMethodsToSkip = m_iNumOfFrames;
            if (m_iNumOfFrames != 0)
            {
                frames = new StackFrame[m_iNumOfFrames];
                for (int i = 0; i < m_iNumOfFrames; i++)
                {
                    bool fDummy1 = true;
                    bool fDummy2 = true;
                    StackFrame sfTemp = new StackFrame(fDummy1, fDummy2);
                    sfTemp.SetMethodBase(StackF.GetMethodBase(i));
                    sfTemp.SetOffset(StackF.GetOffset(i));
                    sfTemp.SetILOffset(StackF.GetILOffset(i));
                    sfTemp.SetIsLastFrameFromForeignExceptionStackTrace(StackF.IsLastFrameFromForeignExceptionStackTrace(i));
                    if (fNeedFileInfo)
                    {
                        sfTemp.SetFileName(StackF.GetFilename(i));
                        sfTemp.SetLineNumber(StackF.GetLineNumber(i));
                        sfTemp.SetColumnNumber(StackF.GetColumnNumber(i));
                    }

                    frames[i] = sfTemp;
                }

                if (e == null)
                    m_iMethodsToSkip += CalculateFramesToSkip(StackF, m_iNumOfFrames);
                m_iNumOfFrames -= m_iMethodsToSkip;
                if (m_iNumOfFrames < 0)
                {
                    m_iNumOfFrames = 0;
                }
            }
            else
                frames = null;
        }

        public virtual int FrameCount
        {
            get
            {
                return m_iNumOfFrames;
            }
        }

        public virtual StackFrame GetFrame(int index)
        {
            if ((frames != null) && (index < m_iNumOfFrames) && (index >= 0))
                return frames[index + m_iMethodsToSkip];
            return null;
        }

        public virtual StackFrame[] GetFrames()
        {
            if (frames == null || m_iNumOfFrames <= 0)
                return null;
            StackFrame[] array = new StackFrame[m_iNumOfFrames];
            Array.Copy(frames, m_iMethodsToSkip, array, 0, m_iNumOfFrames);
            return array;
        }

        public override String ToString()
        {
            return ToString(TraceFormat.TrailingNewLine);
        }

        internal enum TraceFormat
        {
            Normal,
            TrailingNewLine,
            NoResourceLookup
        }

        internal String ToString(TraceFormat traceFormat)
        {
            bool displayFilenames = true;
            String word_At = "at";
            String inFileLineNum = "in {0}:line {1}";
            if (traceFormat != TraceFormat.NoResourceLookup)
            {
                word_At = Environment.GetResourceString("Word_At");
                inFileLineNum = Environment.GetResourceString("StackTrace_InFileLineNumber");
            }

            bool fFirstFrame = true;
            StringBuilder sb = new StringBuilder(255);
            for (int iFrameIndex = 0; iFrameIndex < m_iNumOfFrames; iFrameIndex++)
            {
                StackFrame sf = GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    if (fFirstFrame)
                        fFirstFrame = false;
                    else
                        sb.Append(Environment.NewLine);
                    sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);
                    Type t = mb.DeclaringType;
                    if (t != null)
                    {
                        string fullName = t.FullName;
                        for (int i = 0; i < fullName.Length; i++)
                        {
                            char ch = fullName[i];
                            sb.Append(ch == '+' ? '.' : ch);
                        }

                        sb.Append('.');
                    }

                    sb.Append(mb.Name);
                    if (mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod)
                    {
                        Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                        sb.Append('[');
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (fFirstTyParam == false)
                                sb.Append(',');
                            else
                                fFirstTyParam = false;
                            sb.Append(typars[k].Name);
                            k++;
                        }

                        sb.Append(']');
                    }

                    sb.Append('(');
                    ParameterInfo[] pi = mb.GetParameters();
                    bool fFirstParam = true;
                    for (int j = 0; j < pi.Length; j++)
                    {
                        if (fFirstParam == false)
                            sb.Append(", ");
                        else
                            fFirstParam = false;
                        String typeName = "<UnknownType>";
                        if (pi[j].ParameterType != null)
                            typeName = pi[j].ParameterType.Name;
                        sb.Append(typeName);
                        sb.Append(' ');
                        sb.Append(pi[j].Name);
                    }

                    sb.Append(')');
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        String fileName = null;
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch (SecurityException)
                        {
                            displayFilenames = false;
                        }

                        if (fileName != null)
                        {
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }

                    if (sf.GetIsLastFrameFromForeignExceptionStackTrace())
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append(Environment.GetResourceString("Exception_EndStackTraceFromPreviousThrow"));
                    }
                }
            }

            if (traceFormat == TraceFormat.TrailingNewLine)
                sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        private static String GetManagedStackTraceStringHelper(bool fNeedFileInfo)
        {
            StackTrace st = new StackTrace(0, fNeedFileInfo);
            return st.ToString();
        }
    }
}