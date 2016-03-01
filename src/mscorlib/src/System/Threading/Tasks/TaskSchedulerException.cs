using System.Runtime.Serialization;

namespace System.Threading.Tasks
{
    public class TaskSchedulerException : Exception
    {
        public TaskSchedulerException(): base (Environment.GetResourceString("TaskSchedulerException_ctor_DefaultMessage"))
        {
        }

        public TaskSchedulerException(string message): base (message)
        {
        }

        public TaskSchedulerException(Exception innerException): base (Environment.GetResourceString("TaskSchedulerException_ctor_DefaultMessage"), innerException)
        {
        }

        public TaskSchedulerException(string message, Exception innerException): base (message, innerException)
        {
        }

        protected TaskSchedulerException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}