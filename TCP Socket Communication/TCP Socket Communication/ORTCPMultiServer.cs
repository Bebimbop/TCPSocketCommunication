using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    public class ORTCPMultiServer : ORTCPAbstractMultiServer
    {
        public delegate void TCPServerMessageRecievedEvent(ORTCPEventParams eventParams);

        public event TCPServerMessageRecievedEvent OnTCPMessageReceived;

        private bool verbose = true;
        private int port = 1933;

        private static ORTCPMultiServer s_instance;
        public static ORTCPMultiServer Instance => s_instance;

        public void Start()
        {
            s_instance = this;
            isListening = false;
            newConnections = new Queue<NewConnection>();
            clients = new Dictionary<int, ORTCPClient>();
            Console.WriteLine("ORTCP Multi Server created.");
            StartListening();
        }

        private void StartListening()
        {
            StartListening(port);
        }

        private void StartListening(int _port)
        {
            //if(verbose)
                //
            if (isListening)
                return;

            this.port = _port;
            isListening = true;
            newConnections.Clear();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            AcceptClient();
        }

        private void StopListening()
        {
            isListening = false;

            if (tcpListener == null)
                return;
            tcpListener.Stop();
            tcpListener = null;
        }

        public void Update()
        {
            Console.WriteLine("Updating server.");
            while (newConnections.Count > 0)
            {
                NewConnection newconnection = newConnections.Dequeue();
                ORTCPClient client = ORTCPClient.CreateClientInstance("ORMultiServerclient", newconnection.tcpClient, this);

                int clientID = SaveClient(client);
                ORTCPEventParams eventParams = new ORTCPEventParams();
                eventParams.eventType = ORTCPEventType.CONNECTED;
                eventParams.client = client;
                eventParams.clientID = clientID;
                eventParams.socket = newconnection.tcpClient;
                Console.WriteLine("[TCPServer] New client connected.");
                //client.Start();
                client.Update();
            }

            if (clients.Count > 0)
            {
                for(int i = 0; i < clients.Count; i++)
                    clients[i].Update();
            }
        }

        public void DisconnectallClients()
        {
            //if(verbose)
                //
            foreach (KeyValuePair<int, ORTCPClient> entry in clients)
            {
                Console.WriteLine("Disconnecting Client: " + entry.Value);
                entry.Value.Disconnect();
            }

            clients.Clear();
        }

        public void SendAllClientsMessage(string message)
        {
            //Client isn't being added apparently.
            if(clients == null || clients.Count == 0)
                Console.WriteLine("No clients to message.");
            foreach (KeyValuePair<int, ORTCPClient> entry in clients)
            {
                Console.WriteLine("Client To Deliever To: " + entry.Value);
                entry.Value.Send(message);
            }
        }

        public void DisconnectclientWithID(int clientID)
        {
            //if(verbose)
                //
            ORTCPClient client = GetClient(clientID);
            client?.Disconnect();
        }

        public void SendClientWithIDMessage(int clientID, string message)
        {
            //if(verbose)
                //
            ORTCPClient client = GetClient(clientID);
            client?.Send(message);
        }

        public void OnServerConnect(ORTCPEventParams eventParams)
        {

        }

        public void OnClientDisconnect(ORTCPEventParams eventParams)
        {
            eventParams.clientID = GetClientID(eventParams.client);
            RemoveClient(eventParams.client);
        }

        public void OnDataReceived(ORTCPEventParams eventParams)
        {
            eventParams.clientID = GetClientID(eventParams.client);

            OnTCPMessageReceived?.Invoke(eventParams);
        }
    }
}
