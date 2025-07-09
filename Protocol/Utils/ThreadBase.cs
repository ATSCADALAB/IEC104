using System.Threading;

namespace IEC104.Protocol.Utils
{
    public abstract class ThreadBase
    {
        private Thread thread;
        private bool running;

        public void Start()
        {
            if (thread != null && thread.IsAlive)
                return;

            running = true;
            thread = new Thread(() =>
            {
                try
                {
                    Run();
                }
                catch
                {
                    // Handle thread exceptions
                }
                finally
                {
                    running = false;
                }
            });
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            if (thread != null && thread.IsAlive)
            {
                thread.Join(5000); // Wait up to 5 seconds
            }
        }

        public bool IsRunning()
        {
            return running;
        }

        public abstract void Run();
    }
}