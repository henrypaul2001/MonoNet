using System;
using System.Collections.Generic;
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

        int[] buffer;
        int bufferStart;
        int bufferCount;

        Diagnostics diagnostics;

        Client localClient;
        Client remoteClient;

        int remoteClientID;

        int remoteSequence;
        int localSequence;

        public Connection(Client localClient, Client remoteClient)
        {
            diagnostics = new Diagnostics();

            this.localClient = localClient;
            this.remoteClient = remoteClient;

            remoteClientID = remoteClient.ID;

            remoteSequence = 0;
            localSequence = 0;

            buffer = new int[33];
            bufferStart = 0;
            bufferCount = 0;
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

        public void SendPacket(Packet packet)
        {
            localSequence++;
        }

        public void ReceivePacket(Packet packet)
        {
            AddToBuffer(packet.Sequence);
            if (remoteSequence < packet.Sequence)
            {
                // Packet is newer
                remoteSequence = packet.Sequence;
            }
        }
    }
}