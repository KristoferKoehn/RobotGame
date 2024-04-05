using Godot;
using MMOTest.scripts.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class GameLoop : Node
{
    public static Node Root = null;
    public Stack<Node> sceneStack = new Stack<Node>();
    int PORT = 9999;

    public override void _EnterTree()
    {
        Root = GetTree().Root;
        ConnectionManager.GetInstance();
        SceneOrganizerManager.GetInstance();
    }

    public override void _Ready()
    {
        if (OS.HasFeature("dedicated_server") || OS.GetCmdlineUserArgs().Contains("--host"))
        {
            string ip = UpnpSetup();
            UniversalConnector connector = new UniversalConnector("50.47.173.115", PORT);
            connector.Host("ActorImplementation", ip);
            MainLevel tL = GD.Load<PackedScene>("res://scenes/levels/MainLevel.tscn").Instantiate<MainLevel>();
            ConnectionManager.GetInstance().Connector = connector;
            ConnectionManager.GetInstance().host = true;
            this.PushScene(tL);
        } else
        {
            PushScene(ResourceLoader.Load<PackedScene>("res://scenes/menu/MainMenu.tscn").Instantiate());
        }
        // put settings here
    }


    public void PushScene(Node node)
    {
        if (sceneStack.Count > 0)
        {
            this.RemoveChild(sceneStack.Peek());
        }
        this.sceneStack.Push(node);
        this.AddChild(node);
    }

    public void PopScene()
    {
        Node node = sceneStack.Pop();
        this.RemoveChild(node);
        node.QueueFree();
        this.AddChild(sceneStack.Peek());
    }

    public string UpnpSetup()
    {
        Upnp upnp = new Upnp();

        int result = upnp.Discover();

        Debug.Assert((Upnp.UpnpResult)result == Upnp.UpnpResult.Success, $"UPNP DISCOVER FAILED! ERROR {result}");

        Debug.Assert(upnp.GetGateway() != null && upnp.GetGateway().IsValidGateway(), "ESTABLISH GATEWAY FAILED");

        int MapResult = upnp.AddPortMapping(9001);
        Debug.Assert(MapResult == 0, "INVALID PORT MAPPING");

        GD.Print($"SUCCESSFUL UPNP SETUP? map result: {MapResult} - valid gateway: {upnp.GetGateway().IsValidGateway()}");
        return upnp.QueryExternalAddress();
    }
}
