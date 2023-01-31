using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal class PacketManager
    {
        List<Packet> packetQueue;

        public PacketManager()
        {

        }

        void ProcessPacket(Packet packet)
        {
            // Process the byte array of packet in packetQueue
        }

        void ReceivePacket()
        {
            // Constructs a custom packet object based on the byte array it received and adds it to the packetqueue
        }

        void SendPacket(Packet packet)
        {
            // Send a packet to it's destination
        }
    }
}