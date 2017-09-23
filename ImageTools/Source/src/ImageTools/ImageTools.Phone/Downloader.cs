using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace ImageTools
{
    public class Downloader
    {
        protected Downloader(Stream networkStream, Stream destinationStream, Action callback)
        {
            this.Source = networkStream;
            this.Destination = destinationStream;
            this.Callback = callback;

            this.InternalThread = new Thread(this.Download);
            this.InternalThread.Start();
        }

        protected Stream Source { get; private set; }
        protected Stream Destination { get; private set; }

        protected Thread InternalThread { get; private set; }

        private Action Callback { get; set; }

        public static void Start(Stream networkStream, Stream destinationStream, Action callback)
        {
            new Downloader(networkStream, destinationStream, callback);
        }

        private void Download()
        {
            const int BufferSize = 8192;

            try
            {
                while (true)
                {
                    var buffer = new byte[BufferSize];

                    int count = this.Source.Read(buffer, 0, buffer.Length);

                    if (count > 0)
                    {
                        this.Destination.Write(buffer, 0, count);
                    }

                    var b = this.Source.ReadByte();

                    if (b == -1)
                    {
                        break;
                    }

                    this.Destination.WriteByte((byte)b);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                this.Source.Close();
            }

            if (this.Callback != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(this.Callback);
            }
        }
    }
}
