using Godot;

public partial class Town : Control
{
	public override void _Ready()
	{	
		var gameState = GetNode<GameState>("/root/GameState");
				
		GD.Print("Welcome to town");
	}
	
	private void _on_roster_pressed()
	{
		var main = GetNode<Main>("/root/Main");
		main.CallDeferred(nameof(main.SwitchScene), "res://Town/Roster/Roster.tscn");
	}

	private void _on_dungeon_pressed()
	{
		GetNode<Main>("/root/Main").CallDeferred(nameof(Main.SwitchScene), "res://Dungeons/Dungeon.tscn");
	}
}
