using System.Net.Sockets;

public enum ORTCPEventType
{
    None,
    Connected,
    Disconnected,
    ConnectionRefused,
    DataReceived
}

public enum ORTCPSocketType
{
    Binary,
    Text
}

public class ORSocketPacket
{
    public byte[] bytes = null;
    public int bytescount = 0;

    public ORSocketPacket(byte[] bytes, int bytesCount)
    {
        this.bytes = bytes;
        this.bytescount = bytesCount;
    }
}

public class ORTCPEventParams
{
    public ORTCPServer server = null;
    public ORTCPClient client = null;
    public int clientID = 0;
    public TcpClient socket = null;
    public ORTCPEventType eventType = ORTCPEventType.None;
    public string message = "";
    public ORSocketPacket packet = null;
}