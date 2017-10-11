using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

public class ORTCPAbstractMultiServer : MonoBehaviour {
	
	protected class NewConnection 
	{	
		public TcpClient tcpClient;
		public NewConnection(TcpClient tcpClient) 
		{
			this.tcpClient = tcpClient;
		}
	}

	
	protected int ClientID = 0;
	protected Dictionary<int, ORTCPClient> _clients;
	protected TcpListener _tcpListener;
	protected Queue<NewConnection> _newConnections;
	protected bool _listenning;
	
	public int clientsCount 
	{
		get { return _clients.Count; }
	}
	
	public bool listenning 
	{
		get { return _listenning; }
	}
	
	
	protected int SaveClient(ORTCPClient client) 
	{	
		int currentClientID = ClientID;
		_clients.Add(currentClientID, client);
		ClientID++;
		return currentClientID;
	}
	
	protected int RemoveClient(int clientID) 
	{	
		ORTCPClient client = GetClient(clientID);
		if (client == null)
			return clientID;
		client.Disconnect();
		_clients.Remove(clientID);
		Destroy(client.gameObject);		
		return clientID;
	}
	
	protected int RemoveClient(ORTCPClient client) 
	{		
		int clientID = GetClientID(client);
		if (clientID < 0) 
		{
			Destroy(client.gameObject);
			return -1;
		}
		return RemoveClient(clientID);
	}
	
	
	protected TcpClient GetTcpClient(int clientID) 
	{
		ORTCPClient client = null;
		if (!_clients.TryGetValue(clientID, out client))
			return null;
		return client.tcpClient;
	}
	
	protected ORTCPClient GetClient(int clientID) 
	{
		ORTCPClient client = null;
		if (_clients.TryGetValue(clientID, out client))
		    return client;
		return null;
	}
	
	protected int GetClientID(ORTCPClient client) 
	{
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			if (entry.Value == client)
				return entry.Key;
		return -1;
	}
	
	protected int GetClientID(TcpClient tcpClient) 
	{
		foreach (KeyValuePair<int, ORTCPClient> entry in _clients)
			if (entry.Value.tcpClient == tcpClient)
				return entry.Key;
		return -1;
	}
	
	protected void AcceptClient() {
		_tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), _tcpListener);
	}

	protected void AcceptTcpClientCallback(IAsyncResult ar) 
	{	
	    TcpListener tcpListener = (TcpListener)ar.AsyncState;
		TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
		if (tcpListener != null && tcpClient.Connected) 
		{
			_newConnections.Enqueue(new NewConnection(tcpClient));
			AcceptClient();
		}
	}
	
	
	
}
