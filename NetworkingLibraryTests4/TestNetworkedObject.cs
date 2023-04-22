using NetworkingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    public class TestNetworkedObject : NetworkedGameObject
    {
        [NetworkedVariable]
        public int testVariable;

        public TestNetworkedObject(NetworkManager networkManager, int clientID, Dictionary<string, string> constructProperties) : base(networkManager, clientID, constructProperties)
        {
            testVariable = 1;
        }

        public TestNetworkedObject(NetworkManager networkManager, int clientID, int objectID) : base(networkManager, clientID, objectID)
        {
            testVariable = 1;
        }
    }
}
