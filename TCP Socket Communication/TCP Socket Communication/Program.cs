using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    class Program
    {
        static string receivedMessage = null;

        static void Main(string[] args)
        {
            string message = null;

            ORTCPClient client = new ORTCPClient();

            client.Start();
            client.OnTCPMessageReceived += OnReceiveServerMessage;

            while (message != "esc")
            {
                message = Console.ReadLine();

                client.Update();

                //Can send message to reset application
                //Needs a reconnect to be implemented.
                if (message != null && message.Equals("reset"))
                {
                    client.Send("{reset}");
                    //client.Reconnect();
                }

                if (receivedMessage != null)
                {
                    Console.WriteLine(receivedMessage);
                    receivedMessage = null;
                }
            }
        }

        private static void OnReceiveServerMessage(ORTCPEventParams e)
        {
            receivedMessage = e.message;
        }
    }
}
