using Godot;
using MMOTest.Backend;
using Newtonsoft.Json.Linq;
using System;

public abstract partial class AbstractAbility : RigidBody3D
{
    public abstract void Initialize(JObject obj);
    public abstract void ApplyHost(bool Host);
    public abstract void SetVisible(bool Visible);
}
