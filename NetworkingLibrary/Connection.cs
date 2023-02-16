using System;
using System.Collections.Generic;
using System.Linq;
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

        Diagnostics diagnostics;

        Client localClient;
        Client remoteClient;

        int remoteSequence;
        int localSequence;

        int[] remoteSequenceArray;

        public Connection(Client localClient, Client remoteClient)
        {
            diagnostics = new Diagnostics();

            this.localClient = localClient;
            this.remoteClient = remoteClient;

            remoteSequence = 0;
            localSequence = 0;
            remoteSequenceArray = new int[33];
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

        public void SendPacket(Packet packet)
        {
            localSequence++;
        }

        public void ReceivePacket(Packet packet)
        {
            // Compare sequence of packet against current remote sequence
            // If packet is more recent, update remote sequence number to sequence number of packet
        }
    }
}