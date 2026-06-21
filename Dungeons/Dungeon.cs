using Godot;
using System.Collections.Generic;

public partial class Dungeon : Control
{
	private TextureRect _roomImage;
	private Label _roomName;
	private Label _roomDescription;
	private Button _exploreButton;
	private Button _townButton;

	private GameState _gameState;

	public override void _Ready()
	{
		_roomImage       = GetNode<TextureRect>("MainContainer/RoomImage");
		_roomName        = GetNode<Label>("MainContainer/RoomPanel/VBoxContainer/RoomName");
		_roomDescription = GetNode<Label>("MainContainer/RoomPanel/VBoxContainer/RoomDescription");
		_exploreButton   = GetNode<Button>("MainContainer/ActionsPanel/ButtonRow/ExploreButton");
		_townButton      = GetNode<Button>("MainContainer/ActionsPanel/ButtonRow/TownButton");

		_gameState = GetNode<GameState>("/root/GameState");

		// Debug: auto form party if empty
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
		AutoFormParty();
		
		// Enter the dungeon
		var room = DungeonManager.EnterDungeon("SurfaceRuins", _gameState);
		DisplayRoom(room);
	}

	private void DisplayRoom(RoomData room)
	{
		if (room == null) return;

		_roomName.Text = room.Name;
		_roomDescription.Text = room.Description;
		
		if (!string.IsNullOrEmpty(room.Image)
			&& ResourceLoader.Exists(room.Image))
			_roomImage.Texture = GD.Load<Texture2D>(room.Image);
		else
			_roomImage.Texture = null;

		// Gray out explore button if pool is empty
		var state = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		_exploreButton.Disabled = state.RoomPool.Count == 0;
	}

	private void _on_explore_button_pressed()
	{
		GD.Print("Explore pressed");
		var room = DungeonManager.Explore(_gameState);
		if (room == null) return;
		DisplayRoom(room);
	}

	private void _on_town_button_pressed()
	{
		_gameState.ReturnToTown();
		GetNode<Main>("/root/Main").CallDeferred(nameof(Main.SwitchScene), "res://Town/Town.tscn");
	}

	private void _on_combat_button_pressed()
	{
		// Hardcoded test encounter for now
		var encounter = new List<List<string>>
		{
			new List<string> { "skeleton", "skeleton", "skeleton" }, // back row
			new List<string> { "skeleton", "skeleton" },             // middle row
			new List<string> { "skeleton" }                          // front row
		};

		_gameState.SetEncounter(encounter);
		GetNode<Main>("/root/Main").CallDeferred(nameof(Main.SwitchScene), "res://Combat/Combat.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		/*
		if (@event is InputEventKey key
			&& key.Pressed
			&& key.Keycode == Key.Escape)
		{
			_gameState.ReturnToTown();
			GetNode<Main>("/root/Main").CallDeferred(nameof(Main.SwitchScene), "res://Town/Town.tscn");
			GetViewport().SetInputAsHandled();
		}
		*/
	}	
	
	private void AutoFormParty()
	{
		var rng = new System.Random();
		var available = new System.Collections.Generic.List<Character>(_gameState.Stable);

		// Shuffle
		int n = available.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			(available[k], available[n]) = (available[n], available[k]);
		}

		// Take up to 6
		int count = System.Math.Min(6, available.Count);
		for (int i = 0; i < count; i++)
			_gameState.AddToParty(available[i]);

		GD.Print($"Debug: Auto-formed party with {_gameState.Party.Count} members");

		// Refresh HUD
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
	}

}
