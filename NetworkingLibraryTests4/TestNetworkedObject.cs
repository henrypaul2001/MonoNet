using NetworkingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    public class TestNetworkedObject : Networked_GameObject
    {

        [NetworkedVariable]
        public int TestVariable;

        public TestNetworkedObject(NetworkManager networkManager, int clientID, Dictionary<string, string> constructProperties) : base(networkManager, clientID, constructProperties)
        {
            TestVariable = 1;
        }

        public TestNetworkedObject(NetworkManager networkManager, int clientID, int objectID) : base(networkManager, clientID, objectID)
        {
            TestVariable = 1;
        }
    }
}
