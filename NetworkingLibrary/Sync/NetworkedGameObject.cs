using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public abstract class NetworkedGameObject
    {
        NetworkManager networkManager;
        int clientID;
        int objectID;
        bool isLocal;

        Dictionary<string, string> constructProperties;

        /// <summary>
        /// Local constructor
        /// </summary>
        /// <param name="networkManager"></param>
        /// <param name="clientID">ID of local client</param>
        /// <param name="constructProperties">Properties that will be communicated to remote clients upon construction to determine which parameters are used for the object</param>
        public NetworkedGameObject(NetworkManager networkManager, int clientID, Dictionary<string, string> constructProperties)
        {
            this.networkManager = networkManager;
            this.clientID = clientID;
            this.isLocal = true;
            this.networkManager.NetworkedObjects.Add(this);
            this.constructProperties = constructProperties;

            GenerateObjectID();
        }

        /// <summary>
        /// Remote constructor
        /// </summary>
        /// <param name="networkManager"></param>
        /// <param name="clientID">ID of origin client</param>
        /// <param name="objectID">ID of object communicated by remote client</param>
        public NetworkedGameObject(NetworkManager networkManager, int clientID, int objectID)
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

        /// <summary>
        /// The ID of the client this object belongs to
        /// </summary>
        public int ClientID
        {
            get { return clientID; }
        }

        /// <summary>
        /// Used to identify objects in the same client
        /// </summary>
        public int ObjectID
        {
            get { return objectID; }
        }

        /// <summary>
        /// Does this object belong to the local client
        /// </summary>
        public bool IsLocal
        {
            get { return isLocal; }
        }

        /// <summary>
        /// Which parameters should be used when constructing object remotely
        /// </summary>
        public Dictionary<string, string> ConstructProperties
        {
            get { return constructProperties; }
        }
    }
}