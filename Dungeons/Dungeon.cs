using Godot;
using System.Collections.Generic;

public partial class Dungeon : Control
{
	private TextureRect _roomImage;
	private Label _roomName;
	private Label _roomDescription;
	private Button _exploreButton;
	private Button _townButton;
	private Button _combatButton;
	
	private GameState _gameState;

	private System.Random _rng = new System.Random();

	public override void _Ready()
	{
		_roomImage       = GetNode<TextureRect>("RoomImage");
		_roomName        = GetNode<Label>("RoomPanel/VBoxContainer/RoomName");
		_roomDescription = GetNode<Label>("RoomPanel/VBoxContainer/RoomDescription");
		_exploreButton   = GetNode<Button>("ActionsPanel/ButtonRow/ExploreButton");
		_townButton      = GetNode<Button>("ActionsPanel/ButtonRow/TownButton");
		_combatButton    = GetNode<Button>("ActionsPanel/ButtonRow/CombatButton");
		
		_combatButton.Pressed += TryCombat;
		
		_gameState = GetNode<GameState>("/root/GameState");

		// Debug: auto form party if empty
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
		AutoFormParty();
		
		// Enter the dungeon
		var room = DungeonManager.EnterDungeon("SurfaceRuins", _gameState);
		DisplayRoom(room);
	}

	private void DebugLayout()
	{
		var viewport = GetViewport().GetVisibleRect();
		GD.Print($"Viewport size: {viewport.Size}");

		var roomPanel    = GetNode<ColorRect>("RoomPanel");
		var actionsPanel = GetNode<PanelContainer>("ActionsPanel");
		var roomImage    = GetNode<TextureRect>("RoomImage");

		GD.Print($"RoomPanel rect: {roomPanel.GetRect()}");
		GD.Print($"RoomPanel bottom edge: {roomPanel.GlobalPosition.Y + roomPanel.Size.Y}");

		GD.Print($"ActionsPanel rect: {actionsPanel.GetRect()}");
		GD.Print($"ActionsPanel bottom edge: {actionsPanel.GlobalPosition.Y + actionsPanel.Size.Y}");

		GD.Print($"RoomImage rect: {roomImage.GetRect()}");
		GD.Print($"RoomImage bottom edge: {roomImage.GlobalPosition.Y + roomImage.Size.Y}");

		GD.Print($"HUD top edge should be: {viewport.Size.Y * 0.66f}");
		
		GD.Print($"Visible rect: {GetViewport().GetVisibleRect()}");
		GD.Print($"Canvas transform: {GetViewport().GetFinalTransform()}");
		
		GetNode<PartyHUD>("/root/PartyHud").DebugLayout();
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
			new List<string> { "Skeleton", "Skeleton", "Skeleton" }, // back row
			new List<string> { "Skeleton", "Skeleton" },             // middle row
			new List<string> { "Skeleton" }                          // front row
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

	private void TryCombat()
	{
		var room      = _gameState.CurrentRoom;
		var dungeon   = _gameState.CurrentDungeon;
		var state     = _gameState.GetDungeonState(dungeon);

		EncounterData encounter = null;

		if (DebugFlags.ForceEncounter)
		{
			var enc = EncounterLoader.LoadEncounter(
				_gameState.CurrentDungeon, "SkeletonGuards");
			if (enc != null)
			{
				_gameState.CurrentEncounter     = enc.Formation;
				_gameState.CurrentEncounterData = enc;
				GetNode<Main>("/root/Main").CallDeferred(
					nameof(Main.SwitchScene), "res://Combat/Combat.tscn");
			}
			return;
		}
		
		// Roll room encounters first — skip already completed ones
		if (room?.Encounters != null)
		{
			foreach (var entry in room.Encounters)
			{
				// Skip if already completed this run
				if (state.CompletedEncounters.Contains($"{room.Id}_{entry.Id}"))
					continue;

				if (_rng.NextDouble() < entry.Chance)
				{
					encounter = EncounterLoader.LoadEncounter(dungeon, entry.Id);
					if (encounter != null)
					{
						// Mark completed
						state.CompletedEncounters.Add($"{room.Id}_{entry.Id}");
						GD.Print($"Room encounter: {encounter.Name}");
						break;
					}
				}
			}
		}

		// If no room encounter triggered, roll wandering monsters
		if (encounter == null)
		{
			encounter = EncounterLoader.RollWanderingEncounter(dungeon, _rng);
			if (encounter != null)
				GD.Print($"Wandering encounter: {encounter.Name}");
		}

		if (encounter == null)
		{
			GD.Print("No encounter this room.");
			return;
		}

		// Store encounter in GameState and switch to combat
		_gameState.CurrentEncounter = encounter.Formation;
		_gameState.CurrentEncounterData = encounter;
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Combat/Combat.tscn");
	}
}
