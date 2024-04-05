using Godot;
using MMOTest.scripts.Managers;
using System;
using System.Collections.Generic;

public enum Teams
{
    NO_TEAM,
    RED_TEAM,
    BLUE_TEAM,
    GREEN_TEAM,
    YELLOW_TEAM,
    PURPLE_TEAM,
    ORANGE_TEAM
}

[Tool]
public partial class SpawnArea : Area3D
{
    /*
     * This is the spawn area scene. Edge cases still exist.
     * 
     * 1. Check to see if there is statistically a valid place to spawn in the area:
     * we can assume that the players need about 2 square meters of space to spawn. If there are more than x*z/2 models in this area, 
     * it is not valid.
     * 
     * 2. What if someone is on the edge of the spawn within the 0.8f meter buffer distance? 
     * then a point in that range can be returned. I could put a boundary of 0.8f on the edge but I didn't.
     * 
     * there are other considerations that need to be taken outside this class when spawning things.
     * 1. we need to detect threat on spawn areas in pvp, is an enemy in the area? is there an ability already on spawn?
     * right now it's just the wild west in there. No spawn disabling at all. 
     * 
     * more investigation needs to be done on this system, the vast majority of behaviors are still unknown.
     */

    List<AbstractModel> CurrentOccupiers = new List<AbstractModel>();
    [Export]
    public Teams Team { get; set; }
    [Export]
    BoxShape3D Shape { get; set; }


    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        SpawnManager.GetInstance().AddSpawnArea(this);
        GetNode<CollisionShape3D>("CollisionShape3D").Shape = Shape;
    }


    public override void _Process(double delta)
    {
        //tool script to allow 
        if (Engine.IsEditorHint())
        {
            if (GetNode<CollisionShape3D>("CollisionShape3D").Shape != Shape)
            {
                GetTree().EditedSceneRoot.GetNode<CollisionShape3D>("CollisionShape3D").Shape = Shape;
            }
        }
    }

    /// <summary>
    /// gets a random point at "ground level" of the spawn area
    /// </summary>
    /// <returns>A position in global space a certain distance away from the edges and from any other player object inside the area.</returns>
    public Vector3 GetValidSpawnPoint()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        Vector3 p = new Vector3(); 

        bool cleared = false;
        while(!cleared)
        {
            float x = rng.Randf() % Shape.Size.X;
            x = x - Shape.Size.X / 2f;
            float z = rng.Randf() % Shape.Size.Z;
            z = z - Shape.Size.Z / 2f;
            p = new Vector3(x, 0, z);
            p = ToGlobal(p);

            cleared = true;
            foreach (AbstractModel model in CurrentOccupiers) { 
                if (p.DistanceTo(model.GlobalPosition) < 0.8f)
                {
                    cleared = false;
                }
            }
        }

        // Test
        p.Y = 0f;
        return p;
    }

    public void _on_body_entered(Node3D node3D)
    {
        AbstractModel model = node3D as AbstractModel;
        if (model != null)
        {
            CurrentOccupiers.Add(model);
        }
    }

    public void _on_body_exited(Node3D node3D)
    {
        AbstractModel model = node3D as AbstractModel;
        if (model != null)
        {
            CurrentOccupiers.Remove(model);
        }
    }

    public void _on_tree_exiting()
    {
        if (!Engine.IsEditorHint())
        {
            SpawnManager.GetInstance().RemoveSpawnArea(this);
        }
        
    }

}
