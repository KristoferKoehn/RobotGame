using Godot;
using System;

public partial class NotificationLabel : Label
{

    public override void _Ready()
    {
        Tween t = GetTree().CreateTween();
        t.TweenProperty(this, "global_position", this.GlobalPosition + new Vector2(0, -80), 2);
        t.Finished += this.QueueFree;
        t.Play();
    }



}
