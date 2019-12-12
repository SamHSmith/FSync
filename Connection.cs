using System.Threading;
using System.Net.Sockets;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using System.Text;

namespace FSync{
    public class Connection{
        private Socket socket;
        public bool Running{get; private set;}

        public static int MaxPacketSize=1024;

        public ConcurrentBag<byte[]> dataSends=new ConcurrentBag<byte[]>();

        public Connection(Socket s){
            this.socket=s;
            Running=true;
            Task.Run(()=>Read());
            Task.Run(()=>Write());
        }

        private void Read(){
            while(Running){
                byte[] inbuf = new byte[Connection.MaxPacketSize];
                int byteCount=socket.Receive(inbuf);
                if(inbuf[0]==(byte)PacketType.Ping){
                    Ping(inbuf.AsSpan(1,byteCount));
                    continue;
                }


                //Do stuff
                //Console.WriteLine(Encoding.UTF8.GetChars(inbuf, 0, inbuf.Length));
                double ping=((double)DateTime.UtcNow.Ticks-BitConverter.ToInt64(inbuf,1))/TimeSpan.TicksPerMillisecond;
                Console.WriteLine(ping);
            }
        }

        private void Write(){
            while(Running){
                if(dataSends.TryTake(out var data)){
                    if(data.Length>MaxPacketSize){
                        Console.Error.WriteLine("PACKET IS OVER MAX SIZE");
                    }
                    socket.Send(data);
                }
            }
        }

        private void Ping(Span<byte> packet){
            byte[] pingPacket=new byte[packet.Length+1];
            pingPacket[0]=(byte)PacketType.PingReturn;
            for (int i = 0; i < packet.Length; i++)
            {
                pingPacket[i+1]=packet[i];
            }
            dataSends.Add(pingPacket);
        }
    }

    public enum PacketType{
        Ping,PingReturn
    }
}