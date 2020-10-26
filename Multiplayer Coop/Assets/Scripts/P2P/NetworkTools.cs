using System.Net;
using System.Net.Sockets;

namespace P2P
{
    /// <summary>
    /// Storing variables needed for creating connections
    /// </summary>
    public class NetworkTools
    {
        // Get the endpoint on which this client receives data on
        private static IPEndPoint _localEndPoint;
        public static IPEndPoint LocalEndPoint {
            get
            {
                if (_localEndPoint == null) {
                    _localEndPoint = new IPEndPoint(IPAddress.Parse(LocalIP), MultiplayerManager.ListenPort);
                }
                return _localEndPoint;
            }
            set
            {
                _localEndPoint = value;
            }
        }


        // Get public IP from ipinfo.io
        private static string _publicIP;
        public static string PublicIp {
            get
            {
                if (_publicIP == null) {
                    _publicIP = new WebClient().DownloadString("http://ipinfo.io/ip").Trim('\n');
                }
                return _publicIP;
            }
        }

        // Try to make a connection and get the local end point from the socket info
        private static string _localIP;
        public static string LocalIP {
            get {
                if (_localIP == null) {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
                        socket.Connect("mpcoop.duckdns.org", 65530);
                        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                        _localIP = endPoint.Address.ToString();
                    }
                }
                return _localIP;
            } 
        }

        // Create a new udp client on the same port
        public static UdpClient CreateUdpClient() {
            UdpClient client = new UdpClient();
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(LocalEndPoint);
            return client;
        }
    }
}