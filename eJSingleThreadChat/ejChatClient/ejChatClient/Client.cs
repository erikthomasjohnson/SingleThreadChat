using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ejChatClient
{
    class Client
    {
     
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }
        private bool _disconnectRequested = false;
        public readonly int BufferSize = 2 * 1024;
        private NetworkStream _msgStream = null;
        public readonly string Name;
        public Client(string serverAddress, int port, string name)
        {
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;
            ServerAddress = serverAddress;
            Port = port;
            Name = name;
        }
        public void Connect()
        {
            _client.Connect(ServerAddress, Port);
            EndPoint endPoint = _client.Client.RemoteEndPoint;
            if (_client.Connected)
            {
                Console.WriteLine("Connected to the server at {0}.", endPoint);
                _msgStream = _client.GetStream();
                byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format("name:{0}", Name));
                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);
                if (!_isDisconnected(_client))
                {
                    Running = true;
                    Console.WriteLine("Press Ctrl-C to exit the Client at any time.");
                }
                else
                {
                    _cleanupNetworkResources();
                    Console.WriteLine("The server didn't recognise us as a Client.\n:[");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}.", endPoint);
            }
        }
        public void Disconnect()
        {
            Running = false;
            _disconnectRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }
        public void SendReceiveMessages()
        {
            bool wasRunning = Running;
            while (Running)
            {
                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream.Read(msgBuffer, 0, messageLength);
                    string msgIn = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msgIn);
                }
                if (Console.KeyAvailable)
                {
                    Console.Write("{0}> ", Name);
                    string msgOut = Console.ReadLine();
                    Console.CursorTop -= 1;
                    if ((msgOut.ToLower() == "quit") || (msgOut.ToLower() == "exit"))
                    {
                        Console.WriteLine("Disconnecting...");
                        Running = false;
                    }
                    else if (msgOut != string.Empty)
                    {
                        byte[] msgBuffer = Encoding.UTF8.GetBytes(msgOut);
                        _msgStream.Write(msgBuffer, 0, msgBuffer.Length);
                    }
                }
                Thread.Sleep(10);
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n:[");
                }
                Running &= !_disconnectRequested;
            }
            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }
        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            Thread.Sleep(2000);
            _client.Close();
        }
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                return true;
            }
        }
    }
}
