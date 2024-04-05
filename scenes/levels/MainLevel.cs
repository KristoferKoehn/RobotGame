using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.NetworkInformation;

public partial class MainLevel : Node3D
{

    ENetMultiplayerPeer EnetPeer;


    public override void _EnterTree()
    {
        //gonna make sure these are instantiated on client and host
        ActorManager.GetInstance();
        ConnectionManager.GetInstance();
        SceneOrganizerManager.GetInstance();
        DeathManager.GetInstance();
        MessageQueue.GetInstance();
        MessageQueueManager.GetInstance();
        SpawnManager.GetInstance();
        StatManager.GetInstance();
        UIManager.GetInstance();
        ModelManager.GetInstance();
    }

    public override void _Ready()
    {

        SceneOrganizerManager.GetInstance().SetCurrentLevel(this);
        ConnectionManager.GetInstance().InitializeConnection();

    }

    public override void _Process(double delta)
    {
        if (Multiplayer.GetUniqueId() != 1)
        {
            return;
        }

        //this is effectively the server tick. All the events more or less  are processed in MessageQueueManager
        MessageQueueManager.GetInstance().ProcessMessages();
    }

    //when a puppetmodel enters scene tree
    public void _on_puppet_models_child_entered_tree(Node node)
    {
        AbstractModel dm = (AbstractModel)node;
        dm.SimulationPeerId = this.Multiplayer.GetUniqueId();
        GD.Print("puppet model spawned, trackingID: " + dm.GetTrackingPeerId() + " sim ID: " + dm.SimulationPeerId);

    }

    //when an ability enters the tree
    public void _on_ability_models_child_entered_tree(Node node)
    {
        AbstractAbility t = (AbstractAbility)node;
        t.ApplyHost(ConnectionManager.GetInstance().host);
    }

}
