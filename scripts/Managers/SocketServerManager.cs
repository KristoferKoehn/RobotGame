using Godot;
using MMOTest.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Managers.SocketServerManager
{

    public partial class SocketServerManager : Node
    {

        private SocketServerManager() { }
        private static SocketServerManager instance = null;
        TcpServer TCPin = null;
        WebSocketPeer wsp = null;
        WebSocketPeer.State prevstate = WebSocketPeer.State.Closed;

        X509Certificate cert = null;
        CryptoKey cryptoKey = null;

        public static SocketServerManager GetInstance()
        {
            if (instance == null)
            {
                instance = new SocketServerManager();
                instance.Name = "SocketServerManager";
                GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            }

            return instance;
        }

        public override void _Ready()
        {
            GD.Print("loading socket server...");
            cert = ResourceLoader.Load<X509Certificate>("res://assets/ServerResources/serverCAS.crt");
            cryptoKey = ResourceLoader.Load<CryptoKey>("res://assets/ServerResources/serverKey.key");;
            wsp = new WebSocketPeer();

            TCPin = new TcpServer();
            TCPin.Listen(9002);
           

            if(!TCPin.IsListening())
            {
                GD.Print("TCP server error");
            } else
            {
                GD.Print("TCP server listening...");
            }

        }

        public override void _Process(double delta)
        {

            while (TCPin.IsConnectionAvailable())
            {
                StreamPeerTcp stream = TCPin.TakeConnection();
                GD.Print("web client connected from" + stream.GetConnectedHost());

                StreamPeerTls sptls = new StreamPeerTls();
                sptls.AcceptStream(stream, TlsOptions.Server(cryptoKey, cert));
                stream.Poll();

                wsp.AcceptStream(sptls);
                GD.Print(stream.GetStatus());
            }

            wsp.Poll();
            

            WebSocketPeer.State state = wsp.GetReadyState();

            if (state == WebSocketPeer.State.Open)
            {
                if(state != prevstate)
                {
                    GD.Print("Open!");
                    
                }

                //make json of list of enemies and list of 
                List<byte[]> allies = new List<byte[]>();
                
                foreach(Actor a in ActorManager.GetInstance().actors.Values)
                {
                    if(a.PuppetModelReference != null)
                    {
                        allies.Add(GD.VarToBytes(a.PuppetModelReference.GlobalPosition));
                        
                    }
                }

                wsp.SendText(JsonConvert.SerializeObject(allies));


                while (wsp.GetAvailablePacketCount() > 0)
                {
                    GD.Print(wsp.GetPacket().GetStringFromUtf8());
                }
            }

            if (state == WebSocketPeer.State.Closed && state != prevstate)
            {
                wsp.Close();
                GD.Print("Closed");
            }

            if (state == WebSocketPeer.State.Closing && state != prevstate)
            {
                GD.Print("Closing...");
            }
            if (state == WebSocketPeer.State.Connecting && state != prevstate)
            {
                GD.Print("Connecting...");
            }

            prevstate = state;
        }

    }




}

