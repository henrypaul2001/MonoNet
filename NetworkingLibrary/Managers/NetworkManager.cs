using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Specialized;
using System.CodeDom;
using System.ComponentModel.Design;
using Newtonsoft.Json;
using System.ComponentModel;

namespace NetworkingLibrary
{
    public enum ConnectionType
    {
        PEER_TO_PEER,
        CLIENT_SERVER,
        DEDICATED_SERVER
    }

    public abstract class NetworkManager
    {
        #region stuff for unit tests
        internal string LastPayloadSent { get; set; }
        internal byte[] LastLocalClientConnectionRequest { get; set; }
        internal Dictionary<string, string> LastRemoteConstructPropertiesCreated { get; set; }
        internal int ConstructRemoteObjectCalls { get; set; }
        internal List<string> PayloadsSent { get; set; }
        internal int DisconnectLocalClientCalls { get; set; }
        internal Client LastClientToTimeout { get; set; }
        internal Assembly TestAssembly { get; set; }

        internal int HandleConnctionRequestCalls { get; set; } = 0;
        internal int HandleConnectionAcceptCalls { get; set; } = 0;
        internal int ProcessSyncPacketCalls { get; set; } = 0;
        internal int ProcessConstructPacketCalls { get; set; } = 0;
        internal int ProcessDisconnectPacketCalls { get; set; } = 0;
        internal int ClientTimeoutCalls { get; set; } = 0;
        #endregion

        // List representing networked game objects that need to be synced
        List<Networked_GameObject> networkedObjects;

        // List representing currently connected clients
        List<Client> remoteClients;

        // List representing current connections
        List<Connection> connections;
        object connectionsLock = new object();

        // List representing clients in the process of establishing connection (waiting for connection acknowledgement)
        List<Client> pendingClients;

        ConnectionType connectionType;

        int hostIndex;
        int protocolID;
        int port;

        float timeoutTime;
        float timeoutGracePeriod;

        Client server;

        Client localClient;

        PacketManager packetManager;

        Logger crashReporter;

        bool closed;

        public NetworkManager(ConnectionType connectionType, int protocolID, int port, float timeoutTime, string crashReportFile, LoggingMode loggingMode, LoggingFormat loggingFormat, bool americanDateFormat)
        {
            this.timeoutTime = timeoutTime;
            crashReporter = new Logger(crashReportFile, loggingMode, loggingFormat, americanDateFormat);
            Initialise(connectionType, protocolID, port);
        }

        public NetworkManager(ConnectionType connectionType, int protocolID, int port, float timeoutTime)
        {
            this.timeoutTime = timeoutTime;
            crashReporter = new Logger("crashreport.txt", LoggingMode.APPEND, LoggingFormat.DATETIMEANDMESSAGE, false);
            Initialise(connectionType, protocolID, port);
        }

        public NetworkManager(ConnectionType connectionType, int protocolID, int port)
        {
            timeoutTime = 5;
            crashReporter = new Logger("crashreport.txt", LoggingMode.APPEND, LoggingFormat.DATETIMEANDMESSAGE, false);
            Initialise(connectionType, protocolID, port);
        }

        void Initialise(ConnectionType connectionType, int protocolID, int port)
        {
            try
            {
                // Load testing assembly
                TestAssembly = Assembly.Load("NetworkingLibraryTests4");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            PayloadsSent = new List<string>(3);

            this.connectionType = connectionType;
            this.protocolID = protocolID;
            this.port = port;
            packetManager = new PacketManager(this);

            pendingClients = new List<Client>();
            remoteClients = new List<Client>();
            connections = new List<Connection>();
            networkedObjects = new List<Networked_GameObject>();

            timeoutGracePeriod = 3;

            closed = false;

            if (this.connectionType == ConnectionType.PEER_TO_PEER)
            {
                string localIP = null;
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // IP is IPv4
                        localIP = ip.ToString();
                        break;
                    }
                }
                localClient = new Client(localIP, this);
            }
        }

        public Client LocalClient
        {
            get { return localClient; }
        }

        public List<Client> RemoteClients
        {
            get { return remoteClients; }
        }

