using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public class Connection
    {
        struct Diagnostics
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

        public Connection()
        {
            diagnostics = new Diagnostics();
        }
    }
}
