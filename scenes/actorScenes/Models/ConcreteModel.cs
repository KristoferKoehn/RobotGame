using Godot;
using System;

public partial class ConcreteModel : AbstractModel
{
    [Export] public long TrackingPeerId { get; set; } = -1;
    [Export] public int ActorID { get; set; } = -1;
    AbstractController playerController { get; set; }
    AnimationTree AnimationTree { get; set; }

    public override void AttachController(AbstractController controller)
    {
        playerController = controller;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        if (TrackingPeerId == SimulationPeerId)
        {
            this.Visible = false;
            this.GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        }

        AnimationTree at;
        if ((at = GetNodeOrNull<AnimationTree>("AnimationTree")) != null)
        {
            AnimationTree = at;
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

    public override void ApplyImpulse(Vector3 vec)
    {
        //if not puppet, apply impulse. This prevents puppets from being pushed around/throwing errors because they don't have controllers
        if (playerController != null)
        {
            playerController.ApplyImpulse(vec);
        }
    }

    public override long GetTrackingPeerId()
    {
        return this.TrackingPeerId;
    }

    public override MultiplayerSynchronizer GetMultiplayerSynchronizer()
    {
        return this.GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
    }

    public override AnimationPlayer GetAnimationPlayer()
    {
        return this.GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void SetTrackingPeerId(long peerId)
    {
        this.TrackingPeerId = peerId;
    }

    public override void SetActorID(int actorId)
    {
        ActorID = actorId;
    }

    public override int GetActorID()
    {
        return this.ActorID;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public override void AssignDeathState(bool isdead)
    {
        GD.Print("death state from server: " + isdead);
        this.IsDead = isdead;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public override void MovePlayerToPosition(Vector3 globalPosition)
    {
        this.GlobalPosition = globalPosition;
    }

    public override AnimationTree GetAnimationTree()
    {
        return AnimationTree;
    }
}
