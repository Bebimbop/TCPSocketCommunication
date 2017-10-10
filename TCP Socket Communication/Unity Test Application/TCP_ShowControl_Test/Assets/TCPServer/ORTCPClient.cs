using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using  System.Net.Sockets;
using System.Threading;

public enum ORTCPClientState
{
    Connecting,
    Connected,
    Disconnected
}

public enum ORTCPClientStartConnection
{
    DontConnect,
    Awake,
    Start
}


public class ORTCPClient : MonoBehaviour
{
    public delegate void TCPServerMessageRecievedEvent(ORTCPEventParams eventParams);

    public event TCPServerMessageRecievedEvent OnTCPMessageRecieved;

    public bool verbose = true;

    private bool autoConnectOnDisconnect = true;
    private float disconnectTryInterval = 3;
    private bool autoConnectOnConnectionRefused = true;
    private float connectionRefusedTryInterval = 3;
    private string hostname = "127.0.0.1";
    private int port = 1933;
    private ORTCPSocketType socketType = ORTCPSocketType.Text;
    private int bufferSize = 1024;

    private ORTCPClientState clientState;
    private NetworkStream stream;
    private StreamWriter streamWriter;
    private StreamReader streamReader;
    private Thread readThread;
    private TcpClient client;
    private Queue<ORTCPEventType> eventQueue;
    private Queue<string> messageQueue;
    private Queue<ORSocketPacket> packetsQueue;

    private ORTCPMultiServer serverDelegate;

    public bool IsConnected
    {
        get { return clientState == ORTCPClientState.Connected; }
    }

    public ORTCPClientState State
    {
        get { return clientState; }
    }

    public TcpClient Client
    {
        get { return client; }
    }

    public TcpClient tcpClient
    {
        get { return client; }
    }

    //Only used by the server.
    public static ORTCPClient CreateClientInstance(string name, TcpClient tcpClient, ORTCPMultiServer serverDelegate)
    {
        GameObject go = new GameObject(name);
        ORTCPClient client = go.AddComponent<ORTCPClient>();
        client.SetTcpClient(tcpClient);
        client.serverDelegate = serverDelegate;
        client.verbose = false;
        return client;
    }

    void Awake()
    {
        clientState = ORTCPClientState.Disconnected;
        eventQueue = new Queue<ORTCPEventType>();
        messageQueue = new Queue<string>();
        packetsQueue = new Queue<ORSocketPacket>();
    }

    void Start()
    {
        Connect();
    }

    void Update()
    {
        while (eventQueue.Count > 0)
        {
            ORTCPEventType eventType = eventQueue.Dequeue();

            ORTCPEventParams eventParams = new ORTCPEventParams();
            eventParams.eventType = eventType;
            eventParams.client = this;
            eventParams.socket = client;

            if (eventType == ORTCPEventType.Connected)
            {
                if (verbose)
                    print("[TCPClient] Connected to server.");
                if (serverDelegate != null)
                {
                    print("[TCPClient] Server delegate is not null. Setting Event Params");
                    serverDelegate.OnServerConnect(eventParams);
                }
            }
            else if (eventType == ORTCPEventType.Disconnected)
            {
                if (verbose)
                    print("[TCPClient] Disconnected from server.");
                if (serverDelegate != null)
                    serverDelegate.OnClientDisconnect(eventParams);

                streamReader.Close();
                streamWriter.Close();
                client.Close();

                if (autoConnectOnDisconnect)
                    ORTimer.Execute(gameObject, disconnectTryInterval, "OnDisconnectTimer");
            }
            else if (eventType == ORTCPEventType.DataReceived)
            {
                if (socketType == ORTCPSocketType.Text)
                {
                    eventParams.message = messageQueue.Dequeue();

                    if (verbose)
                        print("[TCPClient] DataRecieved: " + eventParams.message);

                    if (OnTCPMessageRecieved != null)
                        OnTCPMessageRecieved(eventParams);
                }
                else
                    eventParams.packet = packetsQueue.Dequeue();

                if (serverDelegate != null)
                    serverDelegate.OnDataReceived(eventParams);
            }
            else if (eventType == ORTCPEventType.ConnectionRefused)
            {
                if(verbose)
                    print("[TCPClient] Connection refused. Trying again.");
                if (autoConnectOnConnectionRefused)
                    ORTimer.Execute(gameObject, connectionRefusedTryInterval, "OnConnectionRefusedTimer");
            }


        }
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Disconnect();
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
            eventQueue.Enqueue(ORTCPEventType.ConnectionRefused);
            Debug.LogWarning("Connection Excpetion: " + e.Message);
        }
    }

    private void ReadData()
    {
        bool endOfStream = false;

        while (!endOfStream)
        {
            if (socketType == ORTCPSocketType.Text)
            {
                String response = null;
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
                    eventQueue.Enqueue(ORTCPEventType.DataReceived);
                    messageQueue.Enqueue(response);
                }
                else
                    endOfStream = true;
            }
            else if (socketType == ORTCPSocketType.Binary)
            {
                byte[] bytes = new byte[bufferSize];
                int bytesRead = stream.Read(bytes, 0, bufferSize);

                if (bytesRead == 0)
                    endOfStream = true;
                else
                {
                    eventQueue.Enqueue(ORTCPEventType.DataReceived);
                    packetsQueue.Enqueue(new ORSocketPacket(bytes, bytesRead));
                }
            }
        }

        clientState = ORTCPClientState.Disconnected;
        client.Close();
        eventQueue.Enqueue(ORTCPEventType.Disconnected);
    }

    private void OnDisconnectTimer(ORTimer timer)
    {
        Connect();
    }

    private void OnConnectionRefusedtimer(ORTimer timer)
    {
        Connect();
    }

    public void Connect()
    {
        Connect(hostname, port);
    }

    public void Connect(string hostName, int portNumber)
    {
        if(verbose)print("[TCPClient] trying to connect to " + hostName + " " + portNumber);
        if (clientState == ORTCPClientState.Connected)
            return;

        this.hostname = hostName;
        this.port = portNumber;
        clientState = ORTCPClientState.Connecting;
        messageQueue.Clear();
        eventQueue.Clear();
        client = new TcpClient();
        client.BeginConnect(hostName, port, new AsyncCallback(ConnectCallback), client);
    }

    public void Disconnect()
    {
        clientState = ORTCPClientState.Disconnected;
        try
        {
            if (streamReader != null) streamReader.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }

        try
        {
            if (streamWriter != null) streamWriter.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }

        try
        {
            if(client != null)
                client.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }
    }

    public void Send(string message)
    {
        if (verbose)
            print("[TCPClient] sending message. " + message);
        if (!IsConnected)
            return;
        streamWriter.WriteLine(message);
        streamWriter.Flush();
    }

    public void SendBytes(byte[] bytes)
    {
        SendBytes(bytes, 0, bytes.Length);
    }

    public void SendBytes(byte[] bytes, int offset, int size)
    {
        if (!IsConnected)
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
            clientState = ORTCPClientState.Connected;
            eventQueue.Enqueue(ORTCPEventType.Connected);
            readThread = new Thread(ReadData);
            readThread.IsBackground = true;
            readThread.Start();
        }
        else
            clientState = ORTCPClientState.Disconnected;
    }
}
