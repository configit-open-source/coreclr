using System.Diagnostics.Contracts;
using System.Threading;

namespace System
{
    public interface IAppDomainPauseManager
    {
        void Pausing();
        void Paused();
        void Resuming();
        void Resumed();
    }

    public class AppDomainPauseManager : IAppDomainPauseManager
    {
        public AppDomainPauseManager()
        {
            isPaused = false;
        }

        static AppDomainPauseManager()
        {
        }

        static readonly AppDomainPauseManager instance = new AppDomainPauseManager();
        internal static AppDomainPauseManager Instance
        {
            [System.Security.SecurityCritical]
            get
            {
                return instance;
            }
        }

        public void Pausing()
        {
        }

        public void Paused()
        {
            Contract.Assert(!isPaused);
            if (ResumeEvent == null)
                ResumeEvent = new ManualResetEvent(false);
            else
                ResumeEvent.Reset();
            Timer.Pause();
            isPaused = true;
        }

        public void Resuming()
        {
            Contract.Assert(isPaused);
            isPaused = false;
            ResumeEvent.Set();
        }

        public void Resumed()
        {
            Timer.Resume();
        }

        private static volatile bool isPaused;
        internal static bool IsPaused
        {
            [System.Security.SecurityCritical]
            get
            {
                return isPaused;
            }
        }

        internal static ManualResetEvent ResumeEvent
        {
            [System.Security.SecurityCritical]
            get;
            [System.Security.SecurityCritical]
            set;
        }
    }
}