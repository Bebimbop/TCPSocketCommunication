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

        public void DisconnectallClients()
        {
            //if(verbose)
                //
            foreach (KeyValuePair<int, ORTCPClient> entry in clients)
                entry.Value.Disconnect();

            clients.Clear();
        }

        public void SendAllClientsMessage(string message)
        {
            //if(verbose)
                //
            foreach (KeyValuePair<int, ORTCPClient> entry in clients)
                entry.Value.Send(message);
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
