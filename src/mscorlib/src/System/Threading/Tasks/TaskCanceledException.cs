using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
    public class TaskCanceledException : OperationCanceledException
    {
        private Task m_canceledTask;
        public TaskCanceledException(): base (Environment.GetResourceString("TaskCanceledException_ctor_DefaultMessage"))
        {
        }

        public TaskCanceledException(string message): base (message)
        {
        }

        public TaskCanceledException(string message, Exception innerException): base (message, innerException)
        {
        }

        public TaskCanceledException(Task task): base (Environment.GetResourceString("TaskCanceledException_ctor_DefaultMessage"), task != null ? task.CancellationToken : new CancellationToken())
        {
            m_canceledTask = task;
        }

        protected TaskCanceledException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public Task Task
        {
            get
            {
                return m_canceledTask;
            }
        }
    }
}