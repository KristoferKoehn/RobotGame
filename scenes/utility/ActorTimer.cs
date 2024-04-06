using Godot;
using MMOTest.Backend;
using System;

public partial class ActorTimer : Timer
{

	[Signal] 
	public delegate void ActorTimerTimeoutEventHandler(int ActorID);

	public int ActorID { get; set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Timeout += TimerTimeout;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void TimerTimeout()
	{
		EmitSignal(SignalName.ActorTimerTimeout, ActorID);
	}
}
