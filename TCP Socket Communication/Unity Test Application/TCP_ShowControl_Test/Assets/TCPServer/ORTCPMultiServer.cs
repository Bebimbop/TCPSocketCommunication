using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public class ORTCPMultiServer : ORTCPAbstractMultiServer
{

    public delegate void TCPServerMessageReceivedEvent(ORTCPEventParams eventParams);
    public event TCPServerMessageReceivedEvent OnTCPMessageReceived;

    private bool verbose = true;
    private int port = 1933;

    private static ORTCPMultiServer s_instance;
    public static ORTCPMultiServer Instance { get { return s_instance; } }

    void Awake()
    {
        //if (s_instance != null && s_instance != this)
        //    Destroy(this.gameObject);
        //else
            s_instance = this;
    }

    void Start()
    {
        isListening = false;
        newConnections = new Queue<NewConnection>();
        clients = new Dictionary<int, ORTCPClient>();
        StartListening();
    }

    void Update()
    {
        while (newConnections.Count > 0)
        {
            NewConnection newConnection = newConnections.Dequeue();
            ORTCPClient client = ORTCPClient.CreateClientInstance("ORMultiServerClient", newConnection.tcpClient, this);

            int clientId = SaveClient(client);
            ORTCPEventParams eventParams = new ORTCPEventParams();
            eventParams.eventType = ORTCPEventType.Connected;
            eventParams.client = client;
            eventParams.clientID = clientId;
            eventParams.socket = newConnection.tcpClient;
            if (verbose)
                print("[TCPServer] New Client Connected.");
        }
    }

    void OnDestroy()
    {
        DisconnectAllClients();
        StopListening();
    }

    void OnApplicationQuit()
    {
        //Send a reset call to the clients before disconnecting
        Instance.SendAllClientsMessage("{state:20}");
        DisconnectAllClients();
        StopListening();
    }

    public void OnServerConnect(ORTCPEventParams eventParams)
    {
        
    }

    public void OnClientDisconnect(ORTCPEventParams eventParams)
    {
        if(verbose)
            print("[TCPServer] OnclientDisconnect.");

        eventParams.clientID = GetClientID(eventParams.client);
        RemoveClient(eventParams.client);
    }

    public void OnDataReceived(ORTCPEventParams eventParams)
    {
        if(verbose)
            print("[TCPServer] OnDataReceived: "+ eventParams.message);
        eventParams.clientID = GetClientID(eventParams.client);

        if (OnTCPMessageReceived != null)
            OnTCPMessageReceived(eventParams);
    }

    public void StartListening()
    {
        StartListening(port);
    }

    public void StartListening(int portNumber)
    {
        if(verbose)
            print("[TCPServer] StartListening on port: " + portNumber);

        if (isListening)
            return;

        this.port = portNumber;
        isListening = true;
        newConnections.Clear();

        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        AcceptClient();
    }

    public void StopListening()
    {
        isListening = false;

        if (tcpListener == null)
            return;
        tcpListener.Stop();
        tcpListener = null;
    }

    public void DisconnectAllClients()
    {
        if(verbose)
            print("[TCPServer] DisconnectAllClients");
        foreach(KeyValuePair<int, ORTCPClient> entry in clients)
            entry.Value.Disconnect();

        clients.Clear();
    }

    public void SendAllClientsMessage(string message)
    {
        if(verbose)
            print("[TCPServer] SendAllClientsMessage: " + message);

        foreach(KeyValuePair<int, ORTCPClient> entry in clients)
            entry.Value.Send(message);
    }

    public void DisconnectClientWithID(int clientID)
    {
        if (verbose)
            print("[TCPServer] DisconnectClientWithID: " + clientID);

        ORTCPClient client = GetClient(clientID);
        if (client == null)
            return;
        client.Disconnect();
    }

    public void SendClientWithIDMessage(int clientID, string message)
    {
        if(verbose)
            print("[TCPServer] SednClientWithIDMessage: " + clientID + " " + message);
        ORTCPClient client = GetClient(clientID);
        if (client == null)
            return;
        client.Send(message);
    }
}
