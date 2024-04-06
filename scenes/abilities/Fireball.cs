using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public partial class Fireball : AbstractAbility
{

    int SourceActorID = -1;
    public override void Initialize(JObject obj)
    {
        Vector3 position = new Vector3((float)obj.Property("posx"), (float)obj.Property("posy"), (float)obj.Property("posz"));
        Vector3 velocity = new Vector3((float)obj.Property("velx"), (float)obj.Property("vely"), (float)obj.Property("velz"));
        SourceActorID = (int)obj.Property("SourceID");
        this.Position = position + velocity * 2;
        this.LinearVelocity = velocity;
    }

    public override void ApplyHost(bool Host)
    {
        this.GetNode<Area3D>("Area3D").Monitoring = Host;
        this.GetNode<Area3D>("Area3D").Monitorable = Host;
    }

    public override void SetVisible(bool Visible)
    {
        this.Visible = Visible;
    }

    public void _on_area_3d_body_entered(Node3D node)
    {
        this.QueueFree();
        //this will dispose once this function is done

        JObject m = new JObject
        {
            { "type", "cast"},
            { "spell", "FireballExplosion"},
            { "posx", this.Position.X },
            { "posy", this.Position.Y },
            { "posz", this.Position.Z },
            { "SourceID", SourceActorID}
        };
        MessageQueue.GetInstance().AddMessage(m);

       
        AbstractModel target = node as AbstractModel;
        if(target != null)
        {
            StatBlock sourceBlock = StatManager.GetInstance().GetStatBlock(SourceActorID);
            StatBlock targetBlock = StatManager.GetInstance().GetStatBlock(target.GetActorID());
            int TargetID = target.GetActorID();
            //has base damage, and scales off intelligence
            //going to calculate the message here, ONLY SEND DELTA DATA

            float nextHealth = targetBlock.GetStat(StatType.HEALTH) - sourceBlock.GetStat(StatType.ABILITY_POINTS) - 5;
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

}
