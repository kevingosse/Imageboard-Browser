using System;
using System.Threading;

namespace ImageBoardBrowser
{
    public class DiaporamaTimer
    {
        public DiaporamaTimer(Action callback, Func<DateTime?> getNextTickTime)
        {
            this.Callback = callback;
            this.GetNextTickTime = getNextTickTime;
            this.IsRunning = true;

            this.InternalThread = new Thread(this.Process) { IsBackground = true };
            this.InternalThread.Start();
        }

        protected Thread InternalThread { get; set; }

        protected bool IsRunning { get; set; }

        protected Action Callback { get; set; }

        protected Func<DateTime?> GetNextTickTime { get; set; }

        public void Stop()
        {
            this.IsRunning = false;
        }

        private void Process()
        {
            while (this.IsRunning)
            {
                var nextImageTime = this.GetNextTickTime();

                if (nextImageTime != null)
                {
                    if (nextImageTime.Value < DateTime.UtcNow)
                    {
                        this.Callback();
                    }
                }

                Thread.Sleep(500);
            }
        }
    }
}
