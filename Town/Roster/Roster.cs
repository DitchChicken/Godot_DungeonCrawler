using Godot;

public partial class Roster : Control
{
	private GridContainer _portraitGrid;
	private PackedScene _slotScene = GD.Load<PackedScene>("res://UI/PartySlot.tscn");

	public override void _Ready()
	{
		_portraitGrid = GetNode<GridContainer>("HBoxContainer/PortraitGrid");
		LoadPortraits();
		RefreshPartyStatus();
	}

	private void LoadPortraits()
	{
		var gameState = GetNode<GameState>("/root/GameState");

		// Clear existing slots
		foreach (Node child in _portraitGrid.GetChildren())
			child.QueueFree();

		// Always show Stable in fixed order
		foreach (var character in gameState.Stable)
		{
			var slot = _slotScene.Instantiate<PartySlot>();
			_portraitGrid.AddChild(slot);
			slot.SetCharacter(character);

			// Visually indicate if already in party
			if (gameState.Party.Contains(character))
				slot.SetInParty(true);
		}
	}

	private void AddPortraitSlot(Character character)
	{
		var slot = _slotScene.Instantiate<PartySlot>();
		_portraitGrid.AddChild(slot);
		slot.SetCharacter(character);
	}

	private void _on_town_pressed()
	{
		GetNode<CharacterSheet>("/root/CharacterSheet").Hide();
		var main = GetNode<Main>("/root/Main");
		main.CallDeferred(nameof(main.SwitchScene), "res://Town/Town.tscn");
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key
			&& key.Pressed
			&& key.Keycode == Key.Escape)
		{
			GetNode<CharacterSheet>("/root/CharacterSheet").Hide();
			GetNode<Main>("/root/Main").CallDeferred(nameof(Main.SwitchScene), "res://Town/Town.tscn");
			GetViewport().SetInputAsHandled();
		}
	}
	
	public void RefreshPartyStatus()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		foreach (var child in _portraitGrid.GetChildren())
		{
			if (child is PartySlot slot && slot.Character != null)
				slot.SetInParty(gameState.Party.Contains(slot.Character));
		}
	}
}
