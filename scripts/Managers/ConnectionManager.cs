using Godot;
using Managers.SocketServerManager;
using MMOTest.Backend;
using System.Collections.Generic;
using System.Linq;

namespace MMOTest.scripts.Managers
{
    public partial class ConnectionManager : Node
    {

        private static ConnectionManager instance = null;

        ENetMultiplayerPeer EnetPeer;
        const int PORT = 9001;
        public string ServerAddress = "localhost";
        public UniversalConnector Connector;
        public bool host = false;
        public bool headless = false;
        string PuppetNodePath = "PuppetModels";
        string ClientNodePath = "ClientModels";

        private ConnectionManager() { }
        public static ConnectionManager GetInstance()
        {
            if (instance == null)
            {
                instance = new ConnectionManager();
                GameLoop.Root.GetNode<GameLoop>("GameLoop").AddChild(instance);
                instance.Name = "ConnectionManager";
            }

            return instance;
        }

        public void InitializeConnection()
        {
            EnetPeer = new ENetMultiplayerPeer();
            if (OS.HasFeature("dedicated_server") || OS.GetCmdlineUserArgs().Contains("--host"))
            {
                headless = true;
            }
            if (host && headless)
            {
                HeadlessHost();
            }
            else
            {
                Join();
            }

            GD.Print("headless: " + headless + " host: " + host);
        }

        public void HeadlessHost()
        {
            SocketServerManager.GetInstance();
            EnetPeer.CreateServer(PORT);
            Multiplayer.MultiplayerPeer = EnetPeer;
            Multiplayer.PeerConnected += EstablishActor;
            Multiplayer.PeerDisconnected += RemoveActor;
            Timer t = new Timer();
            t.Timeout += Connector.HostRefresh;
            this.AddChild(t);
            t.Start(5);

        }

        public void Join()
        {
            EnetPeer.CreateClient(ServerAddress, PORT);
            Multiplayer.MultiplayerPeer = EnetPeer;
        }

        public void EstablishActor(long PeerId)
        {
            GD.Print("Establishing Actor for connecting client: " + PeerId);
            RandomNumberGenerator rng = new RandomNumberGenerator();
            int ActorID = (int)rng.Randi();
            while (ActorManager.GetInstance().GetActor(ActorID) != null)
            {
                ActorID = (int)rng.Randi();
            }

            //stop here, wait for spawn signal

            //establish actor across both simulations
            ActorManager.GetInstance().CreateActor(PeerId, ActorID);
            ActorManager.GetInstance().RpcId(PeerId, "CreateActor", PeerId, ActorID);


        }

        public void RemoveActor(long PeerId)
        {
            List<Actor> actors = ActorManager.GetInstance().GetActorsFromPeerID(PeerId);

            foreach (Actor actor in actors)
            {
                ActorManager.GetInstance().RemoveActor(actor.ActorID);
                if (actor.ClientModelReference != null)
                {
                    actor.ClientModelReference.QueueFree();
                }
                if (actor.PuppetModelReference != null)
                {
                    actor.PuppetModelReference.QueueFree();
                }
                UIManager.GetInstance().UnregisterActor(actor.ActorID);
                GD.Print("Actor Left: " + actor.ActorID);
            }
        }
    }
}
