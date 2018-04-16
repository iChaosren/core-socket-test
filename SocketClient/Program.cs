using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        private static Socket socket;
        const int PORT = 8899;

        static void Main(string[] args)
        {
            if (!ConnectToServer(PORT))
            {
                Console.ReadKey();
                return;
            }

            NetworkStream networkStream = new NetworkStream(socket);

            StreamReader reader = new StreamReader(networkStream);
            StreamWriter writer = new StreamWriter(networkStream) { AutoFlush = true };

            Task.Factory.StartNew(() =>
            {
                while (socket.Connected)
                    Console.WriteLine("Server says: " + reader.ReadLine());
            });

            Console.WriteLine("Send to Server: ");
            string UserInput = Console.ReadLine();
            while (UserInput.ToLower() != "exit" && socket.Connected)
            {
                writer.WriteLine(UserInput);
                UserInput = Console.ReadLine();
            }
            DisconnectFromServer();
        }

        private static bool ConnectToServer(int port)
        {
            Console.WriteLine($"Connecting to port {port}...");

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            }
            catch (SocketException)
            {
                Console.WriteLine("Failed to connect to server.");
                return false;
            }

            Console.WriteLine("Connected.");
            return true;
        }

        private static void DisconnectFromServer()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Dispose();
        }
    }
}





