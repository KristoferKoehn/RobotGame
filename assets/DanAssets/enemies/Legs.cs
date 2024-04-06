using Godot;
using System;

public partial class Legs : Node3D
{
    [Export] public Node3D Marker { get; set; }
    [Export] public AnimationPlayer Player { get; set; }

}
