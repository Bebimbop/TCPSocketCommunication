using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public enum ORTCPServerState {
	Listening,
	Connected,
	Disconnected
}

public enum ORTCPServerStartListen {
	DontListen,
	Awake,
	Start
}

public class ORTCPServer : MonoBehaviour {
	
	// Static
	
	public static string DefaultORTCPServerName 		= "ORTCPServer";
	public static ORTCPSocketType DefaultSocketType		= ORTCPSocketType.Text;
	public static int DefaultBufferSize					= 1024;
	public static bool DefaultAotListenOnDisconnect		= true;
	public static int DefaultPort						= 1933;
	public static string DefaultOnConnectMessage		= "OnServerConnect";
	public static string DefaultOnDisconnectMessage		= "OnServerDisconnect";
	public static string DefaultOnDataReceivedMessage	= "OnDataReceived";

	public static ORTCPServer CreateInstance() {
		return CreateInstance(DefaultORTCPServerName);
	}
	
	public static ORTCPServer CreateInstance(string name) {
		
		GameObject go = new GameObject(name);
		ORTCPServer server = go.AddComponent<ORTCPServer>();
		
		return server;
		
	}
	
	// MonoBehaviour
	
	public ORTCPServerStartListen listenOn	= ORTCPServerStartListen.Start;
	public bool autoListenOnDisconnect		= DefaultAotListenOnDisconnect;
	public int port							= DefaultPort;
	public ORTCPSocketType socketType		= DefaultSocketType;
	public int bufferSize					= DefaultBufferSize;
	public GameObject[] listeners			= null;
	public string onConnectMessage			= DefaultOnConnectMessage;
	public string onDisconnectMessage		= DefaultOnDisconnectMessage;
	public string onDataReceivedMessage		= DefaultOnDataReceivedMessage;
	
	private ORTCPServerState _state;
	private NetworkStream _stream;
	private StreamWriter _writer;
	private StreamReader _reader;
	private Thread _readThread;
	private TcpListener _tcpListener;
	private TcpClient _client;
	private Queue<ORTCPEventType> _events;
	private Queue<string> _messages;
	private Queue<ORSocketPacket> _packets;
	
	public bool isConnected {
		get { return _state == ORTCPServerState.Connected; }
	}
	
	public ORTCPServerState state {
		get { return _state; }
	}
	
	public TcpClient client {
		get { return _client; }
	}

	private void Awake() {

		_state = ORTCPServerState.Disconnected;
		_events = new Queue<ORTCPEventType>();
		_messages = new Queue<string>();
		_packets = new Queue<ORSocketPacket>();
		
		if (listenOn == ORTCPServerStartListen.Awake)
			StartListening(port, listeners);
		
	}
	
	private void Start() {
		
		if (listenOn == ORTCPServerStartListen.Start)
			StartListening(port, listeners);		
	}
	
	private void Update() {

		while (_events.Count > 0) {
			
			ORTCPEventType eventType = _events.Dequeue();
			
			ORTCPEventParams eventParams = new ORTCPEventParams();
			eventParams.eventType = eventType;
			eventParams.server = this;
			eventParams.socket = _client;
			
			if (eventType == ORTCPEventType.Connected) {
				print("[TCPServer] New client connected");
				foreach (GameObject listener in listeners)
					listener.SendMessage(onConnectMessage, eventParams, SendMessageOptions.DontRequireReceiver);
				
			} else if (eventType == ORTCPEventType.Disconnected) {
				
				_reader.Close();
				_writer.Close();
				_client.Close();
				print("[TCPServer] Server Disconnected");
				foreach (GameObject listener in listeners)
					listener.SendMessage(onDisconnectMessage, eventParams, SendMessageOptions.DontRequireReceiver);
				
				if (autoListenOnDisconnect)
					StartListening();
				
			} else if (eventType == ORTCPEventType.DataReceived) {
				if (socketType == ORTCPSocketType.Text) {
					
					eventParams.message = _messages.Dequeue();
					print("[TCPServer] Server DataReceived: " + eventParams.message);
					
				} else {
					
					eventParams.packet = _packets.Dequeue();
					
				}
				
				foreach (GameObject listener in listeners)
					listener.SendMessage(onDataReceivedMessage, eventParams, SendMessageOptions.DontRequireReceiver);
				
			}
			
		}
		
	}
	
	
	private void OnDestroy() {
		
		Disconnect();
		
		StopListening();
		
	}
	
