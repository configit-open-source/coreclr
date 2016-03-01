using System.Diagnostics.Contracts;
using System.Reflection;
using System.Security.Permissions;
using System.Text;

namespace System.Diagnostics
{
    public class StackFrame
    {
        private MethodBase method;
        private int offset;
        private int ILOffset;
        private String strFileName;
        private int iLineNumber;
        private int iColumnNumber;
        private bool fIsLastFrameFromForeignExceptionStackTrace;
        internal void InitMembers()
        {
            method = null;
            offset = OFFSET_UNKNOWN;
            ILOffset = OFFSET_UNKNOWN;
            strFileName = null;
            iLineNumber = 0;
            iColumnNumber = 0;
            fIsLastFrameFromForeignExceptionStackTrace = false;
        }

        public StackFrame()
        {
            InitMembers();
            BuildStackFrame(0 + StackTrace.METHODS_TO_SKIP, false);
        }

        public StackFrame(bool fNeedFileInfo)
        {
            InitMembers();
            BuildStackFrame(0 + StackTrace.METHODS_TO_SKIP, fNeedFileInfo);
        }

        public StackFrame(int skipFrames)
        {
            InitMembers();
            BuildStackFrame(skipFrames + StackTrace.METHODS_TO_SKIP, false);
        }

        public StackFrame(int skipFrames, bool fNeedFileInfo)
        {
            InitMembers();
            BuildStackFrame(skipFrames + StackTrace.METHODS_TO_SKIP, fNeedFileInfo);
        }

        internal StackFrame(bool DummyFlag1, bool DummyFlag2)
        {
            InitMembers();
        }

        public StackFrame(String fileName, int lineNumber)
        {
            InitMembers();
            BuildStackFrame(StackTrace.METHODS_TO_SKIP, false);
            strFileName = fileName;
            iLineNumber = lineNumber;
            iColumnNumber = 0;
        }

        public StackFrame(String fileName, int lineNumber, int colNumber)
        {
            InitMembers();
            BuildStackFrame(StackTrace.METHODS_TO_SKIP, false);
            strFileName = fileName;
            iLineNumber = lineNumber;
            iColumnNumber = colNumber;
        }

        public const int OFFSET_UNKNOWN = -1;
        internal virtual void SetMethodBase(MethodBase mb)
        {
            method = mb;
        }

        internal virtual void SetOffset(int iOffset)
        {
            offset = iOffset;
        }

        internal virtual void SetILOffset(int iOffset)
        {
            ILOffset = iOffset;
        }

        internal virtual void SetFileName(String strFName)
        {
            strFileName = strFName;
        }

        internal virtual void SetLineNumber(int iLine)
        {
            iLineNumber = iLine;
        }

        internal virtual void SetColumnNumber(int iCol)
        {
            iColumnNumber = iCol;
        }

        internal virtual void SetIsLastFrameFromForeignExceptionStackTrace(bool fIsLastFrame)
        {
            fIsLastFrameFromForeignExceptionStackTrace = fIsLastFrame;
        }

        internal virtual bool GetIsLastFrameFromForeignExceptionStackTrace()
        {
            return fIsLastFrameFromForeignExceptionStackTrace;
        }

        public virtual MethodBase GetMethod()
        {
            Contract.Ensures(Contract.Result<MethodBase>() != null);
            return method;
        }

        public virtual int GetNativeOffset()
        {
            return offset;
        }

        public virtual int GetILOffset()
        {
            return ILOffset;
        }

        public virtual String GetFileName()
        {
            if (strFileName != null)
            {
                FileIOPermission perm = new FileIOPermission(PermissionState.None);
                perm.AllFiles = FileIOPermissionAccess.PathDiscovery;
                perm.Demand();
            }

            return strFileName;
        }

        public virtual int GetFileLineNumber()
        {
            return iLineNumber;
        }

        public virtual int GetFileColumnNumber()
        {
            return iColumnNumber;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder(255);
            if (method != null)
            {
                sb.Append(method.Name);
                if (method is MethodInfo && ((MethodInfo)method).IsGenericMethod)
                {
                    Type[] typars = ((MethodInfo)method).GetGenericArguments();
                    sb.Append('<');
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

                    sb.Append('>');
                }

                sb.Append(" at offset ");
                if (offset == OFFSET_UNKNOWN)
                    sb.Append("<offset unknown>");
                else
                    sb.Append(offset);
                sb.Append(" in file:line:column ");
                bool useFileName = (strFileName != null);
                if (useFileName)
                {
                    try
                    {
                        FileIOPermission perm = new FileIOPermission(PermissionState.None);
                        perm.AllFiles = FileIOPermissionAccess.PathDiscovery;
                        perm.Demand();
                    }
                    catch (System.Security.SecurityException)
                    {
                        useFileName = false;
                    }
                }

                if (!useFileName)
                    sb.Append("<filename unknown>");
                else
                    sb.Append(strFileName);
                sb.Append(':');
                sb.Append(iLineNumber);
                sb.Append(':');
                sb.Append(iColumnNumber);
            }
            else
            {
                sb.Append("<null>");
            }

            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        private void BuildStackFrame(int skipFrames, bool fNeedFileInfo)
        {
            StackFrameHelper StackF = new StackFrameHelper(fNeedFileInfo, null);
            StackTrace.GetStackFramesInternal(StackF, 0, null);
            int iNumOfFrames = StackF.GetNumberOfFrames();
            skipFrames += StackTrace.CalculateFramesToSkip(StackF, iNumOfFrames);
            if ((iNumOfFrames - skipFrames) > 0)
            {
                method = StackF.GetMethodBase(skipFrames);
                offset = StackF.GetOffset(skipFrames);
                ILOffset = StackF.GetILOffset(skipFrames);
                if (fNeedFileInfo)
                {
                    strFileName = StackF.GetFilename(skipFrames);
                    iLineNumber = StackF.GetLineNumber(skipFrames);
                    iColumnNumber = StackF.GetColumnNumber(skipFrames);
                }
            }
        }
    }
}