using Godot;
using MMOTest.scripts.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class ServerMenu : Node2D
{
	UniversalConnector connector;
	Tree tree;
	Timer timer;
	int PORT = 9001;
	[Export]
	string IPAddress = "0.0.0.0";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		connector = new UniversalConnector(IPAddress, 9999);
		tree = this.GetNode<Tree>("Control/Panel/Tree");
        tree.CreateItem();
		tree.HideRoot = true;
		updateTree();
    }


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
		
	}

	public void updateTree()
	{

		tree.Clear();
		tree.CreateItem();

        List<string> serverList = connector.Browse();
		if (serverList.Count < 1)
		{
			return;
		}
		foreach (string server in serverList)
		{
			string[] strings = server.Split(new char[] { ' ' }, 2);
			TreeItem ti = tree.CreateItem();
            ti.SetText(0, strings[0]);
            ti.SetText(1, strings[1]);
        }
    }

	public void _on_join_pressed()
	{
		if (tree.GetSelected() is not null)
		{
			TreeItem selection = tree.GetSelected();
			string ip = connector.Join(selection.GetText(0));
            MainLevel tL = GD.Load<PackedScene>("res://scenes/levels/MainLevel.tscn").Instantiate<MainLevel>();
			ConnectionManager.GetInstance().ServerAddress = ip;
            this.GetParent<GameLoop>().PushScene(tL);
        } else if (this.GetNode<TextEdit>("Control/TextEdit").Text.Length > 0)
		{
            string ip = this.GetNode<TextEdit>("Control/TextEdit").Text;
            MainLevel tL = GD.Load<PackedScene>("res://scenes/levels/MainLevel.tscn").Instantiate<MainLevel>();
            ConnectionManager.GetInstance().ServerAddress = ip;
            this.GetParent<GameLoop>().PushScene(tL);
        }
	}

	public void _on_back_pressed()
	{
        this.GetParent<GameLoop>().PushScene(ResourceLoader.Load<PackedScene>("res://scenes/menu/MainMenu.tscn").Instantiate());
    }


    public void _on_refresh_pressed()
	{
		updateTree();
	}

}
