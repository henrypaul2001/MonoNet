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
        public float LatencyEstimation;
        public float PacketLossPercentage;

        private float[] rttBuffer;
        private int rttBufferStart;
        private int rttBufferCount;
        private int rttBufferMaxSize;

        public Diagnostics(int rttBufferSize)
        {
            PacketsReceived = 0;
            PacketsSent = 0;
            PacketsLost = 0;
            RTT = -1;
            LatencyEstimation = -1;
            PacketLossPercentage = 1;

            rttBuffer = new float[rttBufferSize];
            rttBufferStart = 0;
            rttBufferCount = 0;
            rttBufferMaxSize = rttBufferSize;
        }

        private void AddToBuffer(float number)
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
                LatencyEstimation = RTT / 2;
            }
        }

        internal void UpdatePacketLossPercentage(float totalSent, float totalLost)
        {
            PacketLossPercentage = (totalLost / totalSent) * 100;
        }
    }

    public class Connection
    {
        #region stuff for unit tests
        internal int InternalRemoteSequence { set { remoteSequence = value; } }
        internal Dictionary<int, Packet> InternalPacketsWaitingForAck { get { return packetsWaitingForAck; } set { packetsWaitingForAck = value; } }
        internal List<Packet> InternalLostPackets { get { return lostPackets; } }
        internal Packet[] InternalSentPacketsBuffer { get { return sentPacketsBuffer; } set { sentPacketsBuffer = value; } }
        #endregion

        float packetTimeoutTime;

        int[] sequenceBuffer;
        int sequenceBufferStart;
        int sequenceBufferCount;

        Diagnostics diagnostics;

        Dictionary<int, Packet> packetsWaitingForAck;
        List<Packet> lostPackets;

        Packet[] sentPacketsBuffer;
        int sentPacketsBufferStart;
        int sentPacketsBufferCount;

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

            sequenceBuffer = new int[33];
            sequenceBufferStart = 0;
            sequenceBufferCount = 0;

            sentPacketsBuffer = new Packet[100];
            sentPacketsBufferStart = 0;
            sentPacketsBufferCount = 0;

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

        internal void AddToPacketBuffer(Packet packet)
        {
            sentPacketsBuffer[(sentPacketsBufferStart + sentPacketsBufferCount) % sentPacketsBuffer.Length] = packet;
            if (sentPacketsBufferCount < sentPacketsBuffer.Length)
            {
                sentPacketsBufferCount++;
            }
            else
            {
                sentPacketsBufferStart = (sentPacketsBufferStart + 1) % sentPacketsBuffer.Length;
            }
        }

        internal void AddToSequenceBuffer(int number)
        {
            sequenceBuffer[(sequenceBufferStart + sequenceBufferCount) % sequenceBuffer.Length] = number;
            if (sequenceBufferCount < sequenceBuffer.Length)
            {
                sequenceBufferCount++;
            }
            else
            {
                sequenceBufferStart = (sequenceBufferStart + 1) % sequenceBuffer.Length;
            }
        }

        internal bool BufferContains(int number)
        {
            for (int i = 0; i < sequenceBufferCount; i++)
            {
                if (sequenceBuffer[(sequenceBufferStart + i) % sequenceBuffer.Length] == number)
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
                    pair.Value.PacketLost = true;
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
                AddToPacketBuffer(packet);
                diagnostics.UpdatePacketLossPercentage(sentPacketsBufferCount, GetPacketsLostInBuffer());
            }
        }

        internal void PacketReceived(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                diagnostics.PacketsReceived++;

                AddToSequenceBuffer(packet.Sequence);
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

            float rtt = (float)(DateTime.UtcNow - acknowledgedPacket.SendTime).TotalMilliseconds;

            packetsWaitingForAck.Remove(acknowledgedSequence);

            diagnostics.UpdateRTT(rtt);
        }

        internal int GetPacketsLostInBuffer()
        {
            int packetsLost = 0;
            for (int  i = 0; i < sentPacketsBufferCount; i++)
            {
                if (sentPacketsBuffer[i].PacketLost)
                {
                    packetsLost++;
                }
            }
            return packetsLost;
        }

        public override string ToString()
        {
            return $"LocalIP: {localClient.IP}/LocalPort: {localClient.Port}/RemoteIP: {remoteClient.IP}/RemotePort: {remoteClient.Port}";
        }
    }
}