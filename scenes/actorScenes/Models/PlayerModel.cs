using Godot;
using System;

public partial class PlayerModel : AbstractModel
{
    [Export] public long TrackingPeerId { get; set; } = -1;
    [Export] public int ActorID { get; set; } = -1;
    AbstractController playerController { get; set; }
    AnimationTree AnimationTree { get; set; }

    public Node3D pivot;
    public Node3D tilt;
    public Node3D body;
    public AnimationTree antree;
    public Node3D gunarm;
    public Node3D aimer;
    public Node3D raycaster;
    public Node3D handCastPoint;
    public Node3D MissileCastPoint;
    public Node3D rifle;
    public Node3D machinegun;
    public Node3D shotgun;
    public Node3D bazooka;

    public override void AttachController(AbstractController controller)
    {
        playerController = controller;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{

        pivot = GetNode<Node3D>("pivot");
        tilt = GetNode<Node3D>("pivot/tilt");
        body = GetNode<Node3D>("foot_l");
        antree = GetNode<AnimationTree>("AnimationTree");
        gunarm = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm");
        aimer = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/aimer");
        raycaster = GetNode<Node3D>("pivot/tilt/RayCast3D");
        handCastPoint = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/spawner");
        MissileCastPoint = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/missile/spawner");
        rifle = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/rifle");
        machinegun = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/machinegun");
        shotgun = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/shotgun");
        bazooka = GetNode<Node3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/bazooka");

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
        return antree;
    }

    public override Node3D GetCastPoint()
    {
        return GetNode<Marker3D>("CastPoint");
    }
}
