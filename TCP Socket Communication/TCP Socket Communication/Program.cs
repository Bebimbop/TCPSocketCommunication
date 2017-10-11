using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    class Program
    {
        public static IObservable<ORTCPEventParams> _tcpMessageRecieved;
        public static ORTCPMultiServer Server;
        static void Main(string[] args)
        {
            
            Server = new ORTCPMultiServer();
            Server.Start(1983);
            
            _tcpMessageRecieved =   
                Observable
                    .FromEvent<ORTCPMultiServer.TCPServerMessageRecivedEvent, ORTCPEventParams>
                    (h => (p) => h(p), 
                        h => Server.OnTCPMessageRecived += h,
                        h => Server.OnTCPMessageRecived -= h);

            _tcpMessageRecieved.Subscribe(ServerMessage);
            Console.Read();
        }

        public static void ServerMessage(ORTCPEventParams e)
        {
            Server.SendAllClientsMessage("Fuck you!!!!!");
            //Console.WriteLine(e.message);
        }
    }
}

/*
  if (message != "" && message != "esc" && message != "/cmprestart" && 
                    message != "/cmpshutdown")
                {
                    Console.WriteLine("Sending Message to all Clients: " + message);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(message);
                    message = null;
                }

                if (message == "/cmprestart")
                {
                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.FileName = "cmd";
                    proc.Arguments = "/C shutdown /r /f /t 0";
                    Process.Start(proc);
                }
                else if (message == "/cmpshutdown")
                {
                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.FileName = "cmd";
                    proc.Arguments = "/C shutdown /s /f /t 0";
                    Process.Start(proc);
                }
*/