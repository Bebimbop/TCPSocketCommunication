using System;
using System.Net.Sockets;

namespace TCP_Socket_Communication
{
    public enum ORTCPEventType
    {
        NONE,
        CONNECTED,
        DISCONNECTED,
        CONNECTIONREFUSED,
        DATARECEIVED
    }

    public enum ORTCPSocketType
    {
        BINARY,
        TEXT
    }

    public class ORTCPSocketPacket
    {
        public byte[] bytes = null;
        public int bytescount = 0;

        public ORTCPSocketPacket(byte[] bytes, int bytesCount)
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
        public ORTCPEventType eventType = ORTCPEventType.NONE;
        public string message = "";
        public ORTCPSocketPacket packet = null;
    }
}
