using Godot;
using MMOTest.scripts.Managers;
using System;

public partial class SpawnPanel : Control
{

    String currentClass = null;
    Label displayLabel = null;
    PlayerUI playerUI = null;
    public bool spawned = false;

    public override void _Ready()
    {
        displayLabel = this.GetNode<Label>("Panel/ClassSelection");
        playerUI = this.GetParent<PlayerUI>();
    }

    public void _on_spawn_button_pressed()
    {
        spawned = true;

        GD.Print("spawn pressed, class: " + currentClass);
        SpawnManager.GetInstance().RpcId(1, "SpawnPlayer", currentClass, playerUI.ActorID);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        this.Visible = false;
    }

    public void _on_test_player_button_pressed()
    { 
        currentClass = "Test";
        displayLabel.Text = "Playing As: " + currentClass;
    }
}
