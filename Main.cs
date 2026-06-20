using Godot;

public partial class Main : Node
{
	public override void _Ready()
	{
		CallDeferred(nameof(SwitchScene), "res://Town/Town.tscn");
	}

	public void SwitchScene(string scenePath)
	{
		GetTree().ChangeSceneToFile(scenePath);
	}
}
