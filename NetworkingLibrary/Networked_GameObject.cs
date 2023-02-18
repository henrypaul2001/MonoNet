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
        int objectID;
        bool isLocal;

        public Networked_GameObject(NetworkManager networkManager, int clientID)
        {
            this.networkManager = networkManager;
            this.clientID = clientID;
            this.isLocal = true;
            this.networkManager.NetworkedObjects.Add(this);

            GenerateObjectID();
        }

        public Networked_GameObject(NetworkManager networkManager, int clientID, int objectID)
        {
            this.networkManager = networkManager;
            this.clientID = clientID;
            this.objectID = objectID;
            isLocal = false;

            this.networkManager.NetworkedObjects.Add(this);
        }

        void GenerateObjectID()
        {
            // Get other object IDs
            List<int> objectIDs = new List<int>();
            if (networkManager.NetworkedObjects != null)
            {
                for (int i = 0; i < networkManager.NetworkedObjects.Count(); i++)
                {
                    if (networkManager.NetworkedObjects[i] != null && networkManager.NetworkedObjects[i] != this)
                    {
                        objectIDs.Add(networkManager.NetworkedObjects[i].ObjectID);
                    }
                }
            }

            int id;
            Random rnd = new Random();

            do
            {
                id = rnd.Next(100, 201);
            } while (objectIDs.Contains(id));

            objectID = id;
        }

        public int ClientID
        {
            get { return clientID; }
        }

        public int ObjectID
        {
            get { return objectID; }
        }

        public bool IsLocal
        {
            get { return isLocal; }
        }

        /*
        public Packet constructPacket()
        {
            throw new NotImplementedException();
        }
        */
    }
}