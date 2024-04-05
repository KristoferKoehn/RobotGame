using Godot;
using System;

public partial class MainMenu : Node3D
{

    public void _on_solo_pressed()
    {
        this.GetParent<GameLoop>().PushScene(ResourceLoader.Load<PackedScene>("res://scenes/levels/testLevelWithCharacter.tscn").Instantiate());
    }

    public void _on_connect_pressed()
    {
        this.GetParent<GameLoop>().PushScene(ResourceLoader.Load<PackedScene>("res://scenes/menu/networkMenu/ServerMenu.tscn").Instantiate());
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
