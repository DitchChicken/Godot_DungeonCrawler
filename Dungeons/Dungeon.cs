using Godot;
using System.Collections.Generic;

public partial class Dungeon : Control
{
	private TextureRect _roomImage;
	private Label _roomName;
	private Label _roomDescription;
	private Button _moveButton;
	private Button _townButton;
	private Button _combatButton;
	private MoveMenu _moveMenu;
	
	private GameState _gameState;

	private System.Random _rng = new System.Random();

	public override void _Ready()
	{
		_roomImage       = GetNode<TextureRect>("RoomImage");
		_roomName        = GetNode<Label>("RoomPanel/VBoxContainer/RoomName");
		_roomDescription = GetNode<Label>("RoomPanel/VBoxContainer/RoomDescription");
		_moveButton      = GetNode<Button>("ActionsPanel/ButtonRow/MoveButton");
		_townButton      = GetNode<Button>("ActionsPanel/ButtonRow/TownButton");
		_combatButton    = GetNode<Button>("ActionsPanel/ButtonRow/CombatButton");
		
		_combatButton.Pressed += TryCombat;
		
		_gameState = GetNode<GameState>("/root/GameState");

		_moveMenu = new MoveMenu();
		_moveMenu.Visible = false;
		_moveMenu.ZIndex  = 20;
		AddChild(_moveMenu);
		_moveMenu.RoomChosen    += OnMoveToRoom;
		_moveMenu.ExploreChosen += OnExploreChosen;
		_moveMenu.Cancelled     += OnMoveCancelled;

		// Rewire the (renamed) Move button
		_moveButton.Pressed += OnMovePressed;
		
		// Debug: auto form party if empty
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
		AutoFormParty();
		
		// Ability menu
		var abilityMenu = new AbilityMenu();
		abilityMenu.Visible = false;
		abilityMenu.ZIndex  = 25;
		AddChild(abilityMenu);

		DungeonAbilityUse.Menu             = abilityMenu;
		DungeonAbilityUse.TargetController = GetNode<DungeonTargetController>("DungeonTargetController");
		
		// Enter the dungeon
		var room = DungeonManager.EnterDungeon("DwarvenBrewery", _gameState);
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
		_roomDescription.Text = room.GetDescriptionText();
		
		if (!string.IsNullOrEmpty(room.Image)
			&& ResourceLoader.Exists(room.Image))
			_roomImage.Texture = GD.Load<Texture2D>(room.Image);
		else
			_roomImage.Texture = null;
	}

	private void DoExplore()
	{		
		var room = DungeonManager.Explore(_gameState);
		if (room == null) return;	
		
		TickExplorationCooldowns();
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
	
	private void OnMovePressed()
	{
		var dungeon = _gameState.CurrentDungeon;
		var state   = _gameState.GetDungeonState(dungeon);

		// Build (id, name) list of explored rooms
		var rooms = new System.Collections.Generic.List<(string, string)>();
		foreach (var roomId in state.ExploredRooms)
		{
			var room = DungeonManager.LoadRoom(dungeon, roomId);
			string name = room?.Name ?? roomId;
			rooms.Add((roomId, name));
		}

		bool canExplore = DungeonManager.CanExplore(_gameState);
		string currentId = _gameState.CurrentRoom?.Id ?? "";

		_moveMenu.Open(rooms, canExplore, currentId);
	}

	private void OnMoveToRoom(string roomId)
	{
		_moveMenu.Visible = false;

		var dungeon = _gameState.CurrentDungeon;
		var room    = DungeonManager.LoadRoom(dungeon, roomId);
		if (room == null) return;

		_gameState.CurrentRoom = room;
		TickExplorationCooldowns();
		
		// Update last-room pointer for consistency
		var state = _gameState.GetDungeonState(dungeon);
		state.LastRoomId = roomId;

		RefreshRoomDisplay();   // your existing method that redraws the room
		// Revisiting does NOT re-roll encounters (they were completed on first visit)
	}

	private void OnExploreChosen()
	{
		_moveMenu.Visible = false;
		DoExplore();   // your existing explore logic (the old button handler body)
	}

	private void OnMoveCancelled()
	{
		_moveMenu.Visible = false;
	}
	
	private void RefreshRoomDisplay()
	{
		var room = _gameState.CurrentRoom;
		if (room == null) return;

		// Room name
		_roomName.Text = room.Name;

		// Description (joined from the string array)
		_roomDescription.Text = room.GetDescriptionText();

		// Room image
		if (!string.IsNullOrEmpty(room.Image) && ResourceLoader.Exists(room.Image))
			_roomImage.Texture = GD.Load<Texture2D>(room.Image);
		else
			_roomImage.Texture = null;

		// Update Move button availability — always allow Move (revisit is always possible
		// once you've explored at least the entry room), but Explore Further depends on pool
		_moveButton.Disabled = false;
	}
	
	private void TickExplorationCooldowns()
	{
		foreach (var c in _gameState.Party)
		{
			var keys = new List<string>(c.ExplorationCooldowns.Keys);
			foreach (var key in keys)
			{
				if (c.ExplorationCooldowns[key] > 0)
					c.ExplorationCooldowns[key]--;
			}
		}
	}
}
