﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    class Program
    {
        public static string serverMessage;

        static void Main(string[] args)
        {
            string message = null;
            ORTCPMultiServer server = new ORTCPMultiServer();
            server.Start();
            ORTCPMultiServer.Instance.OnTCPMessageReceived += ServerMessage;

            if(ORTCPMultiServer.Instance == null)
                Console.WriteLine("ORTCP Multi Server is null.");

            while (message != "esc")
            {
                ORTCPMultiServer.Instance.Update();

                message = Console.ReadLine();

                if (message != "" && message != "esc")
                {
                    Console.WriteLine("Sending Message to all Clients: " + message);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(message);
                    message = null;
                }
            }
        }

        public static void ServerMessage(ORTCPEventParams e)
        {
            Console.WriteLine(e.message);
        }
    }
}