        public List<Client> PendingClients
        {
            get { return pendingClients; }
        }

        public List<Connection> Connections
        {
            get 
            {
                lock (connectionsLock)
                {
                    return connections;
                }
            }
        }

        public float TimeoutTime
        {
            get { return timeoutTime; }
        }

        internal List<Connection> ConnectionsInternal
        {
            set 
            {
                lock (connectionsLock)
                {
                    connections = value;
                }
            }
            get
            {
                lock (connectionsLock) 
                {
                    return connections;
                }
            }
        }

        internal List<Client> PendingClientsInternal
        {
            set { pendingClients = value; }
            get { return  PendingClients; }
        }

        internal List<Client> RemoteClientsInternal
        {
            set { remoteClients = value; }
            get { return remoteClients; }
        }

        public List<Networked_GameObject> NetworkedObjects
        {
            get { return networkedObjects; }
        }

        public ConnectionType ConnectionType
        {
            get { return connectionType; }
        }

        public int ProtocolID
        {
            get { return protocolID; }
        }

        public int Port
        {
            get { return port; }
        }

        internal PacketManager PacketManager
        {
            get { return packetManager; }
        }

        public Logger CrashReporter
        {
            get { return crashReporter; }
        }

        public bool Closed
        {
            get { return closed; }
        }

        public void Update()
        {
            try
            {
                CheckForConnectionTimeouts();

                // Sync game state
                SendGameState();

                // Check all connections for lost packets
                int connectionCount = connections.Count;
                for (int i = 0; i < connectionCount; i++)
                {
                    if (i < connections.Count)
                    {
                        if (connections[i] != null)
                        {
                            connections[i].CheckForLostPackets();
                        }
                    }
                }
            } catch (Exception e)
            {
                crashReporter.Log($"IP: {LocalClient.IP} / Port: {LocalClient.Port} / ID: {LocalClient.ID} / Connections: {Connections.Count} : {e}\n");
                Debug.WriteLine(e);
                Close();
            }
        }

        internal void CheckForConnectionTimeouts()
        {
            List<Connection> timedOutConnections = new List<Connection>();
            int connectionCount = connections.Count;
            for (int i = 0; i < connectionCount; i++)
            {
                if (i < connections.Count)
                {
                    if (connections[i] != null)
                    {
                        if (connections[i].TimeAtLastPacketReceive != null)
                        {
                            TimeSpan timeSinceConnectionEstablished = DateTime.UtcNow.Subtract(connections[i].TimeAtConnectionEstablished);
                            if (timeSinceConnectionEstablished.TotalMilliseconds >= (timeoutGracePeriod * 1000))
                            {
                                TimeSpan timeSinceReceive = DateTime.UtcNow.Subtract(connections[i].TimeAtLastPacketReceive);
                                if (timeSinceReceive.TotalMilliseconds >= (timeoutTime * 1000))
                                {
                                    timedOutConnections.Add(connections[i]);
                                }
                            }
                        }
                    }
                }
            }

            foreach (Connection connection in timedOutConnections)
            {
                LastClientToTimeout = connection.RemoteClient;
                ClientTimeout(connection.RemoteClient);
            }
        }

        public void SendLocalObjectToAllConnections(Networked_GameObject obj)
        {
            Type objType = obj.GetType();
            string payload = $"id={localClient.ID}/objID={obj.ObjectID}/{objType.FullName}/PROPSTART/";

            Dictionary<string, string> properties = obj.ConstructProperties;
            if (properties != null)
            {
                foreach (KeyValuePair<string, string> pair in properties)
                {
                    payload += $"{pair.Key}={pair.Value}/";
                }
            }
            payload += "PROPEND/";

            for (int i = 0; i < connections.Count; i++)
            {
                PayloadsSent.Add(payload);
                CreateAndSendSyncOrConstructPacket(PacketType.CONSTRUCT, payload, connections[i].RemoteClient.IP, connections[i].RemoteClient.Port);
            }
        }

