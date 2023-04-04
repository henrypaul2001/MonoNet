using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public struct Diagnostics
    {
        public int PacketsReceived;
        public int PacketsSent;
        public int PacketsLost;
        public float RTT;
        public float Latency;
        public float PacketLossPercentage;

        private float[] rttBuffer;
        private int rttBufferStart;
        private int rttBufferCount;
        internal int rttBufferMaxSize;

        public Diagnostics(int rttBufferSize)
        {
            PacketsReceived = 0;
            PacketsSent = 0;
            PacketsLost = 0;
            RTT = -1;
            Latency = -1;
            PacketLossPercentage = 0;

            rttBuffer = new float[rttBufferSize];
            rttBufferStart = 0;
            rttBufferCount = 0;
            rttBufferMaxSize = rttBufferSize;
        }

        internal void AddToBuffer(float number)
        {
            rttBuffer[(rttBufferStart + rttBufferCount) % rttBuffer.Length] = number;
            if (rttBufferCount < rttBuffer.Length)
            {
                rttBufferCount++;
            }
            else
            {
                rttBufferStart = (rttBufferStart + 1) % rttBuffer.Length;
            }
        }

        internal void UpdateRTT(float rtt)
        {
            AddToBuffer(rtt);

            if (rttBufferCount == rttBufferMaxSize)
            {
                float bufferSum = 0;
                for (int i = 0; i < rttBufferMaxSize; i++)
                {
                    bufferSum += rttBuffer[i];
                }

                RTT = bufferSum / rttBufferMaxSize;
            }
        }
    }

    public class Connection
    {
        #region stuff for unit tests
        internal int InternalRemoteSequence { set { remoteSequence = value; } }
        internal Dictionary<int, Packet> InternalPacketsWaitingForAck { get { return packetsWaitingForAck; } set { packetsWaitingForAck = value; } }
        internal List<Packet> InternalLostPackets { get { return lostPackets; } }
        #endregion

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
            diagnostics = new Diagnostics(10);

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

        internal void AddToBuffer(int number)
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

        internal bool BufferContains(int number)
        {
            for (int i = 0; i < bufferCount; i++)
            {
                if (buffer[(bufferStart + i) % buffer.Length] == number)
                {
                    return true;
                }
            }
            return false;
        }

        internal AckBitfield GenerateAckBitfield()
        {
            AckBitfield ackBitfield = new AckBitfield();

            for (int i = 0; i < 33; i++)
            {
                if (BufferContains(RemoteSequence - i))
                {
                    ackBitfield |= (AckBitfield)(1 << i);
                }
            }

            return ackBitfield;
        }

        internal void CheckForLostPackets()
        {
            List<int> keysToRemove = new List<int>();
            foreach (KeyValuePair<int, Packet> pair in packetsWaitingForAck)
            {
                TimeSpan elapsedTime = DateTime.UtcNow.Subtract(pair.Value.SendTime);
                if (elapsedTime.TotalMilliseconds >= (packetTimeoutTime * 1000))
                {
                    // Packet is lost
                    Debug.WriteLine($"Packet lost: sequence number={pair.Value.Sequence} time sent={pair.Value.SendTime}", "Packet Loss");
                    keysToRemove.Add(pair.Key);
                    lostPackets.Add(pair.Value);
                    diagnostics.PacketsLost++;
                }
            }

            // Remove lost packets from waiting list
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                packetsWaitingForAck.Remove(keysToRemove[i]);
            }
        }

        internal void PacketSent(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                diagnostics.PacketsSent++;
                localSequence++;
                packetsWaitingForAck.Add(packet.Sequence, packet);
            }
        }

        internal void PacketReceived(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                diagnostics.PacketsReceived++;

                AddToBuffer(packet.Sequence);
                if (RemoteSequence < packet.Sequence)
                {
                    // Packet is newer
                    remoteSequence = packet.Sequence;
                }

                // Scan ack bitfield and update packets waiting for ack list
                ProcessAckBitfield(packet.Ack, packet.AckBitfield);
            }
        }

        internal void ProcessAckBitfield(int ack, AckBitfield bitfield)
        {
            for (int i = 0; i < 33; i++)
            {
                AckBitfield bit = (AckBitfield)(1 << i);
                if ((bitfield & bit) == bit)
                {
                    // Bit is set, remove packet from waiting list
                    PacketAcknowledged(ack - i);
                }
            }
        }

        private void PacketAcknowledged(int acknowledgedSequence)
        {
            Packet acknowledgedPacket;
            bool success = packetsWaitingForAck.TryGetValue(acknowledgedSequence, out acknowledgedPacket);
            if (!success)
            {
                //Debug.WriteLine("Acknowledged packet was not found in waiting list, ignoring acknowledgement", "Packet Acknowledgement");
                return;
            }

            diagnostics.UpdateRTT((float)(DateTime.UtcNow - acknowledgedPacket.SendTime).TotalMilliseconds);

            packetsWaitingForAck.Remove(acknowledgedSequence);
        }

        void UpdateDiagnostics()
        {

        }

        public override string ToString()
        {
            return $"LocalIP: {localClient.IP}/LocalPort: {localClient.Port}/RemoteIP: {remoteClient.IP}/RemotePort: {remoteClient.Port}";
        }
    }
}