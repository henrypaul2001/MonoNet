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
        STANDARD,
        CONNECT,
        DISCONNECT,
        ACCEPT,
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

        public Packet(PacketType packetType, string ipSource, int portSource, byte[] data)
        {
            this.packetType = packetType;
            this.ipSource = ipSource;
            this.data = data;
            this.portSource = portSource;
        }

        public Packet(string ipDestination, string ipSource, int portDestination, byte[] data, PacketType packetType) 
        {
            this.ipDestination = ipDestination;
            this.ipSource = ipSource;
            this.portDestination = portDestination;
            this.data = data;
            this.packetType = packetType;

            CompressData();
        }

        public string IPDestination
        {
            get { return ipDestination; }
        }

        public string IPSource
        {
            get { return ipSource; }
        }

        public int PortSource
        {
            get { return portSource; }
        }

        public int PortDestination
        {
            get { return portDestination; }
        }

        public byte[] Data
        {
            get { return data; }
        }

        public byte[] CompressedData
        {
            get { return compressedData; }
        }

        public int Sequence
        {
            get { return sequence; }
        }

        public int Ack
        {
            get { return ack; }
        }

        public PacketType PacketType
        {
            get { return packetType; }
        }

        public AckBitfield AckBitfield
        {
            get { return ackBitfield; }
        }

        void CompressData()
        {
            compressedData = data;
        }

        byte[] DecompressData()
        {
            throw new NotImplementedException();
        }
    }
}
