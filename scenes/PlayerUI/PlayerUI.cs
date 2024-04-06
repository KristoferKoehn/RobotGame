using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;

public partial class PlayerUI : CanvasLayer
{

	ProgressBar progressBar;

	TextureProgressBar textureProgressBarOver;
	TextureProgressBar textureProgressBarUnder;


	public int ActorID;
	bool initialized = false;


	public void initialize(int ActorID)
	{
		this.ActorID = ActorID;
        initialized = true;
		GetNode<Label>("ActorID").Text = "ActorID: " + ActorID;
		this.Name = ActorID.ToString();
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		textureProgressBarOver = GetNode<TextureProgressBar>("Control/TextureProgressBar");
        textureProgressBarUnder = GetNode<TextureProgressBar>("Control/TextureProgressBar2");
        UIManager.GetInstance().RegisterUI(ActorID, this);
		UIManager.GetInstance().RpcId(1, "RegisterActor", ActorID);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!initialized)
		{
			return;
		}
		StatBlock sb = StatManager.GetInstance().GetStatBlock(ActorID);
		float health = sb.GetStat(StatType.HEALTH);
		float maxHealth = sb.GetStat(StatType.MAX_HEALTH);


		Tween t1 = GetTree().CreateTween();
		t1.TweenProperty(textureProgressBarOver, "value", health, 0.1);
		t1.Play();

		Tween t2 = GetTree().CreateTween();
        t2.TweenProperty(textureProgressBarUnder, "value", health, 0.4);
		t2.Play();

    }

	public Label MakeLabel(Vector2 pos)
	{
		Label label = new Label();
		label.GlobalPosition = pos;
		this.AddChild(label);
		return label;
	}

    public override void _Input(InputEvent @event)
    {

        if (Input.MouseMode != Input.MouseModeEnum.Captured && GetNode<SpawnPanel>("SpawnPanel").spawned)
        {
            InputEventMouseButton button = @event as InputEventMouseButton;
            if (button != null)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else
            {
                return;
            }
        }
    }
}
