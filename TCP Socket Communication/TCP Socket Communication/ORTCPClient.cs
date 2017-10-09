using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    public enum ORTCPClientState
    {
        CONNECTING,
        CONNECTED,
        DISCONNECTED,
    }

    public enum ORTCPClientStartConnection
    {
        DONTCONNECT,
        AWAKE,
        START
    }

    public class ORTCPClient
    {
        public delegate void TCPServerMessageRecievedEvent(ORTCPEventParams eventParams);

        public event TCPServerMessageRecievedEvent OnTCPMessageReceived;

        public bool verbose = true;

        private bool autoConnectOnDisconnect = true;
        private float disconnectTryInterval = 3;
        private bool autoConnectOnConnectionRefused = true;
        private float connectionRefusedTryInterval = 3;
        private string hostname = "127.0.0.1";
        private int port = 1933;
        private ORTCPSocketType socketType = ORTCPSocketType.TEXT;
        private int bufferSize = 1024;

        private ORTCPClientState clientState;
        private NetworkStream stream;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        private System.Threading.Thread readthread;
        private TcpClient client;
        private Queue<ORTCPEventType> eventQueue;
        private Queue<string> messageQueue;
        private Queue<ORTCPSocketPacket> packetQueue;

        private ORTCPMultiServer serverDelegate;

        public bool IsConnected()
        {
            return clientState == ORTCPClientState.CONNECTED;
        }

        public ORTCPClientState State()
        {
            return clientState;
        }

        public TcpClient Client()
        {
            return client;
        }

        public TcpClient tcpClient()
        {
            return client;
        }

        public static ORTCPClient CreateClientInstance(string name, TcpClient tcpClient,
            ORTCPMultiServer serverDelegate)
        {
            ORTCPClient client = new ORTCPClient();
            client.SetTcpClient(tcpClient);
            client.serverDelegate = serverDelegate;
            client.verbose = false;
            return client;
        }

        public void Start()
        {
            clientState = ORTCPClientState.DISCONNECTED;
            eventQueue = new Queue<ORTCPEventType>();
            messageQueue = new Queue<string>();
            packetQueue = new Queue<ORTCPSocketPacket>();

            Connect();
        }

        private void Connect()
        {
            Connect(hostname, port);
        }

        private void Connect(string _hostName, int _port)
        {
            if (clientState == ORTCPClientState.CONNECTED)
                return;

            hostname = _hostName;
            port = _port;
            clientState = ORTCPClientState.CONNECTING;
            messageQueue.Clear();
            eventQueue.Clear();
            client = new TcpClient();
            client.BeginConnect(hostname, port, new AsyncCallback(ConnectCallback), client);
        }

        private void ConnectCallback(IAsyncResult asyncResult)
        {
            try
            {
                TcpClient tcpClient = (TcpClient) asyncResult.AsyncState;
                tcpClient.EndConnect(asyncResult);
                SetTcpClient(tcpClient);
            }
            catch (Exception e)
            {
                eventQueue.Enqueue(ORTCPEventType.CONNECTIONREFUSED);
                Console.WriteLine(e);
                throw;
            }
        }

        public void Disconnect()
        {
            clientState = ORTCPClientState.DISCONNECTED;
            try
            {
                streamReader?.Close();
            }
            catch (Exception e)
            {
                e.ToString();
            }

            try
            {
                streamWriter?.Close();
            }
            catch (Exception e)
            {
                e.ToString();
            }

            try
            {
                client?.Close();
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public void Send(string message)
        {
            //if(verbose)
                //
            if (!IsConnected())
                return;
            streamWriter.WriteLine(message);
            streamWriter.Flush();
        }

        public void SendBytes(byte[] bytes)
        {
            SendBytes(bytes, 0, bytes.Length);
        }

        private void SendBytes(byte[] bytes, int offset, int size)
        {
            if (!IsConnected())
                return;
            stream.Write(bytes, offset, size);
            stream.Flush();
        }

        private void SetTcpClient(TcpClient tcpClient)
        {
            client = tcpClient;
            if (client.Connected)
            {
                stream = client.GetStream();
                streamReader = new StreamReader(stream);
                streamWriter = new StreamWriter(stream);
                clientState = ORTCPClientState.CONNECTED;
                eventQueue.Enqueue(ORTCPEventType.CONNECTED);
                readthread = new Thread(ReadData);
                readthread.IsBackground = true;
                readthread.Start();
            }
            else
                clientState = ORTCPClientState.DISCONNECTED;
        }

        private void ReadData()
        {
            bool endOfStream = false;

            while (!endOfStream)
            {
                if (socketType == ORTCPSocketType.TEXT)
                {
                    string response = null;
                    try
                    {
                        response = streamReader.ReadLine();
                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }

                    if (response != null)
                    {
                        response = response.Replace(Environment.NewLine, "");
                        eventQueue.Enqueue(ORTCPEventType.DATARECEIVED);
                        messageQueue.Enqueue(response);
                    }
                    else
                        endOfStream = true;
                }
                else if (socketType == ORTCPSocketType.BINARY)
                {
                    byte[] bytes = new byte[bufferSize];
                    int bytesRead = stream.Read(bytes, 0, bufferSize);

                    if (bytesRead == 0)
                        endOfStream = true;
                    else
                    {
                        eventQueue.Enqueue(ORTCPEventType.DATARECEIVED);
                        packetQueue.Enqueue(new ORTCPSocketPacket(bytes, bytesRead));
                    }
                }
            }

            clientState = ORTCPClientState.DISCONNECTED;
            client.Close();
            eventQueue.Enqueue(ORTCPEventType.DISCONNECTED);
        }

        public void Update()
        {
            while (eventQueue.Count > 0)
            {
                ORTCPEventType eventType = eventQueue.Dequeue();
                ORTCPEventParams eventParams = new ORTCPEventParams();
                eventParams.eventType = eventType;
                eventParams.client = this;
                eventParams.socket = client;

                if (eventType == ORTCPEventType.CONNECTED)
                {
                    //if(verbose)
                    serverDelegate?.OnServerConnect(eventParams);
                }
                else if (eventType == ORTCPEventType.DISCONNECTED)
                {
                    //if(verbose)
                    serverDelegate?.OnClientDisconnect(eventParams);
                    streamReader.Close();
                    streamWriter.Close();
                    client.Close();

                    //reconnect
                }
                else if (eventType == ORTCPEventType.CONNECTIONREFUSED)
                {
                    //reconnect
                }
                else if (eventType == ORTCPEventType.DATARECEIVED)
                {
                    if (socketType == ORTCPSocketType.TEXT)
                    {
                        eventParams.message = messageQueue.Dequeue();

                        //if(verbose)

                        OnTCPMessageReceived?.Invoke(eventParams);
                    }
                    else
                        eventParams.packet = packetQueue.Dequeue();

                    serverDelegate?.OnDataReceived(eventParams);
                }
            }
        }
    }
}
