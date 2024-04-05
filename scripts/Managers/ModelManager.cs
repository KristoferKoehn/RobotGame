using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;
using System;
using System.Collections.Generic;

public partial class ModelManager : Node
{

    static ModelManager instance = null;


    private ModelManager()
    {

    }

    public static ModelManager GetInstance()
    {
        if (instance == null)
        {
            instance = new ModelManager();
            GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            instance.Name = "ModelManager";
        }
        return instance;
    }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (Multiplayer.GetUniqueId() != 1)
        {
            //do client-only stuff here
            return;
        }

        foreach (Actor actor in ActorManager.GetInstance().actors.Values)
        {
            //makes all the stuff line up. Assign all synced variables from client to puppet
            if (actor.PuppetModelReference != null && actor.ClientModelReference != null)
            {
                actor.PuppetModelReference.GlobalPosition = actor.ClientModelReference.GlobalPosition;
                actor.PuppetModelReference.GlobalRotation = actor.ClientModelReference.GlobalRotation;
                actor.PuppetModelReference.Velocity = actor.ClientModelReference.Velocity;


                //change this to sync across all animationtree params (this will suck)
                if (actor.PuppetModelReference.GetAnimationPlayer().CurrentAnimation != actor.ClientModelReference.GetAnimationPlayer().CurrentAnimation)
                {
                    actor.PuppetModelReference.GetAnimationPlayer().CurrentAnimation = actor.ClientModelReference.GetAnimationPlayer().CurrentAnimation;
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientModelChange(int PeerID, int ActorID, string classname)
    {
        //changing the client model clientside

        Actor a = ActorManager.GetInstance().GetActor(ActorID);

        if (a != null)
        {
            if (Multiplayer.GetUniqueId() > 1 && a.ClientModelReference != null) {
                Timer t = new Timer();
                t.Timeout += a.ClientModelReference.QueueFree;
                t.Timeout += t.QueueFree;
                this.AddChild(t);
                t.Start(20);
            }

            a.ClientModelReference = ResourceLoader.Load<PackedScene>("res://scenes/actorScenes/Models/" + classname + "Model.tscn", cacheMode: ResourceLoader.CacheMode.Reuse).Instantiate<AbstractModel>();
            a.ClientModelReference.GetMultiplayerSynchronizer().SetVisibilityFor(0, false);
            a.ClientModelReference.GetMultiplayerSynchronizer().SetVisibilityFor(1, true);
            a.ClientModelReference.GetMultiplayerSynchronizer().SetVisibilityFor(PeerID, true);
            a.ClientModelReference.SetMultiplayerAuthority(PeerID);
            SceneOrganizerManager.GetInstance().GetCurrentLevel().GetNode<Node>("ClientModels").AddChild(a.ClientModelReference, forceReadableName: true);

            a.ClientModelReference.Name = PeerID.ToString();
            a.ClientModelReference.SetActorID(ActorID);

            if(Multiplayer.GetUniqueId() > 1)
            {
                SceneOrganizerManager.GetInstance().GetCurrentLevel().GetNode<PlayerController>("PlayerController").AttachModel(a.ClientModelReference);
            }
            
        }
    }

    public void ChangeActorModel(int ActorID, string classname)
    {
        Actor a = ActorManager.GetInstance().GetActor(ActorID);
        long PeerID = a.ActorMultiplayerAuthority;

        //remote client model change
        RpcId(PeerID, "ClientModelChange", (int)PeerID, ActorID, classname);
        //server client model change
        ClientModelChange((int)PeerID, ActorID, classname);

        //create and assign new puppet model
        
        if(a.PuppetModelReference != null)
        {
            Timer t = new Timer();
            t.Timeout += a.PuppetModelReference.QueueFree;
            t.Timeout += t.QueueFree;
            this.AddChild(t);
            t.Start(20);
        }

        a.PuppetModelReference = ResourceLoader.Load<PackedScene>("res://scenes/actorScenes/Models/" + classname + "Model.tscn", cacheMode: ResourceLoader.CacheMode.Reuse).Instantiate<AbstractModel>();
        a.PuppetModelReference.SetMultiplayerAuthority(1);
        a.PuppetModelReference.SetTrackingPeerId(PeerID);
        a.PuppetModelReference.SetActorID(ActorID);

        SceneOrganizerManager.GetInstance().GetCurrentLevel().GetNode<Node>("PuppetModels").AddChild(a.PuppetModelReference, forceReadableName: true);

    }

    // the route to think about is this:

    // when the player dies, the spawn panel pops up after a few seconds.
    // the player selects a class, and what needs to happen is the models get reassigned.
    // the puppet is rather easy because of the spawner node, from the server just make it and assign it to an actor
    // the client model needs to be made serverside AND RPC'd into the client.
    // then in basically the same function call, the client model gets moved to the spawn position. 

    // the only issues I forsee is that the player controller snaps to the new model in a weird way.
    // 



}
