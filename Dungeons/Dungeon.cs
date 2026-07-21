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

	public override void _Ready()
	{
		_roomImage       = GetNode<TextureRect>("RoomImage");
		_roomName        = GetNode<Label>("RoomPanel/VBoxContainer/RoomName");
		_roomDescription = GetNode<Label>("RoomPanel/VBoxContainer/RoomDescription");
		_moveButton      = GetNode<Button>("ActionsPanel/ButtonRow/MoveButton");
		_townButton      = GetNode<Button>("ActionsPanel/ButtonRow/TownButton");
		_combatButton    = GetNode<Button>("ActionsPanel/ButtonRow/CombatButton");

		_combatButton.Pressed += TryCombat;
		_moveButton.Pressed   += OnMovePressed;

		_gameState = GetNode<GameState>("/root/GameState");

		_moveMenu = new MoveMenu();
		_moveMenu.Visible = false;
		_moveMenu.ZIndex  = 20;
		AddChild(_moveMenu);
		_moveMenu.RoomChosen += OnMoveToRoom;
		_moveMenu.Cancelled  += OnMoveCancelled;

		// Debug: auto form party if empty
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
			AutoFormParty();

		// Ability menu
		var abilityMenu = new AbilityMenu();
		abilityMenu.Visible = false;
		abilityMenu.ZIndex  = 25;
		AddChild(abilityMenu);
		DungeonAbilityUse.Menu = abilityMenu;

		// Enter the dungeon
		DungeonManager.EnterDungeon("DwarvenBrewery", _gameState);
		RefreshRoomDisplay();
	}

	// Single source of truth for drawing the current room
	private void RefreshRoomDisplay()
	{
		var room = _gameState.CurrentRoom;
		if (room == null) return;

		_roomName.Text        = room.Name;
		_roomDescription.Text = room.GetDescriptionText();

		if (!string.IsNullOrEmpty(room.Image) && ResourceLoader.Exists(room.Image))
			_roomImage.Texture = GD.Load<Texture2D>(room.Image);
		else
			_roomImage.Texture = null;
	}

	// --- Movement ---

	private void OnMovePressed()
	{
		var dungeon = _gameState.CurrentDungeon;
		var state   = _gameState.GetDungeonState(dungeon);
		if (state?.Map == null) return;

		// Cheater menu: every room in the graph, flagged explored/unexplored
		var rooms = new List<(string id, string name, bool explored)>();
		foreach (var roomId in state.Map.AllRoomIds)
		{
			var room     = DungeonManager.LoadRoom(dungeon, roomId);
			bool visited = state.ExploredRooms.Contains(roomId);
			rooms.Add((roomId, room?.Name ?? roomId, visited));
		}

		_moveMenu.Open(rooms, _gameState.CurrentRoom?.Id ?? "");
	}

	private void OnMoveToRoom(string roomId)
	{
		_moveMenu.Visible = false;

		var room = DungeonManager.MoveToRoom(_gameState, roomId);
		if (room == null) return;

		TickExplorationCooldowns();
		RefreshRoomDisplay();
	}

	private void OnMoveCancelled() => _moveMenu.Visible = false;

	// --- Combat ---

	private void TryCombat()
	{
		var dungeon = _gameState.CurrentDungeon;
		var state   = _gameState.GetDungeonState(dungeon);
		var roomId  = _gameState.CurrentRoom?.Id;
		if (string.IsNullOrEmpty(roomId)) return;

		var instance = state.Encounters.GetRoomEncounter(roomId);

		if (instance == null && DebugFlags.ForceEncounter)
		{
			instance = state.Encounters.CreateInstance(
				dungeon, DebugFlags.ForcedEncounterId, roomId, EncounterAttachment.Permanent);
			GD.Print($"[DEBUG] Forced encounter: {instance?.InstanceId ?? "FAILED"}");
		}

		if (instance == null)
		{
			GD.Print("No encounter here.");
			return;
		}

		_gameState.CurrentEncounterInstance = instance;
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Combat/Combat.tscn");
	}

	// --- Town ---

	private void _on_town_button_pressed()
	{
		_gameState.ReturnToTown();
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Town/Town.tscn");
	}

	// --- Helpers ---

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

	private void AutoFormParty()
	{
		var rng = new System.Random();
		var available = new List<Character>(_gameState.Stable);

		int n = available.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			(available[k], available[n]) = (available[n], available[k]);
		}

		int count = System.Math.Min(6, available.Count);
		for (int i = 0; i < count; i++)
			_gameState.AddToParty(available[i]);

		GD.Print($"Debug: Auto-formed party with {_gameState.Party.Count} members");
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
	}
}
