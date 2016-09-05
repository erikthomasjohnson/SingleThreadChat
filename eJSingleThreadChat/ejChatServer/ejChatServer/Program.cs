using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ejChatServer
{
    class Program
    {
        public static Server chat;
        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            chat.Shutdown();
            args.Cancel = true;
        }
        public static void Main(string[] args)
        {
            string name = "Erik's Amazing Chat Room";
            int port = 6000;
            chat = new Server(name, port);
            Console.CancelKeyPress += InterruptHandler;
            chat.Run();
        }
    }
}
