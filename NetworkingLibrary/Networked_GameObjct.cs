using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public abstract class Networked_GameObject
    {
        NetworkManager networkManager;

        public Networked_GameObject(ref NetworkManager networkManager)
        {
            this.networkManager = networkManager;
        }

        public Packet constructPacket()
        {
            throw new NotImplementedException();
        }
    }
}