using Godot;
using System;

public abstract partial class AbstractModel : CharacterBody3D
{
	public bool IsDead { get; set; } = false;
    public long SimulationPeerId { get; set; } = -2;

    // Called when the node enters the scene tree for the first time.
    public abstract void AttachController(AbstractController controller);
	public abstract void SetTrackingPeerId(long peerId);
	public abstract void SetActorID(int actorId);
	public abstract int GetActorID();
	public abstract long GetTrackingPeerId();
	public abstract void ApplyImpulse(Vector3 vec);
	public abstract MultiplayerSynchronizer GetMultiplayerSynchronizer();
	public abstract AnimationPlayer GetAnimationPlayer();

	public abstract AnimationTree GetAnimationTree();

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public abstract void AssignDeathState(bool isdead);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public abstract void MovePlayerToPosition(Vector3 globalPosition);

}
