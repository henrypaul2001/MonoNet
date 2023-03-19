using NetworkingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    public class TestNetworkedObjectStresser : Networked_GameObject
    {
        [NetworkedVariable]
        public int testInt1;
        public int testInt2;
        public int testInt3;
        public int testInt4;

        [NetworkedVariable]
        public string testString1;
        public string testString2;

        [NetworkedVariable]
        public float testFloat1;
        public float testFloat2;
        public float testFloat3;

        public TestNetworkedObjectStresser(NetworkManager networkManager, int clientID, Dictionary<string, string> constructProperties) : base(networkManager, clientID, constructProperties)
        {
            testInt1 = 1;
            testInt2 = 1;
            testInt3 = 1;
            testInt4 = 1;

            testString1 = "initial";
            testString2 = "initial";

            testFloat1 = 1.0f;
            testFloat2 = 1.0f;
            testFloat3 = 1.0f;
        }

        public TestNetworkedObjectStresser(NetworkManager networkManager, int clientID, int objectID) : base(networkManager, clientID, objectID)
        {
            testInt1 = 1;
            testInt2 = 1;
            testInt3 = 1;
            testInt4 = 1;

            testString1 = "initial";
            testString2 = "initial";

            testFloat1 = 1.0f;
            testFloat2 = 1.0f;
            testFloat3 = 1.0f;
        }
    }
}
