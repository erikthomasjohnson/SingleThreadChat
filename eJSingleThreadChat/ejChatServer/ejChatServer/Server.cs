using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ejChatServer
{
    class Server
    {
        private int emptyUser = 0;
        private TcpListener _listener;
        private List<TcpClient> _clientList = new List<TcpClient>();
        private Dictionary<TcpClient, string> _names = new Dictionary<TcpClient, string>();
        private Queue<string> _messageQueue = new Queue<string>();
        public readonly string ChatName;
        public readonly int Port;
        public bool Running { get; private set; }
        public readonly int BufferSize = 2 * 1024;
        public Server(string chatName, int port)
        {
            ChatName = chatName;
            Port = port;
            Running = false;
            _listener = new TcpListener(IPAddress.Any, Port);
        }
        public void Shutdown()
        {
            Running = false;
            Console.WriteLine("Shutting down server");
        }
        public void Run()
        {
            Console.WriteLine("Starting the \"{0}\" TCP Chat Server on port {1}.", ChatName, Port);
            Console.WriteLine("Press Ctrl-C to shut down the server at any time.");
            _listener.Start();
            Running = true;
            while (Running)
            {
                if (_listener.Pending())
                    _handleNewConnection();
                _checkForDisconnects();
                _checkForNewMessages();
                _sendMessages();
                Thread.Sleep(10);
            }
            foreach (TcpClient m in _clientList)
                _cleanupClient(m);
            _listener.Stop();
            Console.WriteLine("Server is shut down.");
        }
        private void _handleNewConnection()
        {
            bool good = false;
            TcpClient newClient = _listener.AcceptTcpClient();
            NetworkStream netStream = newClient.GetStream();
            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;
            EndPoint endPoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine("Handling a new client from {0}...", endPoint);
            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = netStream.Read(msgBuffer, 0, msgBuffer.Length);
            if (bytesRead > 0)
            {
                string msg = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);
                if (!msg.StartsWith("name:") || msg.Substring(msg.IndexOf(':') + 1) == "")
                {
                    emptyUser++;
                    msg = "name:User " + emptyUser.ToString();
                }

                if (msg.StartsWith("name:"))
                {
                    string name = msg.Substring(msg.IndexOf(':') + 1);

                    if (_names.ContainsValue(name))
                    {
                        byte[] msgBufferReply = Encoding.UTF8.GetBytes("That name is already taken, please enter a different display name: ");
                        newClient.GetStream().Write(msgBufferReply, 0, msgBufferReply.Length);
                        _cleanupClient(newClient);
                    }
                    else if ((name != string.Empty) && (!_names.ContainsValue(name)))
                    {
                        good = true;
                        _names.Add(newClient, name);
                        _clientList.Add(newClient);
                        Console.WriteLine("{0} is a Messenger with the name {1}.", endPoint, name);
                        _messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    Console.WriteLine("Wasn't able to identify {0} as a Client.", endPoint);
                    _cleanupClient(newClient);
                }
            }
            if (!good)
                newClient.Close();
        }
        private void _checkForDisconnects()
        {
            foreach (TcpClient m in _clientList.ToArray())
            {
                if (_isDisconnected(m))
                {
                    string name = _names[m];
                    Console.WriteLine("Messeger {0} has left.", name);
                    _messageQueue.Enqueue(String.Format("{0} has left the chat", name));
                    _clientList.Remove(m);
                    _names.Remove(m);
                    _cleanupClient(m);
                }
            }
        }
        private void _checkForNewMessages()
        {
            foreach (TcpClient m in _clientList)
            {
                int messageLength = m.Available;
                if (messageLength > 0)
                {
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length);
                    string msgIn = String.Format("{0}: {1}", _names[m], Encoding.UTF8.GetString(msgBuffer));
                    _messageQueue.Enqueue(msgIn);
                }
            }
        }
        private void _sendMessages()
        {
            foreach (string msgOut in _messageQueue)
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msgOut);
                foreach (TcpClient m in _clientList)
                    m.GetStream().Write(msgBuffer, 0, msgBuffer.Length);
            }
            _messageQueue.Clear();
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
        private static void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close();
            client.Close();
        }
    }
}
