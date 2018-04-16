using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer
{
    public class Program
    {
        private static string pendingConsoleWrite { get; set; }
        private static object pendingLock = new object();

        static void Main(string[] args)
        {
            SocketListener listener = new SocketListener(8899, 10);
            listener.Start(socket =>
            {
                var networkStream = new NetworkStream(socket);

                StreamWriter writer = new StreamWriter(networkStream) { AutoFlush = true };
                StreamReader reader = new StreamReader(networkStream);

                while (socket.Connected)
                {
                    string s = "";
                    try
                    {
                        s = reader.ReadLine();
                    }
                    catch(IOException io)
                    {
                        return;
                    }

                    lock (pendingLock)
                    {
                        if(!string.IsNullOrEmpty(s))
                            pendingConsoleWrite = "Client says: " + s;
                    }

                    writer.WriteLine("Request Received");
                }

            });
            Task.Factory.StartNew(() =>
                        {
                            while (true)
                            {
                                lock (pendingLock)
                                {
                                    if (!string.IsNullOrEmpty(pendingConsoleWrite))
                                    {
                                        Console.WriteLine(pendingConsoleWrite);
                                        pendingConsoleWrite = "";
                                    }
                                }
                            }
                        }
                        );
        }
    }
}
