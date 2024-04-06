using Godot;
using System;

[Tool]
public partial class EnemyModel : AbstractModel
{
    [Export] public long TrackingPeerId { get; set; } = -1;
    [Export] public int ActorID { get; set; } = -1;
    [Export] public Vector2 MovementBlendPosition { get; set; } = Vector2.Zero;
    AbstractController playerController { get; set; }
    AnimationTree AnimationTree { get; set; }
    [Export] public int Leg { get; set; } = 0;
    [Export] public int Body { get; set; } = 0;
    [Export] public int BackWeapon { get; set; } = 0;
    [Export] public int HandWeapon { get; set; } = 0;

    int lastLeg = -1;
    int lastBody = -1;
    int lastBackWeapon = -1;
    int lastHandWeapon = -1;

    [Export] public Node3D LegNodes { get; set; } = null;
    [Export] public Node3D BodyNodes { get; set; } = null;

    [Export] public Marker3D HandMarker { get; set; } = null;
    [Export] public Marker3D BackMarker { get; set; } = null;

    AnimationTree ATree = null;

    float Marker = 0;


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

        if (lastLeg != Leg)
        {

            GD.Print("LegDifference");
            if (LegNodes != null)
            {
                LegNodes.QueueFree();
            }

            LegNodes = GD.Load<PackedScene>($"res://assets/DanAssets/enemies/{Leg}Leg.tscn").Instantiate<Node3D>();

            switch (Leg)
            {
                case 0:
                    Marker = 3.85f; break;
                case 1:
                    Marker = 4.43f; break;
                case 2:
                    Marker = 4.65f; break;
                default:
                    Marker = 4.43f; break;
            }

            AddChild(LegNodes);
            ATree = LegNodes.GetNode<AnimationTree>("AnimationTree");

            if (Engine.IsEditorHint())
            {
                LegNodes.Owner = this;
            }

        }

        if (lastBody != Body)
        {
            GD.Print("Difference Body");
            if (BodyNodes != null)
            {
                BodyNodes.QueueFree();
            }

            BodyNodes = GD.Load<PackedScene>($"res://assets/DanAssets/enemies/{Body}Body.tscn").Instantiate<Node3D>();

            BodyNodes.Position += new Vector3(0, Marker, 0);

            AddChild(BodyNodes);

            if (Engine.IsEditorHint())
            {
                BodyNodes.Owner = this;
            }

        }
       
        if (lastBackWeapon != BackWeapon)
        {

            switch (BackWeapon)
            {
                case 0:
                    BodyNodes.GetNode<Node3D>("backweapons/missile").Visible = true;
                    BodyNodes.GetNode<Node3D>("backweapons/railcannon").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/shouldercannon").Visible = false;
                    break;
                case 1:
                    BodyNodes.GetNode<Node3D>("backweapons/missile").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/railcannon").Visible = true;
                    BodyNodes.GetNode<Node3D>("backweapons/shouldercannon").Visible = false;
                    break;
                case 2:
                    BodyNodes.GetNode<Node3D>("backweapons/missile").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/railcannon").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/shouldercannon").Visible = true;
                    break;
                default:
                    BodyNodes.GetNode<Node3D>("backweapons/missile").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/railcannon").Visible = false;
                    BodyNodes.GetNode<Node3D>("backweapons/shouldercannon").Visible = false;
                    break;
            }

        }

        if (lastHandWeapon != HandWeapon)
        {
            GD.Print("Difference Weapons");

            switch (HandWeapon)
            {
                case 0:
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/rifle").Visible = true;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/smg").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/zook").Visible = false;
                    break;
                case 1:
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/rifle").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/smg").Visible = true;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/zook").Visible = false;
                    break;
                case 2:
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/rifle").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/smg").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/zook").Visible = true;
                    break;
                default:
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/rifle").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/smg").Visible = false;
                    BodyNodes.GetNode<Node3D>("shouldercuff_l/Skeleton3D/right/hand/zook").Visible = false;
                    break;
            }
        }


        if(BodyNodes != null && !BodyNodes.IsQueuedForDeletion())
        {
            HandMarker = BodyNodes.GetNode<Marker3D>("shouldercuff_l/Skeleton3D/right/hand/spawner");
            BackMarker = BodyNodes.GetNode<Marker3D>("backweapons/spawner");
        }


        BodyNodes.Position = new Vector3(0, Marker, 0);


        //GetAnimationTree().Set("parameters/movement/blend_position", MovementBlendPosition);

        lastLeg = Leg;
        lastBody = Body;
        lastBackWeapon = BackWeapon; 
        lastHandWeapon = HandWeapon;

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
        return ATree;
    }

    public override Node3D GetCastPoint()
    {
        return GetNode<Marker3D>("foot_l/Skeleton3D/BoneAttachment3D/playerarm/rifle/spawner");
    }
}
