using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using TCP_Socket_Communication;

public enum ORTCPClientState {
	Connecting,
	Connected,
	Disconnected,

}

public enum ORTCPClientStartConnection 
{
	DontConnect,
	Awake,
	Start
}

public class ORTCPClient 
{
	public delegate void TCPServerMessageRecivedEvent(ORTCPEventParams eventParams);
	public event TCPServerMessageRecivedEvent OnTCPMessageRecived;

    internal int laneID = 0;
	public bool verbose = true;
	//public inAppStatus AppStatus = inAppStatus.Done;
	private bool autoConnectOnDisconnect			= true;
	private float disconnectTryInterval				= 3;
	private bool autoConnectOnConnectionRefused		= true;
	private float connectionRefusedTryInterval		= 3;
	private string hostname							= "127.0.0.1";
	private int port								= 1983;
	private ORTCPSocketType socketType				= ORTCPSocketType.Text;
	private int bufferSize							= 1024;
	
	
	public ORTCPClientState _state = ORTCPClientState.Disconnected;
	private NetworkStream _stream;
	private StreamWriter _writer;
	private StreamReader _reader;
	private Thread _readThread;
	private TcpClient _client;
	private Queue<ORTCPEventType> _events = new Queue<ORTCPEventType>();
	private Queue<string> _messages 	= new Queue<string>();
	private Queue<ORSocketPacket> _packets = new Queue<ORSocketPacket>();
	
	private ORTCPMultiServer serverDelegate; 
	
	public bool isConnected {
		get { return _state == ORTCPClientState.Connected; }
	}
	
	public ORTCPClientState state {
		get { return _state; }
	}
	
	public TcpClient client {
		get { return _client; }
	}
	
	public TcpClient tcpClient {
		get { return _client; }
	}
	
	public static ORTCPClient CreateInstance(string name, TcpClient tcpClient, ORTCPMultiServer serverDelegate, int port) // this is only used by the server
	{
		ORTCPClient client = new ORTCPClient();
		client.port = port;
		client.SetTcpClient(tcpClient);
		client.serverDelegate = serverDelegate;
		client.verbose = false;
		client.Start();
		return client;
	}


	public void Start () 
	{	
		Connect();
		Observable
			.Interval(TimeSpan.FromSeconds(1))
			.Where(x => _events.Count > 0)
			.Subscribe(x =>
			{
				ORTCPEventType eventType = _events.Dequeue();
			
				ORTCPEventParams eventParams = new ORTCPEventParams();
				eventParams.eventType = eventType;
				eventParams.client = this;
				eventParams.socket = _client;
			
				if (eventType == ORTCPEventType.Connected) 
				{
					//Console.WriteLine("[TCPClient] Connnected to server");
					if(serverDelegate!=null)serverDelegate.OnServerConnect(eventParams);
				} 
				else if (eventType == ORTCPEventType.Disconnected) 
				{
					//Console.WriteLine("[TCPClient] Disconnnected from server");
					if(serverDelegate!=null)serverDelegate.OnClientDisconnect(eventParams);
				
					_reader.Close();
					_writer.Close();
					_client.Close();
				
				} 
				else if (eventType == ORTCPEventType.DataReceived) 
				{
					if (socketType == ORTCPSocketType.Text) 
					{
						eventParams.message = _messages.Dequeue();
					//	Console.WriteLine("[TCPClient] DataReceived: "+ eventParams.message);

						if (OnTCPMessageRecived != null)
							OnTCPMessageRecived (eventParams);
					} 
					else 
						eventParams.packet = _packets.Dequeue();
					if(serverDelegate!=null)serverDelegate.OnDataReceived(eventParams);
				} 
				else if (eventType == ORTCPEventType.ConnectionRefused) 
				{
					//Console.WriteLine("[TCPClient] ConnectionRefused... will try again...");
				}
			});
	}
	

	
	
	private void ConnectCallback(IAsyncResult ar) 
	{	
        try {
	    	TcpClient tcpClient = (TcpClient)ar.AsyncState;
			tcpClient.EndConnect(ar);
			SetTcpClient(tcpClient);
        } catch (Exception e) {
			_events.Enqueue(ORTCPEventType.ConnectionRefused);
			//Console.WriteLine("Connect Exception: " + e.Message);
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
				try { response = _reader.ReadLine(); } catch (Exception e) { e.ToString(); }
				
				if (response != null) 
				{
					response = response.Replace(Environment.NewLine, "");
					_events.Enqueue(ORTCPEventType.DataReceived);
					_messages.Enqueue(response);
				} 
				else 
					endOfStream = true;
				
				
			} 
			else if (socketType == ORTCPSocketType.Binary) 
			{
				byte[] bytes = new byte[bufferSize];
				int bytesRead = _stream.Read(bytes, 0, bufferSize);
				if (bytesRead == 0) 
					endOfStream = true;
				else 
				{
					_events.Enqueue(ORTCPEventType.DataReceived);
					_packets.Enqueue(new ORSocketPacket(bytes, bytesRead));
				}
			}
		}
		
		_state = ORTCPClientState.Disconnected;
		_client.Close();
		_events.Enqueue(ORTCPEventType.Disconnected);
		
	}
		
	
	
	public void Connect() {
		Connect(hostname, port);
	}
	
	public void Connect(string hostname, int port) 
	{
		//Console.WriteLine("[TCPClient] trying to connect to "+hostname+" "+port);
		if (_state == ORTCPClientState.Connected)
			return;
		
		this.hostname = hostname;
		this.port = port;
		_state = ORTCPClientState.Connecting;
		_messages.Clear();
		_events.Clear();
		_client = new TcpClient();
		_client.BeginConnect(hostname,
		                     port,
		                     new AsyncCallback(ConnectCallback),
		                     _client);
	}
	
	public void Disconnect() 
	{
		_state = ORTCPClientState.Disconnected;
		try { if (_reader != null) _reader.Close(); } catch (Exception e) { e.ToString(); }
		try { if (_writer != null) _writer.Close(); } catch (Exception e) { e.ToString(); }
		try { if (_client != null) _client.Close(); } catch (Exception e) { e.ToString(); }
	}

	public void Send(string message) 
	{	
		//Console.WriteLine("[TCPClient] sending message: "+message);
		if (!isConnected)
			return;
		_writer.WriteLine(message);
		_writer.Flush();
	}
	
	public void SendBytes(byte[] bytes) 
	{
		SendBytes(bytes, 0, bytes.Length);
	}
	
	public void SendBytes(byte[] bytes, int offset, int size) 
	{	
		if (!isConnected)
			return;
		_stream.Write(bytes, offset, size);
		_stream.Flush();
	}
	
	private void SetTcpClient(TcpClient tcpClient) 
	{	
		_client = tcpClient;
		if (_client.Connected) 
		{
			_stream = _client.GetStream();
			_reader = new StreamReader(_stream);
			_writer = new StreamWriter(_stream);
			_state = ORTCPClientState.Connected;
			_events.Enqueue(ORTCPEventType.Connected);
			_readThread = new Thread(ReadData);
			_readThread.IsBackground = true;
			_readThread.Start();
		} 
		else 
			_state = ORTCPClientState.Disconnected;
	}

}
