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
            try
            {
                Socket socket = (Socket)result.AsyncState;

                // Create new endpoint that will represent the IP address of the sender
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                int bytesReceived = socket.EndReceiveFrom(result, ref remoteEP);

                IPEndPoint remoteIP = (IPEndPoint)remoteEP;

                Debug.WriteLine($"{bytesReceived} bytes received from IP: {remoteIP.Address}", "Packet I/O");

                // Check if packet belongs to game by checking IP address against current connections, or checking if the protocol ID is a match
                List<string> addresses = networkManager.GetConnectedAddresses();
                string output = Encoding.ASCII.GetString(data);
                string[] split = output.Split('/');
                int protocolID = int.Parse(split[1]);

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
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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
                Debug.WriteLine("Sent {0} bytes to {1}", bytesSent, remoteEP.Address, "Packet I/O");

            } catch (Exception e)
            {
                Debug.WriteLine($"Error sending packet: {e}");
            }
        }

        private void ConstructPacketFromByteArray(byte[] data, string sourceIP, int sourcePort)
        {
            string payload = Encoding.ASCII.GetString(data, 0, data.Length);

            string[] split = payload.Split('/');

            Packet packet;
            switch (split[2])
            {
                case "REQUEST":
                    // Connection packet

                    // Begin establishing connection
                    packet = new Packet(PacketType.CONNECT, sourceIP, sourcePort, data);
                    networkManager.HandleConnectionRequest(packet);
                    break;
                case "ACCEPT":
                    // Connection accept packet

                    // Create accept packet and pass to network manager
                    packet = new Packet(PacketType.ACCEPT, sourceIP, sourcePort, data);
                    networkManager.HandleConnectionAccept(packet);
                    break;
                case "SYNC":
                    // Synchronisation packet

                    // Create sync packet and pass to network manager
                    packet = CreateConstructOrSyncPacket(PacketType.SYNC, data, sourceIP, sourcePort);
                    networkManager.ProcessSyncPacket(packet);
                    break;
                case "CONSTRUCT":
                    // Object construction packet (a new local object has been created on a remote client, therefore all clients in session must now create a matching object locally)

                    // Create construct packet and pass to network manager
                    packet = CreateConstructOrSyncPacket(PacketType.CONSTRUCT, data, sourceIP, sourcePort);
                    networkManager.ProcessConstructPacket(packet);
                    break;
                case "DISCONNECT":
                    // Disconnect packet

                    // Create disconnect packet and pass to network manager
                    packet = new Packet(PacketType.DISCONNECT, sourceIP, sourcePort, data);
                    networkManager.HandleDisconnect(packet);
                    break;
            }
        }

        private Packet CreateConstructOrSyncPacket(PacketType packetType, byte[] data, string sourceIP, int sourcePort)
        {
            string payload = Encoding.ASCII.GetString(data, 0, data.Length);

            string[] split = payload.Split('/');

            int localSequence = int.Parse(split[3]);
            int remoteSequence = int.Parse(split[4]);

            int payloadLength = BitConverter.ToInt32(data, 0);

            // Get actual size of incoming byte array, ignoring null bytes
            int dataSize = Array.IndexOf(data, (byte)0, 4);
            if (dataSize == -1)
            {
                // null byte not found, use full array size
                dataSize = data.Length;
            }

            // Create new byte array to represent the meaningful data of the packet, seperate from the initial length bytes and ending bitfield
            byte[] extractedData = new byte[payloadLength];

            // Create new byte array to represent the ack bitfield
            byte[] ackBytes = new byte[dataSize - payloadLength];

            // Split the extracted data and bitfield data from initial byte array
            Array.Copy(data, 4, extractedData, 0, payloadLength);
            Array.Copy(data, payloadLength, ackBytes, 0, dataSize - payloadLength);

            // Create ackbitfield from ack byte array
            AckBitfield ackBitfield = (AckBitfield)BitConverter.ToUInt32(ackBytes, 0);

            return new Packet(packetType, localSequence, remoteSequence, ackBitfield, sourceIP, sourcePort, extractedData);
        }
    }
}