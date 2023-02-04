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

        internal static void StartReceiving(ref Socket socket)
        {
            byte[] buffer = new byte[1024];

            // Create new endpoint that will represent the IP address of the sender
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, result =>
            {
                // Pass extra parameters to callback
                ReceiveCallback(result, buffer);
            }, socket);
        }

        private static void ReceiveCallback(IAsyncResult result, byte[] data)
        {
            Socket socket = (Socket)result.AsyncState;

            // Create new endpoint that will represent the IP address of the sender
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            
            int bytesReceived = socket.EndReceiveFrom(result, ref remoteEP);

            IPEndPoint remoteIP = (IPEndPoint)remoteEP;

            Console.WriteLine($"{bytesReceived} bytes received from IP: {remoteIP.Address}");

            // Check if packet belongs to game by checking IP address against current connections, or checking if the protocol ID is a match

            // If packet does not belong to game, discard it and restart receive

            // IF packet belongs to game, add to packetqueue and restart receive

            StartReceiving(ref socket);
        }

        internal static void ReceivePacket(ref Socket socket)
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

        private static void SendCallback(IAsyncResult result, IPEndPoint remoteEP)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int bytesSent = socket.EndSend(result);
                Console.WriteLine("Sent {0} bytes to {1}", bytesSent, remoteEP.Address);

            } catch (Exception e)
            {
                Console.WriteLine($"Error sending packet: {e}");
            }
        }
    }
}