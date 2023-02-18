using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal class PacketManager
    {
        List<Packet> packetQueue;
        NetworkManager networkManager;

        public PacketManager(NetworkManager networkManager)
        {
            packetQueue = new List<Packet>();
            this.networkManager = networkManager;
        }

        internal static void ProcessPacket(Packet packet)
        {
            // Process the byte array of packet in packetQueue
        }

        internal void StartReceiving(ref Socket socket, NetworkManager networkManager)
        {
            byte[] buffer = new byte[1024];

            // Create new endpoint that will represent the IP address of the sender
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, result =>
            {
                // Pass extra parameters to callback
                ReceiveCallback(result, buffer, networkManager);
            }, socket);
        }

        private void ReceiveCallback(IAsyncResult result, byte[] data, NetworkManager networkManager)
        {
            Socket socket = (Socket)result.AsyncState;

            // Create new endpoint that will represent the IP address of the sender
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            
            int bytesReceived = socket.EndReceiveFrom(result, ref remoteEP);

            IPEndPoint remoteIP = (IPEndPoint)remoteEP;

            Debug.WriteLine($"{bytesReceived} bytes received from IP: {remoteIP.Address}");

            // Check if packet belongs to game by checking IP address against current connections, or checking if the protocol ID is a match
            List<string> addresses = networkManager.GetConnectedAddresses();
            string output = Encoding.ASCII.GetString(data);
            string[] split = output.Split('/');
            int protocolID = int.Parse(split[0]);

            if (addresses.Contains(remoteIP.Address.ToString()))
            {
                // Packet belongs to game
                ConstructPacketFromByteArray(data, remoteIP.Address.ToString(), remoteIP.Port);
            }
            else if (protocolID == networkManager.ProtocolID)
            {
                // Packet belongs to game
                ConstructPacketFromByteArray(data, remoteIP.Address.ToString(), remoteIP.Port);
            }

            StartReceiving(ref socket, networkManager);
        }

        internal void ReceivePacket(ref Socket socket)
        {
            // Constructs a custom packet object based on the byte array it received and adds it to the packetqueue
        }

        internal void SendPacket(Packet packet, ref Socket socket)
        {
            // Send a packet to it's destination
            IPEndPoint destination = new IPEndPoint(IPAddress.Parse(packet.IPDestination), packet.PortDestination);

            socket.BeginSendTo(packet.CompressedData, 0, packet.CompressedData.Length, SocketFlags.None, destination, result =>
            {
                // Pass extra parameters to callback
                SendCallback(result, destination);
            }, socket);
        }

        private void SendCallback(IAsyncResult result, IPEndPoint remoteEP)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int bytesSent = socket.EndSend(result);
                Debug.WriteLine("Sent {0} bytes to {1}", bytesSent, remoteEP.Address);

            } catch (Exception e)
            {
                Debug.WriteLine($"Error sending packet: {e}");
            }
        }

        private void ConstructPacketFromByteArray(byte[] data, string sourceIP, int sourcePort)
        {
            string payload = Encoding.ASCII.GetString(data, 0, data.Length);

            string[] split = payload.Split('/');

            PacketType packetType;

            if (split[1] == "REQUEST")
            {
                // Connection packet

                // Begin establishing connection
                Packet packet = new Packet(PacketType.CONNECT, sourceIP, sourcePort, data);
                networkManager.HandleConnectionRequest(packet);
            }
            else if (split[1] == "ACCEPT")
            {
                // Connection accept packet

                // Create accept packet and pass to network manager
                Packet packet = new Packet(PacketType.ACCEPT, sourceIP, sourcePort, data);
                networkManager.HandleConnectionAccept(packet);
            }
            else if (split[1] == "SYNC")
            {
                // Synchronisation packet

                // Create sync packet and pass to network manager
                int localSequence = int.Parse(split[2]);
                int remoteSequence = int.Parse(split[3]);
                AckBitfield ackBitfield = (AckBitfield)Enum.Parse(typeof(AckBitfield), split[4]);

                Packet packet = new Packet(PacketType.SYNC, localSequence, remoteSequence, ackBitfield, sourceIP, sourcePort, data);
                networkManager.ProcessSyncPacket(packet);
            }
        }
    }
}