using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ejChatClient
{
    class Program
    {
        public static Client client;
        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            client.Disconnect();
            args.Cancel = true;
        }
        public static void Main(string[] args)
        {
            Console.Write("Enter a name to use: ");
            string name = Console.ReadLine();
            string host = "localhost";
            int port = 6000;
            client = new Client(host, port, name);
            Console.CancelKeyPress += InterruptHandler;
            client.Connect();
            client.SendReceiveMessages();
        }
    }
}
