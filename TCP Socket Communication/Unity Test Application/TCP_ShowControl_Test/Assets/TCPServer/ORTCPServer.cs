using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public enum ORTCPServerState
{
    Listening,
    Connected,
    Disconnected
}

public enum ORTCPServerStartListen
{
    DontListen,
    Awake,
    Start
}


public class ORTCPServer : MonoBehaviour
{
    public static string DefaultORTCPServerName = "ORTCPServer";
    public static ORTCPSocketType DefaultSocketType = ORTCPSocketType.Text;
    public static int DefaultBufferSize = 1024;
    public static bool DefaultAutoListenOnDisconnect = true;
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
        GameObject go = new GameObject(name);
        ORTCPServer server = go.AddComponent<ORTCPServer>();

        return server;
    }

    public ORTCPServerStartListen listenOn = ORTCPServerStartListen.Start;
    public bool autoListenOnDisconnect = DefaultAutoListenOnDisconnect;
    public int port = DefaultPort;
    public ORTCPSocketType socketType = DefaultSocketType;
    public int bufferSize = DefaultBufferSize;
    public GameObject[] listeners = null;
    public string onConnectMessage = DefaultOnConnectMessage;
    public string onDisconnectMessage = DefaultOnDisconnectMessage;
    public string onDataReceivedMessage = DefaultOnDataReceivedMessage;

    private ORTCPServerState serverState;
    private NetworkStream stream;
    private StreamWriter streamWriter;
    private StreamReader streamReader;
    private Thread readThread;
    private TcpListener tcpListener;
    private TcpClient tcpClient;
    private Queue<ORTCPEventType> eventQueue;
    private Queue<string> messageQueue;
    private Queue<ORSocketPacket> packetQueue;

    public bool IsConnected
    {
        get { return serverState == ORTCPServerState.Connected; }
    }

    public ORTCPServerState State
    {
        get { return serverState; }
    }

    public TcpClient Client
    {
        get { return tcpClient; }
    }

    void Awake()
    {
        serverState = ORTCPServerState.Disconnected;
        eventQueue = new Queue<ORTCPEventType>();
        messageQueue = new Queue<string>();
        packetQueue = new Queue<ORSocketPacket>();

        if (listenOn == ORTCPServerStartListen.Awake)
            StartListening(port, listeners);
    }

    void Start()
    {
        if (listenOn == ORTCPServerStartListen.Start)
            StartListening(port, listeners);
    }

    void Update()
    {
        while (eventQueue.Count > 0)
        {
            ORTCPEventType eventType = eventQueue.Dequeue();

            ORTCPEventParams eventParams = new ORTCPEventParams();
            eventParams.eventType = eventType;
            eventParams.server = this;
            eventParams.socket = tcpClient;

            if (eventType == ORTCPEventType.Connected)
            {
                print("[TCPServer] New client connected.");
                foreach(GameObject listener in listeners)
                    listener.SendMessage(onConnectMessage, eventParams, SendMessageOptions.DontRequireReceiver);
            }
            else if (eventType == ORTCPEventType.Disconnected)
            {
                streamReader.Close();
                streamWriter.Close();
                tcpClient.Close();

                print("[TCPServer] Server Disconnected");

                foreach(GameObject listener in listeners)
                    listener.SendMessage(onDisconnectMessage, eventParams, SendMessageOptions.DontRequireReceiver);

                if(autoListenOnDisconnect)
                    StartListening();
            }
            else if (eventType == ORTCPEventType.DataReceived)
            {
                if (socketType == ORTCPSocketType.Text)
                {
                    eventParams.message = messageQueue.Dequeue();
                    print("[TCPServer] Server DataReceived: " + eventParams.message);
                }
                else
                    eventParams.packet = packetQueue.Dequeue();

                foreach(GameObject listener in listeners)
                    listener.SendMessage(onDataReceivedMessage, eventParams, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void OnDestroy()
    {
        Disconnect();
        StopListening();
    }

    void OnApplicationQuit()
    {
        Disconnect();
        StopListening();
    }

    private void AcceptTcpClientCallback(IAsyncResult asyncResult)
    {
        TcpListener _tcpListener = (TcpListener) asyncResult.AsyncState;

        tcpClient = _tcpListener.EndAcceptTcpClient(asyncResult);

        stream = tcpClient.GetStream();
        streamReader = new StreamReader(stream);
        streamWriter = new StreamWriter(stream);

        serverState = ORTCPServerState.Connected;

        readThread = new Thread(ReadData);
        readThread.IsBackground = true;
        readThread.Start();

        eventQueue.Enqueue(ORTCPEventType.Connected);
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
                    packetQueue.Enqueue(new ORSocketPacket(bytes, bytesRead));
                }
            }
        }

        serverState = ORTCPServerState.Disconnected;
        tcpListener.Stop();
        eventQueue.Enqueue(ORTCPEventType.Disconnected);
    }

    public void StartListening()
    {
        StartListening(port, listeners);
    }

    public void StartListening(int portNumber)
    {
        StartListening(portNumber, listeners);
    }

    public void StartListening(int portNumber, GameObject[] _listeners)
    {
        print("[TCPCServer] Start Listening " + portNumber);

        if (serverState == ORTCPServerState.Listening)
            return;

        this.port = portNumber;
        this.listeners = _listeners;

        serverState = ORTCPServerState.Listening;

        messageQueue.Clear();
        eventQueue.Clear();

        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), listeners);
    }

    public void StopListening()
    {
        serverState = ORTCPServerState.Disconnected;

        if (tcpListener == null)
            return;

        tcpListener.Stop();
        tcpListener = null;
    }

    public void Disconnect()
    {
        print("[TCPServer] Server Disconnecting.");
        serverState = ORTCPServerState.Disconnected;

        try
        {
            if(streamReader != null)
                streamReader.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }

        try
        {
            if(streamWriter != null)
                streamWriter.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }

        try
        {
            if(tcpClient != null)
                tcpClient.Close();
        }
        catch (Exception e)
        {
            e.ToString();
        }
    }

    public void Send(string message)
    {
        print("[TCPServer] Sending: " + message);

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
}
