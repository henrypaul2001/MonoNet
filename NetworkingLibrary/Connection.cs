using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public class Connection
    {
        public struct Diagnostics
        {
            int packetLoss;
            float RTT;
            float latency;
        }

        float packetTimeoutTime;

        int[] buffer;
        int bufferStart;
        int bufferCount;

        Diagnostics diagnostics;

        Dictionary<int, Packet> packetsWaitingForAck;
        List<Packet> lostPackets;

        Client localClient;
        Client remoteClient;

        int remoteClientID;

        int remoteSequence;
        int localSequence;

        public Connection(Client localClient, Client remoteClient, float packetTimeoutTime)
        {
            diagnostics = new Diagnostics();

            this.localClient = localClient;
            this.remoteClient = remoteClient;
            this.packetTimeoutTime = packetTimeoutTime;

            remoteClientID = remoteClient.ID;

            remoteSequence = 0;
            localSequence = 0;

            buffer = new int[33];
            bufferStart = 0;
            bufferCount = 0;

            packetsWaitingForAck = new Dictionary<int, Packet>();
            lostPackets = new List<Packet>();
        }

        public Diagnostics DiagnosticInfo
        {
            get { return diagnostics; }
        }

        public Client RemoteClient
        {
            get { return remoteClient; }
        }

        public Client LocalClient
        {
            get { return localClient; }
        }

        public int RemoteSequence
        {
            get { return remoteSequence; }
        }

        public int LocalSequence
        {
            get { return localSequence; }
        }

        public int RemoteClientID
        {
            get { return remoteClientID; }
        }

        public void AddToBuffer(int number)
        {
            buffer[(bufferStart + bufferCount) % buffer.Length] = number;
            if (bufferCount < buffer.Length)
            {
                bufferCount++;
            }
            else
            {
                bufferStart = (bufferStart + 1) % buffer.Length;
            }
        }

        public bool BufferContains(int number)
        {
            for (int i = 0; i < bufferCount; i++)
            {
                if (buffer[(bufferStart + 1) % buffer.Length] == number)
                {
                    return true;
                }
            }
            return false;
        }

        public AckBitfield GenerateAckBitfield()
        {
            AckBitfield ackBitfield = new AckBitfield();

            for (int i = 0; i < 33; i++)
            {
                if (BufferContains(remoteSequence - i))
                {
                    ackBitfield |= (AckBitfield)(1 << i);
                }
            }

            return ackBitfield;
        }

        public void CheckForLostPackets()
        {
            foreach (KeyValuePair<int, Packet> pair in packetsWaitingForAck)
            {
                TimeSpan elapsedTime = DateTime.Now.Subtract(pair.Value.SendTime);
                if (elapsedTime.TotalMilliseconds >= packetTimeoutTime)
                {
                    // Packet is lost
                    Debug.WriteLine($"Packet lost: sequence number={pair.Value.Sequence} time sent={pair.Value.SendTime}", "Packet Loss");
                    packetsWaitingForAck.Remove(pair.Key);
                    lostPackets.Add(pair.Value);
                }
            }
        }

        public void PacketSent(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                localSequence++;
                packetsWaitingForAck.Add(packet.Sequence, packet);
            }
        }

        public void PacketReceived(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                AddToBuffer(packet.Sequence);
                if (remoteSequence < packet.Sequence)
                {
                    // Packet is newer
                    remoteSequence = packet.Sequence;
                }

                // Scan ack bitfield
                int ack = packet.Ack;
                AckBitfield ackBitfield = packet.AckBitfield;
                for (int i = 0; i < 32; i++)
                {
                    AckBitfield bit = (AckBitfield)(1 << i);
                    if ((ackBitfield & bit) == bit)
                    {
                        // Bit is set, remove packet from waiting list
                        int acknowledgedSequence = ack - i;
                        packetsWaitingForAck.Remove(acknowledgedSequence);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"LocalIP: {localClient.IP}/LocalPort: {localClient.Port}/RemoteIP: {remoteClient.IP}/RemotePort: {remoteClient.Port}";
        }
    }
}