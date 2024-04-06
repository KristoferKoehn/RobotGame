using Godot;
using System;

public partial class Bodies : Node3D
{
    [Export] public Node3D Rifle { get; set; }
    [Export] public Node3D Smg { get; set; }
    [Export] public Node3D Bazooka { get; set; }
    [Export] public Node3D Missile { get; set; }
    [Export] public Node3D ShoulderCannon { get; set; }
    [Export] public Node3D RailCannon { get; set; }
    [Export] public Marker3D Handspawner { get; set; }
    [Export] public Marker3D Backspawner { get; set; }

}
