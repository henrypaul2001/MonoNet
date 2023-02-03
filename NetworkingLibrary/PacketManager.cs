using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal static class PacketManager
    {
        static List<Packet> packetQueue = new List<Packet>();

        internal static void ProcessPacket(Packet packet)
        {
            // Process the byte array of packet in packetQueue
        }

        internal static void ReceivePacket()
        {
            // Constructs a custom packet object based on the byte array it received and adds it to the packetqueue
        }

        internal static void SendPacket(Packet packet, ref Socket socket)
        {
            // Send a packet to it's destination
            IPEndPoint destination = new IPEndPoint(IPAddress.Parse(packet.IPDestination), packet.Port);

            socket.BeginSendTo(packet.CompressedData, 0, packet.CompressedData.Length, SocketFlags.None, destination, result =>
            {
                // Pass extra parameters to callback
                SendCallback(result, destination);
            }, socket);
        }

        private static void SendCallback(IAsyncResult ar, IPEndPoint remoteEP)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int bytesSent = socket.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to {1}", bytesSent, remoteEP.Address);

            } catch (Exception e)
            {
                Console.WriteLine($"Error sending packet: {e}");
            }
        }
    }
}