        public void SendLocalObjects(Connection destinationConnection)
        {
            for (int i = 0; i < networkedObjects.Count; i++)
            {
                if (networkedObjects[i].IsLocal)
                {
                    Type objType = networkedObjects[i].GetType();
                    string payload = $"id={localClient.ID}/objID={networkedObjects[i].ObjectID}/{objType.FullName}/PROPSTART/";

                    Dictionary<string, string> properties = networkedObjects[i].ConstructProperties;
                    if (properties != null)
                    {
                        foreach (KeyValuePair<string, string> pair in properties)
                        {
                            payload += $"{pair.Key}={pair.Value}/";
                        }
                    }
                    payload += "PROPEND/";
                    PayloadsSent.Add(payload);
                    CreateAndSendSyncOrConstructPacket(PacketType.CONSTRUCT, payload, destinationConnection.RemoteClient.IP, destinationConnection.RemoteClient.Port);
                    /*
                    Type objType = networkedObjects[i].GetType();
                    ConstructorInfo[] constructors = objType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    ConstructorInfo constructor = null;
                    if (constructors.Length == 1)
                    {
                        constructor = constructors[0];
                    }
                    else if (constructors.Length > 1)
                    {
                        constructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<RemoteConstructor>() != null);
                    }
                    else
                    {
                        // Oh dear
                    }

                    ParameterInfo[] parameters = null;
                    if (constructor != null)
                    {
                        parameters = constructor.GetParameters();
                    }

                    string payload = $"id={localClient.ID}/objID={networkedObjects[i].ObjectID}/{objType.FullName}/PARSTART/";

                    foreach (ParameterInfo param in parameters)
                    {
                        // Serialize parameter by looking at the property in the game object with the matching name
                        //var test1 = networkedObjects[i].GetType();
                        //var test2 = test1.GetProperty(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        //var test3 = test2.GetValue(networkedObjects[i]);
                        //var value = networkedObjects[i].GetType().GetProperty(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(networkedObjects[i]);
                        if (param.Name != "networkManager" && param.Name != "clientID" && param.Name != "objectID")
                        {
                            Type targetType = param.ParameterType;
                            //Type targetType = Type.GetType($"{param.ParameterType.FullName}, {param.ParameterType.Namespace}");

                            // Get the member info
                            MemberInfo memberInfo = networkedObjects[i].GetType().GetMember(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).FirstOrDefault();
                            if (memberInfo == null)
                            {
                                memberInfo = networkedObjects[i].GetType().GetProperty(param.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            }
                            if (memberInfo != null)
                            {
                                // Get the value of member
                                object memberValue = null;
                                if (memberInfo.MemberType == MemberTypes.Field)
                                {
                                    memberValue = ((FieldInfo)memberInfo).GetValue(networkedObjects[i]);
                                    memberValue = Convert.ChangeType(memberValue, targetType);
                                }
                                else if (memberInfo.MemberType == MemberTypes.Property)
                                {
                                    memberValue = ((PropertyInfo)memberInfo).GetValue(networkedObjects[i]);
                                    memberValue = Convert.ChangeType(memberValue, targetType);
                                }

                                // I think youre looking at this a bit wrong, no need for serialize, with the Game1 class, all clients will have the same name for it, so just have reflection type that name in the parameter box as a string
                                string paramValue;
                                try
                                {
                                    paramValue = JsonConvert.SerializeObject(memberValue);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                    paramValue = memberValue.ToString();
                                }
                                payload += $"{param.ParameterType.FullName}={paramValue}/";

                            }
                        }
                    }

                    payload += "PAREND/";

                    CreateAndSendSyncOrConstructPacket(PacketType.CONSTRUCT, payload);
                    */
                }
            }
        }

