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
        #region stuff for unit tests
        internal int PacketsIgnored { get; set; } = 0;
        internal int PacketsProcessed { get; set; } = 0;
        internal int LastPacketSentTotalBytes { get; set; } = 0;
        internal Packet LastPacketConstructed { get; set; }
        #endregion

        NetworkManager networkManager;

        public PacketManager(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
        }

        internal void StartReceiving(ISocket socket, NetworkManager networkManager)
        {
            byte[] buffer = new byte[1024];

            // Create new endpoint that will represent the IP address of the sender
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, remoteEP, result =>
            {
                // Pass extra parameters to callback
                ReceiveCallback(result, buffer, socket, networkManager);
            }, socket);
        }

        private void ReceiveCallback(IAsyncResult result, byte[] data, ISocket socket, NetworkManager networkManager)
        {
            try
            {
                //ISocket socket = (ISocket)result.AsyncState;

                // Create new endpoint that will represent the IP address of the sender
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                int bytesReceived = socket.EndReceiveFrom(result, ref remoteEP);

                IPEndPoint remoteIP = (IPEndPoint)remoteEP;

                Debug.WriteLine($"{bytesReceived} bytes received from IP: {remoteIP.Address}", "Packet I/O");

                // Check if packet belongs to game by checking IP address against current connections, or checking if the protocol ID is a match
                List<string> addresses = networkManager.GetConnectedAddresses();
                string output = Encoding.ASCII.GetString(data);
                if (output.Contains('/'))
                {
                    string[] split = output.Split('/');
                    int protocolID;
                    bool intParse = int.TryParse(split[1], out protocolID);
                    if (!intParse)
                    {
                        Debug.WriteLine("Could not parse protocol ID - packet ignored", "Packet I/O");
                        PacketsIgnored++;
                        return;
                    }

                    if (addresses.Contains(remoteIP.Address.ToString()))
                    {
                        // Packet belongs to game
                        string status = ConstructAndProcessPacketFromByteArray(data, remoteIP.Address.ToString(), remoteIP.Port);
                        if (status == "SUCCESS")
                        {
                            PacketsProcessed++;
                        }
                        else
                        {
                            Debug.WriteLine("Error constructing/processing packet from byte array", "Packet I/O");
                        }
                    }
                    else if (protocolID == networkManager.ProtocolID)
                    {
                        // Packet belongs to game
                        string status = ConstructAndProcessPacketFromByteArray(data, remoteIP.Address.ToString(), remoteIP.Port);
                        if (status == "SUCCESS")
                        {
                            PacketsProcessed++;
                        }
                        else
                        {
                            Debug.WriteLine("Error constructing/processing packet from byte array", "Packet I/O");
                        }
                    }

                    StartReceiving(socket, networkManager);
                }
            } catch (Exception e)
            {
                Debug.WriteLine(e, "Packet I/O");
                StartReceiving(socket, networkManager);
                PacketsIgnored++;
            }
        }

        internal void SendPacket(Packet packet, ISocket socket)
        {
            // Send a packet to it's destination
            IPEndPoint destination = new IPEndPoint(IPAddress.Parse(packet.IPDestination), packet.PortDestination);

            socket.BeginSendTo(packet.CompressedData, 0, packet.CompressedData.Length, SocketFlags.None, destination, result =>
            {
                // Pass extra parameters to callback
                SendCallback(result, socket, destination);
            }, socket);
        }

        private void SendCallback(IAsyncResult result, ISocket socket, IPEndPoint remoteEP)
        {
            try
            {
                //ISocket socket = (ISocket)result.AsyncState;
                int bytesSent = socket.EndSend(result);
                Debug.WriteLine($"Sent {bytesSent} bytes to {remoteEP.Address}", "Packet I/O");
                LastPacketSentTotalBytes = bytesSent;

            } catch (Exception e)
            {
                Debug.WriteLine($"Error sending packet: {e}", "Packet I/O");
            }
        }

        internal string ConstructAndProcessPacketFromByteArray(byte[] data, string sourceIP, int sourcePort)
        {
            string completionStatus = "FAIL";
            string payload = Encoding.ASCII.GetString(data, 0, data.Length);

            if (payload.Contains('/'))
            {
                string[] split = payload.Split('/');

                Packet packet;
                switch (split[2])
                {
                    case "REQUEST":
                        // Connection packet

                        // Begin establishing connection
                        packet = new Packet(PacketType.REQUEST, sourceIP, sourcePort, data);
                        networkManager.HandleConnectionRequest(packet);
                        completionStatus = "SUCCESS";
                        LastPacketConstructed = packet;
                        break;
                    case "ACCEPT":
                        // Connection accept packet

                        // Create accept packet and pass to network manager
                        packet = new Packet(PacketType.ACCEPT, sourceIP, sourcePort, data);
                        networkManager.HandleConnectionAccept(packet);
                        completionStatus = "SUCCESS";
                        LastPacketConstructed = packet;
                        break;
                    case "SYNC":
                        // Synchronisation packet

                        // Create sync packet and pass to network manager
                        packet = CreateConstructOrSyncPacketFromByteArray(PacketType.SYNC, data, sourceIP, sourcePort);
                        networkManager.ProcessSyncPacket(packet);
                        completionStatus = "SUCCESS";
                        LastPacketConstructed = packet;
                        break;
                    case "CONSTRUCT":
                        // Object construction packet (a new local object has been created on a remote client, therefore all clients in session must now create a matching object locally)

                        // Create construct packet and pass to network manager
                        packet = CreateConstructOrSyncPacketFromByteArray(PacketType.CONSTRUCT, data, sourceIP, sourcePort);
                        networkManager.ProcessConstructPacket(packet);
                        completionStatus = "SUCCESS";
                        LastPacketConstructed = packet;
                        break;
                    case "DISCONNECT":
                        // Disconnect packet

                        // Create disconnect packet and pass to network manager
                        packet = new Packet(PacketType.DISCONNECT, sourceIP, sourcePort, data);
                        networkManager.ProcessDisconnectPacket(packet);
                        completionStatus = "SUCCESS";
                        LastPacketConstructed = packet;
                        break;
                }
            }
            else
            {
                completionStatus = "FAIL - DATA DOES NOT CONTAIN SEPERATION CHARACTER";
            }

            return completionStatus;
        }

        private Packet CreateConstructOrSyncPacketFromByteArray(PacketType packetType, byte[] data, string sourceIP, int sourcePort)
        {
            string payload = Encoding.ASCII.GetString(data, 0, data.Length);

            string[] split = payload.Split('/');

            DateTime sendTime = DateTime.ParseExact(split[3], "HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal);
            int localSequence = int.Parse(split[4]);
            int remoteSequence = int.Parse(split[5]);

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

            Packet packet = new Packet(packetType, localSequence, remoteSequence, ackBitfield, sourceIP, sourcePort, extractedData, sendTime);

            LastPacketConstructed = packet;
            return packet;
        }
    }
}