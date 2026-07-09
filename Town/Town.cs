using Godot;

public partial class Town : Control
{
	public override void _Ready()
	{	
		var gameState = GetNode<GameState>("/root/GameState");
				
		GD.Print("Welcome to town");
		
		if (gameState.Stable.Count > 0)
		{
			var c = gameState.Stable[0];
			/*
			GD.Print($"--- {c.Name} Equipment ---");
			foreach (var kvp in c.EquippedItems)
				GD.Print($"  {kvp.Key}: {kvp.Value.Name} (AC:{kvp.Value.ArmorClass} DMG:{kvp.Value.BaseDamageMin}-{kvp.Value.BaseDamageMax})");
			GD.Print($"  Total AC: {c.TotalArmorClass()}");
			*/
		}
		GetNode<Button>("ButtonPanel/VBoxContainer/Shop").Pressed += OnShopPressed;
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
	
	private void _on_vault_button_pressed()
	{
		GetNode<Main>("/root/Main")
			.CallDeferred(nameof(Main.SwitchScene), "res://Town/Vault/Vault.tscn");
	}	
	
	private void OnShopPressed()
	{
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Town/Shop/Shop.tscn");
	}
}
