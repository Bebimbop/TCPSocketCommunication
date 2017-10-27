using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    class Program
    {
        private static IObservable<ORTCPEventParams> _tcpMessageRecieved;
        private static ORTCPMultiServer Server;
        private static ORTCPClient APP, SC;
        private static List<ORTCPEventParams>myComandList = new List<ORTCPEventParams>();

        private static Process TargetApp;
        private static int Port;
        
        static void Main(string[] args)
        {
            TargetApp = Process.Start(@"..\" + "NFLX_SuitUp.exe");
            Port = GetPort();
            
            Server = new ORTCPMultiServer();
            Server.Start(Port);
            
            _tcpMessageRecieved =   
                Observable
                    .FromEvent<ORTCPMultiServer.TCPServerMessageRecivedEvent, ORTCPEventParams>
                    (h => (p) => h(p), 
                        h => Server.OnTCPMessageRecived += h,
                        h => Server.OnTCPMessageRecived -= h);

            _tcpMessageRecieved.Subscribe(DoCommnad);
            Console.Read();
        }

        static void DoCommnad(ORTCPEventParams e)
        {
            var cmd = e.message;
            switch (cmd)
            {
                case "100":
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    APP = e.client;
                    var str = "[TCPServer] Sending Message to all Clients: TargetApp Connected";
                    Console.WriteLine(str);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(str);
                    APP.Send("Ready to receive cmds");
                    break;
                
                case "200":
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.White;
                    SC = e.client;
                    var str1 = "[TCPServer] Sending Message to all Clients: ShowControl Connected";
                    Console.WriteLine(str1);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(str1);
                    APP.Send("Show Control Connected");
                    break;
                    
                case "shutdown-machine":
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var str2 = "[TCPServer] Sending Message to all Clients: Shutting down in 3s";
                    Console.WriteLine(str2);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(str2);
                    APP.Send("Shutting down in 3s..");
                    Observable.Timer(TimeSpan.FromSeconds(3))
                        .Take(1)
                        .Subscribe(_ =>
                        {
                            ProcessStartInfo proc = new ProcessStartInfo();
                            proc.WindowStyle = ProcessWindowStyle.Hidden;
                            proc.FileName = "cmd";
                            proc.Arguments = "/C shutdown /s /t 0";
                            Process.Start(proc);
                        });
                }
                    break;

                case "restart-machine":
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var str3 = "[TCPServer] Sending Message to all Clients: Restarting in 3s";
                    Console.WriteLine(str3);
                    ORTCPMultiServer.Instance.SendAllClientsMessage(str3);
                    APP.Send("Shutting down in 3s...");
                    Observable.Timer(TimeSpan.FromSeconds(3))
                        .Take(1)
                        .Subscribe(_ =>
                        {
                            ProcessStartInfo proc = new ProcessStartInfo();
                            proc.WindowStyle = ProcessWindowStyle.Hidden;
                            proc.FileName = "cmd";
                            proc.Arguments = "/C shutdown /f /r /t 0";
                            Process.Start(proc);
                        });
                }
                    break;
                    
                case "restart-application":
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    APP.Send("Restarting the app..");

                    Observable.Timer(TimeSpan.FromSeconds(2))
                        .Take(1)
                        .Subscribe(_=>TargetApp.CloseMainWindow(),
                            () =>
                            {
                                Observable.Timer(TimeSpan.FromSeconds(2))
                                    .Take(1)
                                    .Subscribe(_=>TargetApp.Start());            
                            });
                }
                    break;
                    
                case "reset-application":
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    var str3 = "reset-application";
                    Console.WriteLine("[TCPServer] Sending Message to all Clients: ResetApp");
                    ORTCPMultiServer.Instance.SendAllClientsMessage(str3);               
                }
                    break;

                case "shutdown-application":
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    APP.Send("Shutting down the app...");
                    Observable.Timer(TimeSpan.FromSeconds(2))
                        .Take(1)
                        .Subscribe(_ => TargetApp.CloseMainWindow());
                }
                    break;

				case "appstatus":
				{
						Console.BackgroundColor = ConsoleColor.Blue;
						Console.ForegroundColor = ConsoleColor.White;
						Console.WriteLine("[TCPServer] Sending Message to all Clients: appstatus");
						ORTCPMultiServer.Instance.SendAllClientsMessage("appstatus");
				}
					break;
            }
        }
        
        static private int GetPort()
        {
            StreamReader reader = new StreamReader("Port.txt");
            var _port = 0; 
            int.TryParse(reader.ReadLine(),out _port);
            reader.Close();
            return _port;
        }

        
    }
}


