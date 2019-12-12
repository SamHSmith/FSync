using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace FSync
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--server" || args[0] == "-s"))
            {
                DoServer();
                return;
            }
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Socket socket = new Socket(IPAddress.Any.AddressFamily,
                   SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(localEndPoint);
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress remoteIpAddress = ipHost.AddressList[0];
            IPEndPoint remoteEndPoint=new IPEndPoint(remoteIpAddress,12881);

            socket.SendTo(IpToBytes(socket.LocalEndPoint),remoteEndPoint);

            byte[] inBuf = new byte[80];
            int byteCount = socket.Receive(inBuf);
            IPAddress ip = BytesToIp(inBuf, byteCount, out var port);
            Console.WriteLine($"Allocated port:{ip.ToString()}:{port}");
            remoteEndPoint = new IPEndPoint(ip, port);
            socket.Connect(remoteEndPoint);

            Connection c=new Connection(socket);

            while (true)
            {
                byte[] packet=new byte[8+1];
                packet[0]=(byte)PacketType.Ping;
                Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.Ticks),0,packet,1,8);
                c.dataSends.Add(packet);
                Thread.Sleep(1);
            }
        }

        static byte[] IpToBytes(EndPoint endPoint)
        {
            return Encoding.UTF8.GetBytes(endPoint.ToString());
        }

        static IPAddress BytesToIp(byte[] data, int length, out ushort port)
        {
            string[] adress = new string(Encoding.UTF8.GetChars(data, 0, length)).Split(":");
            IPAddress ip = IPAddress.Parse(adress[0]);
            port = ushort.Parse(adress[1]);
            return ip;
        }

        static bool shouldStop = false;
        static void DoServer()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 12881);
            Console.WriteLine($"Hello Clients! My name is Server@{localEndPoint.Address}:{localEndPoint.Port}");
            Socket listen = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            listen.Bind(localEndPoint);

            while (!shouldStop)
            {
                try
                {
                    byte[] inBuf = new byte[80];
                    int byteCount = listen.Receive(inBuf);
                    IPAddress ip = BytesToIp(inBuf, byteCount, out var port);
                    Task.Run(() => TalkToClientSocket(ip, port));
                }
                catch
                {
                    Console.WriteLine("Regected incomming connection");
                }
            }
        }
        private static List<Connection> clientConnections=new List<Connection>();
        static void TalkToClientSocket(IPAddress address, ushort port)
        {
            Console.WriteLine($"Client Connected:{address.ToString()}:{port}");
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Socket socket = new Socket(IPAddress.Any.AddressFamily,
                   SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(localEndPoint);

            IPEndPoint remoteEndPoint = new IPEndPoint(address, port);
            socket.Connect(remoteEndPoint);

            socket.Send(IpToBytes(socket.LocalEndPoint));
            //TODO: packets might drop

            lock(clientConnections){
                clientConnections.Add(new Connection(socket));
            }
            
        }
    }
}
