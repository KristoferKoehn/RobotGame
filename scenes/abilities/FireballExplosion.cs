using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

public partial class FireballExplosion : AbstractAbility
{

    [Export]
    private float ExplosionSpeed = 1.7f;
    public int SourceActorID = -1;
    bool host;

    public override void _Ready()
    {
        Tween t = CreateTween();
        Tween G = CreateTween();
        t.TweenProperty(this.GetNode<MeshInstance3D>("MeshInstance3D"), "scale", new Vector3(20,20,20), ExplosionSpeed);
        SphereShape3D sphereShape3D = (SphereShape3D)GetNode<CollisionShape3D>("Area3D/CollisionShape3D").Shape;
        G.TweenProperty(sphereShape3D, "radius", 6.06f, ExplosionSpeed);
        
        if (host)
        {
            t.Finished += QueueFree;
        }
        
        t.Play();
        G.Play();
    }

    public override void Initialize(JObject obj)
    {
        this.Position = new Vector3((float)obj.Property("posx"), (float)obj.Property("posy"), (float)obj.Property("posz"));
        if (obj.ContainsKey("SourceID"))
        {
            SourceActorID = (int)obj.Property("SourceID");
        }
    }

    public override void ApplyHost(bool Host)
    {
        this.GetNode<Area3D>("Area3D").Monitoring = true;
        this.GetNode<Area3D>("Area3D").Monitorable = true;
        host = Host;
    }

    public override void SetVisible(bool Visible)
    {
        this.Visible = Visible;
    }

    public void _on_area_3d_body_entered(Node3D node)
    {
        
        if(node is AbstractModel)
        {

            if (host)
            {

                AbstractModel target = node as AbstractModel;
                if (target != null)
                {
                    StatBlock sourceBlock = StatManager.GetInstance().GetStatBlock(SourceActorID);
                    StatBlock targetBlock = StatManager.GetInstance().GetStatBlock(target.GetActorID());
                    int TargetID = target.GetActorID();
                    //has base damage, and scales off intelligence
                    //going to calculate the message here, ONLY SEND DELTA DATA

                    float nextHealth = targetBlock.GetStat(StatType.HEALTH) - sourceBlock.GetStat(StatType.ABILITY_POINTS)/2.0f - 1;
                    float delta = nextHealth - targetBlock.GetStat(StatType.HEALTH);
                    JObject b = new JObject
                    {
                        { "type", "statchange" },
                        { "TargetID", TargetID },
                        { "SourceID", SourceActorID },
                    };

                    List<StatProperty> values = new List<StatProperty>
                    {
                        new StatProperty(StatType.HEALTH, delta)
                    };

                    b["stats"] = JsonConvert.SerializeObject(values);
                    MessageQueue.GetInstance().AddMessage(b);
                }
            }


            ((AbstractModel)node).ApplyImpulse((node.Position + new Vector3(0, 1.0f, 0) - this.Position).Normalized() * 60000);

        }
    }
}