	private void OnApplicationQuit() {
		
		Disconnect();

		StopListening();
		
	}
	
	// Private

	private void AcceptTcpClientCallback(IAsyncResult ar) {
		
	    TcpListener tcpListener = (TcpListener)ar.AsyncState;
		
		_client = tcpListener.EndAcceptTcpClient(ar);
		
		_stream = _client.GetStream();
		_reader = new StreamReader(_stream);
		_writer = new StreamWriter(_stream);
		 
		_state = ORTCPServerState.Connected;
		
		_readThread = new Thread(ReadData);
		_readThread.IsBackground = true;
		_readThread.Start();
		
		_events.Enqueue(ORTCPEventType.Connected);
		
	}
	
	private void ReadData() {

		bool endOfStream = false;

		while (!endOfStream) {
			
			if (socketType == ORTCPSocketType.Text) {
 			
				String response = null;
	
				try { response = _reader.ReadLine(); } catch (Exception e) { e.ToString(); }
				
				if (response != null) {
	
					_events.Enqueue(ORTCPEventType.DataReceived);
						
					_messages.Enqueue(response);
	
				} else {
				
					endOfStream = true;
					
				}
				
			} else if (socketType == ORTCPSocketType.Binary) {
				
				byte[] bytes = new byte[bufferSize];
				
				int bytesRead = _stream.Read(bytes, 0, bufferSize);
				
				if (bytesRead == 0) {
					
					endOfStream = true;
					
				} else {
					
					_events.Enqueue(ORTCPEventType.DataReceived);
					
					_packets.Enqueue(new ORSocketPacket(bytes, bytesRead));
					
				}
				
			}
			
		}
		
		_state = ORTCPServerState.Disconnected;
		
		_tcpListener.Stop();
		
		_events.Enqueue(ORTCPEventType.Disconnected);
		
	}
	
	// Public
	
	public void StartListening() {
		StartListening(port, listeners);
	}
	
	public void StartListening(int port) {
		StartListening(port, listeners);
	}
	
	public void StartListening(int port, GameObject[] listeners) {
		print("[TCPServer] Start Listening: "+port);
		if (_state == ORTCPServerState.Listening)
			return;

		this.port = port;
		this.listeners = listeners;
		
		_state = ORTCPServerState.Listening;
		
		_messages.Clear();
		_events.Clear();
		
		_tcpListener = new TcpListener(IPAddress.Any, port);
		_tcpListener.Start();
		_tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), _tcpListener);
		
	}
	
	public void StopListening() {
		
		_state = ORTCPServerState.Disconnected;
		
		if (_tcpListener == null)
			return;
		
		_tcpListener.Stop();
		_tcpListener = null;
		
	}
	
	public void Disconnect() {
		print("[TCPServer] Server Disconnecting");
		_state = ORTCPServerState.Disconnected;

		try { if (_reader != null) _reader.Close(); } catch (Exception e) { e.ToString(); }
		try { if (_writer != null) _writer.Close(); } catch (Exception e) { e.ToString(); }
		try { if (_client != null) _client.Close(); } catch (Exception e) { e.ToString(); }
	
	}

	public void Send(string message) {
		print("[TCPServer] sending: "+ message);
		if (!isConnected)
			return;
		
		_writer.WriteLine(message);
		_writer.Flush();
		
	}
	
	public void SendBytes(byte[] bytes) {
		SendBytes(bytes, 0, bytes.Length);
	}
	
	public void SendBytes(byte[] bytes, int offset, int size) {
		
		if (!isConnected)
			return;
		
		_stream.Write(bytes, offset, size);
		_stream.Flush();

	}

}
