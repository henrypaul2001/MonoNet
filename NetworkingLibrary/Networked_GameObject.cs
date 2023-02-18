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
        int clientID;

        public Networked_GameObject(NetworkManager networkManager, int clientID)
        {
            this.networkManager = networkManager;
            this.clientID = clientID;

            this.networkManager.NetworkedObjects.Add(this);
        }

        public int ClientID
        {
            get { return clientID; }
        }

        /*
        public Packet constructPacket()
        {
            throw new NotImplementedException();
        }
        */
    }
}