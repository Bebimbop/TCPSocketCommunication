using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UniRx;

public class ORTCPMultiServer : ORTCPAbstractMultiServer 
{
	public delegate void TCPServerMessageRecivedEvent(ORTCPEventParams eventParams);
	public event TCPServerMessageRecivedEvent OnTCPMessageRecived;
	
	public bool verbose = true;
	public int port = 1933;
	
	
	private static ORTCPMultiServer _instance;
	public static ORTCPMultiServer Instance { get { return _instance; } }
	
	private void Awake()
	{
		if (_instance != null && _instance != this)
			Destroy(this.gameObject);
		else
			_instance = this;
	}
	
	private void Start() 
	{
		_listenning = false;
		_newConnections = new Queue<NewConnection>();
		_clients = new Dictionary<int, ORTCPClient>();
		StartListening();

		Observable
			.EveryUpdate()
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
				if (verbose) print("[TCPServer] New client connected");
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
		if(verbose)print("[TCPServer] OnClientDisconnect");
		eventParams.clientID = GetClientID(eventParams.client);
		RemoveClient(eventParams.client);
	}
	
	public void OnDataReceived(ORTCPEventParams eventParams) 
	{
		if(verbose)print("[TCPServer] OnDataReceived: " + eventParams.message);	
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
		if(verbose)print("[TCPServer] StartListening on port: "+port);
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
		if(verbose)print("[TCPServer] DisconnectAllClients");
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			entry.Value.Disconnect();
		_clients.Clear();
	}

	public void SendAllClientsMessage(string message) 
	{
		if(verbose)print("[TCPServer] SendAllClientsMessage: "+message);
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			entry.Value.Send(message);

//		string s = JSONString.Escape(message);
//		Debug.Log(s);
	}
	
	public void DisconnectClientWithID(int clientID) 
	{
		if(verbose)print("[TCPServer] DisconnectClientWithID: "+clientID);
		ORTCPClient client = GetClient(clientID);
		if (client == null)
			return;
		client.Disconnect();
	}
	
	public void SendClientWithIDMessage(int clientID, string message) 
	{
		if(verbose)print("[TCPServer] SendClientWithIDMessage: "+clientID+". "+message);
		ORTCPClient client = GetClient(clientID);

		if (client == null)
			return;



		client.Send(message);
	}
	
}
