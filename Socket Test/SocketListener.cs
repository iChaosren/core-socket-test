using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{
    public class SocketListener
    {
        private int port { get; set; }
        private int maxConnections { get; set; }
        private Socket socket { get; set; }
        private List<Socket> connections { get; set; }
        private object connectionLock { get; set; }

        public SocketListener(int port, int maxConnections)
        {
            this.port = port;
            this.maxConnections = maxConnections;
            connections = new List<Socket>();
            connectionLock = new object();
        }

        public void Start(Action<Socket> newSocketConnectionCallback)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                socket.Listen(maxConnections);

                Console.WriteLine($"Listening for socket connections on port {port}...");

                BlockAndAcceptConnections(newSocketConnectionCallback);
            }));
            thread.Start();
        }

        private void BlockAndAcceptConnections(Action<Socket> newSocketConnectionCallback)
        {
            while (socket != null)
            {
                Socket connection;
                try
                {
                    connection = socket.Accept();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket accept failed: {ex.Message}");
                    continue;
                }

                if (ShouldRefuseConnection())
                {
                    ShutdownSocket(connection);
                    Console.WriteLine("Socket Connection Refused.");
                    continue;
                }

                Console.WriteLine("Socket Connection Accepted.");

                DispatchThreadForNewConnection(connection, newSocketConnectionCallback);
            }
        }

        private bool ShouldRefuseConnection()
        {
            lock (connectionLock)
            {
                return connections.Count >= maxConnections;
            }
        }

        private void DispatchThreadForNewConnection(Socket connection, Action<Socket> newSocketConnectionCallback)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                ExecuteCallback(connection, newSocketConnectionCallback);

                lock (connectionLock)
                {
                    connections.Remove(connection);
                }
            }));
            thread.Start();

            lock (connectionLock)
            {
                connections.Add(connection);
            }
        }

        private static void ExecuteCallback(Socket connection, Action<Socket> newSocketConnectionCallback)
        {
            try
            {
                newSocketConnectionCallback(connection);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket connection closed forcibly: {ex.Message}");
            }
            finally
            {
                ShutdownSocket(connection);
                Console.WriteLine("Socket connection closed.");
            }
        }

        private static void ShutdownSocket(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket could not be shutdown: {ex.Message}");
            }
        }
    }
}
