using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using TCP_Socket_Communication;

public class ORTCPMultiServer : ORTCPAbstractMultiServer 
{
	public delegate void TCPServerMessageRecivedEvent(ORTCPEventParams eventParams);
	public event TCPServerMessageRecivedEvent OnTCPMessageRecived;
	
	public bool verbose = true;
	public int port = 1933;
	
	
	private static ORTCPMultiServer _instance;
	public static ORTCPMultiServer Instance { get { return _instance; } }

	public ORTCPMultiServer()
	{
		_instance = this;
	}
	
	public void Start(int port = 0) 
	{
		_listenning = false;
		_newConnections = new Queue<NewConnection>();
		_clients = new Dictionary<int, ORTCPClient>();
		
		if(port != 0)StartListening(port);
		else StartListening();

		Observable
			.Interval(TimeSpan.FromSeconds(1))
			.Where(_ => _newConnections.Count > 0)
			.Subscribe(_ =>
			{
				//Debug.Log(Thread.CurrentThread.ManagedThreadId);
				NewConnection newConnection = _newConnections.Dequeue();
				ORTCPClient client = ORTCPClient.CreateInstance("ORMultiServerClient", newConnection.tcpClient, this);

				int clientID = SaveClient(client);
				ORTCPEventParams eventParams = new ORTCPEventParams();
				eventParams.eventType = ORTCPEventType.Connected;
				eventParams.client = client;
				eventParams.clientID = clientID;
				eventParams.socket = newConnection.tcpClient;
				Console.WriteLine("[TCPServer] New client connected");
			});
	}
	

	private void OnDestroy() 
	{
		DisconnectAllClients();
		StopListening();
	}
	
	private void OnApplicationQuit() 
	{	
		DisconnectAllClients();
		StopListening();
	}
	
	
	//Delegation methods. The clients call these 
	public void OnServerConnect(ORTCPEventParams eventParams) 
	{
		//if(verbose)print("[TCPServer] OnServerConnect");
	}
	
	public void OnClientDisconnect(ORTCPEventParams eventParams) 
	{
		Console.WriteLine("[TCPServer] OnClientDisconnect");
		eventParams.clientID = GetClientID(eventParams.client);
		RemoveClient(eventParams.client);
	}
	
	public void OnDataReceived(ORTCPEventParams eventParams) 
	{
		Console.WriteLine("[TCPServer] OnDataReceived: " + eventParams.message);	
		eventParams.clientID = GetClientID(eventParams.client);
		if(OnTCPMessageRecived!=null)
			OnTCPMessageRecived(eventParams);
	}
	//---
	
	
	public void StartListening() 
	{
		StartListening(port);
	}
	
	
	public void StartListening(int port) 
	{
		Console.WriteLine("[TCPServer] StartListening on port: "+port);
		if (_listenning)
			return;

		this.port = port;
		_listenning = true;
		_newConnections.Clear();
		
		_tcpListener = new TcpListener(IPAddress.Any, port);
		_tcpListener.Start();
		AcceptClient();
	}

	public void StopListening() 
	{
		_listenning = false;
		if (_tcpListener == null)
			return;
		_tcpListener.Stop();
		_tcpListener = null;
	}
	
	public void DisconnectAllClients() 
	{
		Console.WriteLine("[TCPServer] DisconnectAllClients");
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			entry.Value.Disconnect();
		_clients.Clear();
	}

	public void SendAllClientsMessage(string message) 
	{
		Console.WriteLine("[TCPServer] SendAllClientsMessage: "+message);
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			entry.Value.Send(message);
	}
	
	public void DisconnectClientWithID(int clientID) 
	{
		
		Console.WriteLine("[TCPServer] DisconnectClientWithID: "+clientID);
		ORTCPClient client = GetClient(clientID);
		if (client == null)
			return;
		client.Disconnect();
	}
	
	public void SendClientWithIDMessage(int clientID, string message) 
	{
		Console.WriteLine("[TCPServer] SendClientWithIDMessage: "+clientID+". "+message);
		ORTCPClient client = GetClient(clientID);

		if (client == null)
			return;



		client.Send(message);
	}
	
}
