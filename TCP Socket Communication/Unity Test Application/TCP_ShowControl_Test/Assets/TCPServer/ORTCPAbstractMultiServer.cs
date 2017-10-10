using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Runtime.Remoting.Services;

public class ORTCPAbstractMultiServer : MonoBehaviour
{
    protected class NewConnection
    {
        public TcpClient tcpClient;

        public NewConnection(TcpClient client)
        {
            tcpClient = client;
        }
    }

    protected int clientID;
    protected Dictionary<int, ORTCPClient> clients;
    protected TcpListener tcpListener;
    protected Queue<NewConnection> newConnections;
    protected bool isListening;

    public int ClientCount
    {
        get { return clients.Count; }
    }

    public bool Listening
    {
        get { return isListening; }
    }

    protected int SaveClient(ORTCPClient clientToSave)
    {
        int currentClientID = clientID;
        clients.Add(currentClientID, clientToSave);
        clientID++;
        return currentClientID;
    }

    protected int RemoveClient(int clientIdToRemove)
    {
        ORTCPClient client = GetClient(clientIdToRemove);
        if (client == null)
            return clientIdToRemove;

        client.Disconnect();
        clients.Remove(clientIdToRemove);
        Destroy(client.gameObject);
        return clientIdToRemove;
    }

    protected int RemoveClient(ORTCPClient clientToRemove)
    {
        int clientId = GetClientID(clientToRemove);
        if (clientID < 0)
        {
            Destroy(clientToRemove.gameObject);
            return -1;
        }
        return RemoveClient(clientId);
    }

    protected TcpClient GetTcpClient(int clientIdToGet)
    {
        ORTCPClient client = null;
        if (!clients.TryGetValue(clientIdToGet, out client))
            return null;
        return client.tcpClient;
    }

    protected ORTCPClient GetClient(int clientIdToGet)
    {
        ORTCPClient client = null;
        if (clients.TryGetValue(clientIdToGet, out client))
            return client;
        return null;
    }

    protected int GetClientID(ORTCPClient clientToGet)
    {
        foreach(KeyValuePair<int, ORTCPClient> entry in clients)
            if (entry.Value == clientToGet)
                return entry.Key;
        return -1;
    }

    protected int GetclientID(TcpClient tcpClientToGet)
    {
        foreach(KeyValuePair<int, ORTCPClient> entry in clients)
            if (entry.Value.tcpClient == tcpClientToGet)
                return entry.Key;
        return -1;
    }

    protected void AcceptClient()
    {
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), tcpListener);
    }

    protected void AcceptTcpClientCallback(IAsyncResult asyncResult)
    {
        TcpListener _tcpListener = (TcpListener) asyncResult.AsyncState;
        TcpClient _tcpClient = tcpListener.EndAcceptTcpClient(asyncResult);

        if (_tcpListener != null && _tcpClient.Connected)
        {
            newConnections.Enqueue(new NewConnection(_tcpClient));
            AcceptClient();
        }
    }
}