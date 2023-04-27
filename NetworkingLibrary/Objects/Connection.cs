using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        internal List<Packet> InternalLostPackets { get { return lostPackets; } }
        internal Packet[] InternalSentPacketsBuffer { get { return sentPacketsBuffer; } set { sentPacketsBuffer = value; } }

        internal Dictionary<int, Packet> InternalPacketsWaitingForAck { get { return packetsWaitingForAck; } set { packetsWaitingForAck = value; } }
        #endregion

        float packetTimeoutTime;

        int[] sequenceBuffer;
        int sequenceBufferStart;
        int sequenceBufferCount;

        Diagnostics diagnostics;

        object waitingListLock;

        Dictionary<int, Packet> packetsWaitingForAck;
        List<Packet> lostPackets;

        Packet[] sentPacketsBuffer;
        int sentPacketsBufferStart;
        int sentPacketsBufferCount;

        DateTime timeAtLastPacketReceived;
        DateTime timeAtConnectionEstablished;

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

            timeAtConnectionEstablished = DateTime.UtcNow;

            waitingListLock = new object();
        }

        /// <summary>
        /// Diagnostic struct including RTT, PacketLossPercentage and LatencyEstimation
        /// </summary>
        public Diagnostics DiagnosticInfo
        {
            get { return diagnostics; }
        }

        /// <summary>
        /// The remote client instance of this connection
        /// </summary>
        public Client RemoteClient
        {
            get { return remoteClient; }
        }

        /// <summary>
        /// The local client instance of this connection
        /// </summary>
        public Client LocalClient
        {
            get { return localClient; }
        }

        /// <summary>
        /// The most recently received sequence number in this connection
        /// </summary>
        public int RemoteSequence
        {
            get { return remoteSequence; }
        }

        /// <summary>
        /// The most recently sent sequence number in this connection
        /// </summary>
        public int LocalSequence
        {
            get { return localSequence; }
        }

        /// <summary>
        /// The remote client ID of this connection
        /// </summary>
        public int RemoteClientID
        {
            get { return remoteClientID; }
        }

        /// <summary>
        /// The UTC converted time when the last packet was received in this connection
        /// </summary>
        public DateTime TimeAtLastPacketReceive
        {
            get { return timeAtLastPacketReceived; }
        }

        /// <summary>
        /// The UTC converted time when this connection was established
        /// </summary>
        public DateTime TimeAtConnectionEstablished
        {
            get { return timeAtConnectionEstablished; }
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
            // lock
            lock (waitingListLock)
            {
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
            // unlock
        }

        private void AddPacketToWaitingList(Packet packet)
        {
            lock (waitingListLock)
            {
                try
                {
                    packetsWaitingForAck.Add(packet.Sequence, packet);
                } catch (Exception e)
                {
                    Debug.WriteLine(e, "Packet I/O");
                }
            }
        }

        internal void PacketSent(Packet packet)
        {
            diagnostics.PacketsSent++;
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                localSequence++;
                Task.Run(() => AddPacketToWaitingList(packet));
                AddToPacketBuffer(packet);
                Task.Run(() => diagnostics.UpdatePacketLossPercentage(sentPacketsBufferCount, GetPacketsLostInBuffer()));
            }
        }

        internal void PacketReceived(Packet packet)
        {
            if (packet.PacketType == PacketType.CONSTRUCT || packet.PacketType == PacketType.SYNC)
            {
                diagnostics.PacketsReceived++;
                timeAtLastPacketReceived = DateTime.UtcNow;
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
                    if (ack - i >= 0)
                    {
                        Task.Run(() => PacketAcknowledged(ack - i));
                    }
                }
            }
        }

        private void PacketAcknowledged(int acknowledgedSequence)
        {
            DateTime timeNow = DateTime.UtcNow;

            Packet acknowledgedPacket;
            lock (waitingListLock)
            {
                bool success = packetsWaitingForAck.TryGetValue(acknowledgedSequence, out acknowledgedPacket);
                if (!success)
                {
                    return;
                }

                packetsWaitingForAck.Remove(acknowledgedSequence);
            }

            float rtt = (float)(timeNow - acknowledgedPacket.SendTime).TotalMilliseconds;
            diagnostics.UpdateRTT(rtt);
        }

        internal int GetPacketsLostInBuffer()
        {
            int packetsLost = 0;
            for (int i = 0; i < sentPacketsBufferCount; i++)
            {
                if (sentPacketsBuffer[i] != null)
                {
                    if (sentPacketsBuffer[i].PacketLost)
                    {
                        packetsLost++;
                    }
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