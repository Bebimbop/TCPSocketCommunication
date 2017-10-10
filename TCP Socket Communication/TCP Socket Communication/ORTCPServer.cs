using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCP_Socket_Communication
{
    public enum ORTCPServerState
    {
        LISTENING,
        CONNECTED,
        DISCONNECTED
    }

    public enum ORTCPServerStartListen
    {
        DONTLISTEN,
        AWAKE,
        START
    }

    public class ORTCPServer
    {
        public static string DefaultORTCPServerName = "NFLX_SuitUp_ShowControlServer";
        public static ORTCPSocketType DefaultSocketType = ORTCPSocketType.TEXT;
        public static int DefaultBufferSize = 1024;
        public static bool DefaultAutoListenOnDisconnected = true;
        public static int DefaultPort = 1933;
        public static string DefaultOnConnectMessage = "OnServerConnect";
        public static string DefaultOnDisconnectMessage = "OnServerDisconnect";
        public static string DefaultOnDataReceivedMessage = "OnDataReceived";

        public static ORTCPServer CreateInstance()
        {
            return CreateInstance(DefaultORTCPServerName);
        }

        public static ORTCPServer CreateInstance(string name)
        {
            ORTCPServer server = new ORTCPServer();
            return server;
        }

        public ORTCPServerStartListen listenOn = ORTCPServerStartListen.START;
        public bool autoListenOnDisconnect = DefaultAutoListenOnDisconnected;
        public int port = DefaultPort;
        public ORTCPSocketType socketType = DefaultSocketType;
        public int bufferSize = DefaultBufferSize;
        public string onConnectMessage = DefaultOnConnectMessage;
        public string onDisconnectMessage = DefaultOnDisconnectMessage;
        public string onDataReceivedMessage = DefaultOnDataReceivedMessage;

        private ORTCPServerState serverState;
        private NetworkStream stream;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        private System.Threading.Thread readThread;
        private TcpListener tcpListener;
        private TcpClient tcpClient;
        private Queue<ORTCPEventType> eventQueue;
        private Queue<string> messageQueue;
        private Queue<ORTCPSocketPacket> packetQueue;

        public bool IsConnected()
        {
            return serverState == ORTCPServerState.CONNECTED;
        }

        public ORTCPServerState State()
        {
            return serverState;
        }

        public TcpClient Client()
        {
            return tcpClient;
        }

        public void Start()
        {
            serverState = ORTCPServerState.DISCONNECTED;
            eventQueue = new Queue<ORTCPEventType>();
            messageQueue = new Queue<string>();
            packetQueue = new Queue<ORTCPSocketPacket>();

            StartListening(port);
        }

        private void StartListening()
        {
            StartListening(port);
        }

        private void StartListening(int _port)
        {
            if (serverState == ORTCPServerState.CONNECTED)
                return;

            port = _port;

            serverState = ORTCPServerState.LISTENING;

            messageQueue.Clear();
            eventQueue.Clear();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), tcpListener);
        }

        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
        {
            TcpListener _tcpListener = (TcpListener) asyncResult.AsyncState;
            tcpClient = _tcpListener.EndAcceptTcpClient(asyncResult);

            stream = tcpClient.GetStream();
            streamReader = new StreamReader(stream);
            streamWriter = new StreamWriter(stream);
            
            serverState = ORTCPServerState.CONNECTED;

            readThread = new Thread(ReadData);
            readThread.IsBackground = true;
            readThread.Start();

            eventQueue.Enqueue(ORTCPEventType.CONNECTED);
        }

        public void StopListening()
        {
            serverState = ORTCPServerState.DISCONNECTED;

            if (tcpListener == null)
                return;

            tcpListener.Stop();
            tcpListener = null;
        }

        public void Disconnect()
        {
            serverState = ORTCPServerState.DISCONNECTED;

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
                tcpClient?.Close();
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public void Send(string message)
        {
            if (!IsConnected())
                return;

            streamWriter.WriteLine(message);
            streamWriter.Flush();
        }

        public void SendBytes(byte[] bytes)
        {
            SendBytes(bytes, 0, bufferSize);
        }

        public void SendBytes(byte[] bytes, int offset, int size)
        {
            if (!IsConnected())
                return;

            stream.Write(bytes, offset, size);
            stream.Flush();
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

            serverState = ORTCPServerState.DISCONNECTED;
            tcpListener.Stop();
            eventQueue.Enqueue(ORTCPEventType.DISCONNECTED);
        }

        public void Update()
        {
            while (eventQueue.Count > 0)
            {
                ORTCPEventType eventType = eventQueue.Dequeue();

                ORTCPEventParams eventParams = new ORTCPEventParams();
                eventParams.eventType = eventType;
                eventParams.server = this;
                eventParams.socket = tcpClient;

                if (eventType == ORTCPEventType.CONNECTED)
                {
                    //Send an OnConnectedMessage
                }
                else if (eventType == ORTCPEventType.DISCONNECTED)
                {
                    streamReader?.Close();
                    streamWriter?.Close();
                    tcpClient?.Close();

                    //Send an OnDisconnectMessage

                    if(autoListenOnDisconnect)
                        StartListening();
                }
                else if (eventType == ORTCPEventType.DATARECEIVED)
                {
                    if (socketType == ORTCPSocketType.TEXT)
                        eventParams.message = messageQueue.Dequeue();
                    else
                        eventParams.packet = packetQueue.Dequeue();

                    //Send OnDataReceivedMessage
                }
            }
        }
    }
}