        public void SendGameState()
        {
            for (int i = 0; i < networkedObjects.Count; i++)
            {
                // Should only send data about local networked objects, instead of relaying information about remote objects back to their original clients
                if (networkedObjects[i].IsLocal)
                {
                    string payload = $"id={localClient.ID}/objID={networkedObjects[i].ObjectID}/VARSTART/";

                    // Find all networked variables using reflection
                    var type = networkedObjects[i].GetType();
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.IsDefined(typeof(NetworkedVariable), false));
                    foreach (var field in fields)
                    {
                        payload += $"{field.Name}={field.GetValue(networkedObjects[i])}/";
                    }
                    payload += "VAREND/";
                    LastPayloadSent = payload;
                    CreateAndSendSyncOrConstructPacket(PacketType.SYNC, payload, "ALL", -1);
                }
            }
        }

        internal int GenerateClientID(List<int> excludedIDs)
        {
            int id;
            Random rnd = new Random();

            do
            {
                id = rnd.Next(100, 201);
            } while (excludedIDs.Contains(id));

            return id;
        }

        internal void CreateAndSendSyncOrConstructPacket(PacketType packetType, string payload, string destinationIP, int destinationPort)
        {
            Packet packet;
            for (int j = 0; j < connections.Count; j++)
            {
                if (destinationIP == connections[j].RemoteClient.IP || destinationIP == "ALL")
                {
                    if (destinationPort == connections[j].RemoteClient.Port || destinationIP == "ALL")
                    {
                        packet = CreateSyncOrConstructPacket(packetType, payload, connections[j]);

                        // Send packet
                        LastPayloadSent = payload;
                        connections[j].PacketSent(packet);
                        packetManager.SendPacket(packet, localClient.Socket);
                    }
                }
            }
        }

        internal Packet CreateSyncOrConstructPacket(PacketType packetType, string payload, Connection connection)
        {
            // Construct packet
            DateTime sendTime = DateTime.UtcNow;
            int localSequence = connection.LocalSequence;
            int remoteSequence = connection.RemoteSequence;
            AckBitfield ackBitfield = connection.GenerateAckBitfield();

            string packetData = $"/{protocolID}/{packetType}/{sendTime:HH:mm:ss}/{localSequence}/{remoteSequence}/" + payload;

            byte[] ackBytes = BitConverter.GetBytes((uint)ackBitfield);
            byte[] data = Encoding.ASCII.GetBytes(packetData);

            // Get the length of the data byte array as a byte array
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);

            // Combine byte arrays into one byte array
            byte[] dataWithAckAndLength = new byte[lengthBytes.Length + ackBytes.Length + data.Length];
            Array.Copy(lengthBytes, 0, dataWithAckAndLength, 0, lengthBytes.Length);
            Array.Copy(data, 0, dataWithAckAndLength, lengthBytes.Length, data.Length);
            Array.Copy(ackBytes, 0, dataWithAckAndLength, data.Length + lengthBytes.Length, ackBytes.Length);

            Packet packet = new Packet(packetType, localSequence, remoteSequence, ackBitfield, dataWithAckAndLength, connection.RemoteClient.IP, connection.RemoteClient.Port, sendTime);

            return packet;
        }

        public abstract void ConstructRemoteObject(int clientID, int objectID, Type objectType, Dictionary<string, string> properties);

        internal void ProcessConstructPacket(Packet constructPacket)
        {
            ProcessConstructPacketCalls++;
            string data = Encoding.ASCII.GetString(constructPacket.Data);
            string[] split = data.Split('/');

            // Retrieve object info from packet
            int clientID;
            bool parseClientID = int.TryParse(split[6].Substring(split[6].IndexOf('=') + 1), out clientID);
            if (!parseClientID)
            {
                Debug.WriteLine("Error parsing client ID, packet ignored");
                return;
            }

            int objectID;
            bool parseObjectID = int.TryParse(split[7].Substring(split[7].IndexOf('=') + 1), out objectID);
            if (!parseObjectID)
            {
                Debug.WriteLine("Error parsing object ID, packet ignored");
                return;
            }

            string typeName = split[8];
            string typeNamespace = typeName.Substring(0, typeName.LastIndexOf('.'));
            Type objType = Type.GetType($"{typeName}, {typeNamespace}");
            if (objType == null)
            {
                // Try finding type from the test assembly
                Type[] types = TestAssembly.GetTypes();
                foreach (Type t in types)
                {
                    if (t.FullName == typeName)
                    {
                        objType = t;
                        break;
                    }
                }

                if (objType == null)
                {
                    // Oh dear
                    Debug.WriteLine("Error parsing object type, packet ignored");
                    return;
                }
            }

            // Find the connection associated with the client ID so that packet sequencing can be updated
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].RemoteClientID == clientID)
                {
                    connections[i].PacketReceived(constructPacket);
                }
            }

            //split[9] == "PROPSTART" -- signifies the start point of the construction properties in packet

            // Create dictionary based on properties included in string
            int propertyIndex = 0;
            string currentString;
            string propertyKey;
            string propertyValue;
            Dictionary<string, string> properties = new Dictionary<string, string>();
            while (true)
            {
                currentString = split[10 + propertyIndex];
                if (currentString == "PROPEND")
                {
                    // Reached end of properties
                    break;
                }

                propertyKey = currentString.Substring(0, currentString.IndexOf('='));
                propertyValue = currentString.Substring(currentString.IndexOf('=') + 1);
                properties.Add(propertyKey, propertyValue);
                propertyIndex++;
            }

            LastRemoteConstructPropertiesCreated = properties;

            // Pass values to abstract method so developer can create local instance of networked object
            ConstructRemoteObject(clientID, objectID, objType, properties);

            /*
            int paramIndex = 0;
            string currentString;
            string paramName;
            string paramValue;
            List<object> parameters = new List<object> { this, clientID, objectID };
            while (true)
            {
                currentString = split[9 + paramIndex];
                if (currentString == "PAREND")
                {
                    // Reached end of parameters
                    break;
                }
                paramName = currentString.Substring(0, currentString.IndexOf('='));
                string paramSerializedValue = currentString.Substring(currentString.IndexOf('=') + 1);

                string paramNamespace = paramName.Substring(0, paramName.IndexOf('.'));
                Type targetType = Type.GetType($"{paramName}, {paramNamespace}");
                object deserializedParameter = JsonConvert.DeserializeObject(paramSerializedValue, targetType);
                object param = Convert.ChangeType(deserializedParameter, targetType);
                parameters.Add(param);
                paramIndex++;
            }

            // Find remote constructor of object
            ConstructorInfo[] constructors = objType.GetConstructors();
            ConstructorInfo targetConstructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<RemoteConstructor>() != null);

            if (targetConstructor != null)
            {
                var instance = targetConstructor.Invoke(parameters.ToArray());
                networkedObjects.Add((Networked_GameObject)instance);
            }
            */
        }

        internal void ProcessSyncPacket(Packet syncPacket)
        {
            ProcessSyncPacketCalls++;
            string data = Encoding.ASCII.GetString(syncPacket.Data);
            string[] split = data.Split('/');

            // Retrieve networked variable info from packet
            int clientID;
            bool parseClientID = int.TryParse(split[6].Substring(split[6].IndexOf('=') + 1), out clientID);
            if (!parseClientID)
            {
                Debug.WriteLine("Error parsing client ID, packet ignored");
                return;
            }

            int objectID;
            bool parseObjectID = int.TryParse(split[7].Substring(split[7].IndexOf('=') + 1), out objectID);
            if (!parseObjectID)
            {
                Debug.WriteLine("Error parsing object ID, packet ignored");
                return;
            }

            // Find the connection associated with the client ID so that packet sequencing can be updated
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].RemoteClientID == clientID)
                {
                    connections[i].PacketReceived(syncPacket);
                }
            }

            //split[8] == "VARSTART" -- signifies the start point of the networked variables in packet

            // Find object corresponding to client ID and object ID
            Networked_GameObject obj = GetNetworkedObjectFromClientAndObjectID(clientID, objectID);

            if (obj == null)
            {
                Debug.WriteLine("Couldn't find object from SYNC packet");
            }
            else
            {
                // Sync variables
                int varIndex = 0;
                string currentString;
                string varName;
                string varValue;
                while (true)
                {
                    currentString = split[9 + varIndex];
                    if (currentString == "VAREND")
                    {
                        // Reached end of networked variables
                        break;
                    }
                    varName = currentString.Substring(0, currentString.IndexOf('='));
                    varValue = currentString.Substring(currentString.IndexOf('=') + 1);

                    // Set value of the corresponding variable in networked object
                    FieldInfo field = obj.GetType().GetField(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        // Convert string value to appropriate type
                        object value = Convert.ChangeType(varValue, field.FieldType);

                        field.SetValue(obj, value);
                    }
                    varIndex++;
                }
            }
        }

        internal Networked_GameObject GetNetworkedObjectFromClientAndObjectID(int clientID, int objectID)
        {
            foreach (Networked_GameObject obj in networkedObjects)
            {
                if (obj.ClientID == clientID)
                {
                    if (obj.ObjectID == objectID)
                    {
                        return obj;
                    }
                }
            }

            return null;
        }

        private void DisconnectLocalClient()
        {
            DisconnectLocalClientCalls++;
            // Create disconnect packet and pass to packet manager
            byte[] data = Encoding.ASCII.GetBytes($"0/{protocolID}/DISCONNECT/id={localClient.ID}");

            Packet packet;
            for (int i = 0; i < connections.Count; i++)
            {
                packet = new Packet(connections[i].RemoteClient.IP, localClient.IP, connections[i].RemoteClient.Port, data, PacketType.DISCONNECT);
                packetManager.SendPacket(packet, localClient.Socket);
            }
        }

        public void ConnectLocalClientToHost(string ip, int port)
        {
            LastLocalClientConnectionRequest = localClient.RequestConnection(ip, port);
        }

        internal void ProcessDisconnectPacket(Packet disconnectPacket)
        {
            ProcessDisconnectPacketCalls++;

            // Find the client that disconnected
            string data = Encoding.ASCII.GetString(disconnectPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID)
            {
                Debug.WriteLine("Error parsing remoteID");
                throw new Exception("Error parsing remoteID of disconnecting client");
            }

            // Remove client from connections
            List<int> clientIDs = GetClientIDs(false);

            if (clientIDs.Contains(remoteID))
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i].RemoteClient.ID == remoteID)
                    {
                        connections.RemoveAt(i);
                    }
                }
                for (int i = 0; i < remoteClients.Count; i++)
                {
                    if (remoteClients[i].ID == remoteID)
                    {
                        remoteClients.RemoveAt(i);
                    }
                }

                ClientDisconnect(remoteID);
            }
            else 
            {
                Debug.WriteLine("Could not find disconnecting client in connections");
            }
        }

        public virtual void HandleConnectionRequest(Packet connectionPacket)
        {
            HandleConnctionRequestCalls++;
            string data = Encoding.ASCII.GetString(connectionPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID) {
                throw new Exception("Error parsing remote ID");
            }

            if (remoteID == -1)
            {
                // Then this is the first connection the requesting client is making, as they do not yet have a client ID
                remoteID = GenerateClientID(GetClientIDs(true));
            }

            Client remoteClient = new Client(connectionPacket.IPSource, connectionPacket.PortSource, remoteID, this);
            
            pendingClients.Add(remoteClient);

            // Send connection accept to remote client
            localClient.AcceptConnection(connectionPacket, remoteID);
        }

        public virtual void HandleConnectionAccept(Packet acceptPacket)
        {
            HandleConnectionAcceptCalls++;
            string data = Encoding.ASCII.GetString(acceptPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID)
            {
                throw new Exception("Error parsing remote ID");
            }

            int yourID;
            bool parseYourID = int.TryParse(split[4].Substring(split[4].IndexOf('=') + 1), out yourID);
            if (!parseYourID)
            {
                throw new Exception("Error parsing remote ID");
            }

            int remoteConnectionsNum;
            bool parseConnectionsNum = int.TryParse(split[5].Substring(split[5].IndexOf('=') + 1), out remoteConnectionsNum);
            if (!parseConnectionsNum)
            {
                throw new Exception("Error parsing remote connections number");
            }

            localClient.InternalID = yourID;

            // Connect to any additional peers
            List<string> currentConnectionAddresses = GetConnectedAddresses();
            List<string> pendingConnectionAddresses = GetPendingAddresses();
            for (int i = 0; i < remoteConnectionsNum * 2; i += 2)
            {
                string remoteIP = split[6 + i].Substring(split[6 + i].IndexOf("=") + 1);
                string remotePort = split[6 + i + 1].Substring(split[6 + i + 1].IndexOf("=") + 1);
                bool isMe = int.Parse(remotePort) == localClient.Port && remoteIP == localClient.IP;
                if (!pendingConnectionAddresses.Contains(remoteIP) && !currentConnectionAddresses.Contains(remoteIP) && !isMe)
                {
                    localClient.RequestConnection(remoteIP, int.Parse(remotePort));
                }
            }

            // Get IDs belonging to currently pending clients
            List<int> pendingClientIDs = new List<int>();
            foreach (Client client in pendingClients)
            {
                pendingClientIDs.Add(client.ID);
            }

            if (pendingClientIDs.Contains(remoteID))
            {
                // If this is true, the current code path is being run on the client that initially received the connection request
                
                // Get remote client from pending clients list
                Client remoteClient = null;
                foreach (Client client in pendingClients)
                {
                    if (client.ID == remoteID)
                    {
                        remoteClient = client;
                        break;
                    }
                }

                remoteClients.Add(remoteClient);

                // Create connection
                Connection connection = new Connection(localClient, remoteClient, 500);
                connections.Add(connection);
                Debug.WriteLine($"Connection created between local: {localClient.IP} {localClient.Port} and remote: {remoteClient.IP} {remoteClient.Port}");
                ConnectionEstablished(connection);

                // Remove remote client from pending list
                pendingClients.Remove(remoteClient);
            }
            else
            {
                // If false, the current code path is being run on the client that sent the initial connection request
                
                // Create remote client
                Client remoteClient = new Client(acceptPacket.IPSource, acceptPacket.PortSource, remoteID, this);
                remoteClients.Add(remoteClient);

                // Create connection
                Connection connection = new Connection(localClient, remoteClient, 500);
                connections.Add(connection);
                Debug.WriteLine($"Connection created between local: {localClient.IP} {localClient.Port} and remote: {remoteClient.IP} {remoteClient.Port}");
                ConnectionEstablished(connection);

                // Send connection accept back to remote client
                localClient.AcceptConnection(acceptPacket, remoteID);
            }
        }

        public abstract void ConnectionEstablished(Connection connection);

        public abstract void ClientDisconnect(int clientID);

        public virtual void ClientTimeout(Client lostClient)
        {
            ClientTimeoutCalls++;

            foreach (Connection connection in connections)
            {
                if (connection.RemoteClient == lostClient)
                {
                    connections.Remove(connection);
                    break;
                }
            }
            
            foreach (Client client in remoteClients)
            {
                if (client == lostClient)
                {
                    remoteClients.Remove(client);
                    break;
                }
            }
        }

        public List<int> GetClientIDs(bool includeLocal)
        {
            // Get all client IDs
            List<int> clientIDs = new List<int>();
            if (remoteClients != null)
            {
                for (int i = 0; i < remoteClients.Count(); i++)
                {
                    if (remoteClients[i] != null)
                    {
                        clientIDs.Add(remoteClients[i].ID);
                    }

                }
            }
            if (includeLocal)
            {
                clientIDs.Add(localClient.ID);
            }
            return clientIDs;
        }

        public List<string> GetConnectedAddresses()
        {
            List<string> addresses = new List<string>();

            /*
            if (remoteClients != null)
            {
                foreach (Client client in remoteClients)
                {
                    addresses.Add(client.IP);
                }
            }
            */

            if (connections != null)
            {
                foreach (Connection connection in connections)
                {
                    addresses.Add(connection.RemoteClient.IP);
                }
            }

            return addresses;
        }

        public List<string> GetPendingAddresses()
        {
            List<string> addresses = new List<string>();

            if (pendingClients != null)
            {
                foreach (Client client in pendingClients)
                {
                    addresses.Add(client.IP);
                }
            }
            
            return addresses;
        }

        public void Close()
        {
            closed = true;
            DisconnectLocalClient();
            localClient.Close();
            networkedObjects = null;
            remoteClients = null;
            connections = null;
            pendingClients = null;
            hostIndex = -1;
            protocolID = -1;
            port = -1;
            server = null;
            localClient = null;
            packetManager = null;
        }
    }
}