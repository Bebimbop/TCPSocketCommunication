using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public enum ORTCPEventType {
	None,
	Connected,
	Disconnected,
	ConnectionRefused,
	DataReceived
}

public enum ORTCPSocketType {
	Binary,
	Text
}

public class ORSocketPacket {
	
	public byte[] bytes					= null;
	public int bytesCount				= 0;
	
	public ORSocketPacket(byte[] bytes, int bytesCount) {
		this.bytes = bytes;
		this.bytesCount = bytesCount;
	}
	
}

public class ORTCPEventParams {

	public ORTCPServer server			= null;
	public ORTCPClient client			= null;
	public int clientID					= 0;
	public TcpClient socket				= null;
	public ORTCPEventType eventType		= ORTCPEventType.None;
	public string message				= "";
	public ORSocketPacket packet		= null;
	
}
