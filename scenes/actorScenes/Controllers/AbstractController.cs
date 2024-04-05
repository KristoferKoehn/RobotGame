using Godot;
using System;

public abstract partial class AbstractController : Node3D
{
    public abstract void ApplyImpulse(Vector3 vec);
}
