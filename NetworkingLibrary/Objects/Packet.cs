using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    [Flags]
    public enum AckBitfield : uint
    {
        Ack1 = 0 << 0,
        Ack2 = 0 << 1,
        Ack3 = 0 << 2,
        Ack4 = 0 << 3,
        Ack5 = 0 << 4,
        Ack6 = 0 << 5,
        Ack7 = 0 << 6,
        Ack8 = 0 << 7,
        Ack9 = 0 << 8,
        Ack10 = 0 << 9,
        Ack11 = 0 << 10,
        Ack12 = 0 << 11,
        Ack13 = 0 << 12,
        Ack14 = 0 << 13,
        Ack15 = 0 << 14,
        Ack16 = 0 << 15,
        Ack17 = 0 << 16,
        Ack18 = 0 << 17,
        Ack19 = 0 << 18,
        Ack20 = 0 << 19,
        Ack21 = 0 << 20,
        Ack22 = 0 << 21,
        Ack23 = 0 << 22,
        Ack24 = 0 << 23,
        Ack25 = 0 << 24,
        Ack26 = 0 << 25,
        Ack27 = 0 << 26,
        Ack28 = 0 << 27,
        Ack29 = 0 << 28,
        Ack30 = 0 << 29,
        Ack31 = 0 << 30,
        Ack32 = 0 << 31,
    }
    
    public enum PacketType
    {
        SYNC,
        REQUEST,
        DISCONNECT,
        ACCEPT,
        CONSTRUCT,
    }

    public class Packet
    {
        byte[] data;
        byte[] compressedData;

        int sequence;
        int ack;

        AckBitfield ackBitfield;
        PacketType packetType;

        string ipDestination;
        string ipSource;
        int portSource;
        int portDestination;

        DateTime sendTime;

        bool lost;

        // Used for creating requst / accept packets on receive
        public Packet(PacketType packetType, string ipSource, int portSource, byte[] data)
        {
            this.packetType = packetType;
            this.ipSource = ipSource;
            this.data = data;
            this.portSource = portSource;
            lost = false;
        }

        // Used for creating construct / sync packets on receive
        public Packet(PacketType packetType, int sequence, int ack, AckBitfield ackBitfield, string ipSource, int portSource, byte[] data, DateTime sendTime)
        {
            this.packetType = packetType;
            this.sequence = sequence;
            this.ack = ack;
            this.ackBitfield = ackBitfield;
            this.data = data;
            this.ipSource = ipSource;
            this.portSource = portSource;
            this.sendTime = sendTime;
            lost = false;

        }

        // Used for creating requst / accept packets on send
        public Packet(string ipDestination, string ipSource, int portDestination, byte[] data, PacketType packetType) 
        {
            this.ipDestination = ipDestination;
            this.ipSource = ipSource;
            this.portDestination = portDestination;
            this.data = data;
            this.packetType = packetType;
            lost = false;

            sendTime = DateTime.Now;

            CompressData();
        }

        // Used for creating construct / sync packets on send
        public Packet(PacketType packetType, int sequence, int ack, AckBitfield ackBitfield, byte[] data, string ipDestination, int portDestination, DateTime sendTime)
        {
            this.packetType = packetType;
            this.sequence = sequence;
            this.ack = ack;
            this.ackBitfield = ackBitfield;
            this.data = data;
            this.ipDestination = ipDestination;
            this.portDestination = portDestination;
            this.sendTime = sendTime;
            lost = false;

            CompressData();
        }

        /// <summary>
        /// Target IP of packet
        /// </summary>
        public string IPDestination
        {
            get { return ipDestination; }
        }

        /// <summary>
        /// Address packet came from
        /// </summary>
        public string IPSource
        {
            get { return ipSource; }
        }

        /// <summary>
        /// Port number packet came from
        /// </summary>
        public int PortSource
        {
            get { return portSource; }
        }

        /// <summary>
        /// Target port number of packet
        /// </summary>
        public int PortDestination
        {
            get { return portDestination; }
        }

        /// <summary>
        /// The data of the packet in byte form
        /// </summary>
        public byte[] Data
        {
            get { return data; }
        }

        /// <summary>
        /// The compressed packet data in byte form
        /// </summary>
        public byte[] CompressedData
        {
            get { return compressedData; }
        }

        /// <summary>
        /// Sequence number of the packet
        /// </summary>
        public int Sequence
        {
            get { return sequence; }
        }

        /// <summary>
        /// Remote sequence number the packet is acknowledging
        /// </summary>
        public int Ack
        {
            get { return ack; }
        }

        /// <summary>
        /// UTC converted time on packet send
        /// </summary>
        public DateTime SendTime
        {
            get { return sendTime; }
        }

        public PacketType PacketType
        {
            get { return packetType; }
        }

        /// <summary>
        /// Bitfield representing which of the 32 most recently received packets are being acknowledged
        /// </summary>
        public AckBitfield AckBitfield
        {
            get { return ackBitfield; }
        }

        internal bool PacketLost
        {
            get { return lost; }
            set { lost = value; }
        }
        
        void CompressData()
        {
            // Not yet implemented
            compressedData = data;
        }

        byte[] DecompressData()
        {
            throw new NotImplementedException();
        }
    }
}
