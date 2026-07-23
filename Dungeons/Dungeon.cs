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
	private ScrollContainer _logScroll;
	private RichTextLabel  _logLabel;
	private List<string> _dungeonLog = new List<string>();
	private CompassWidget _compass;
	private VBoxContainer _actionsList;   // grab in _Ready
	private Label _timeLabel;
	private RoomLootPanel _lootPanel;
	
	private GameState _gameState;

	public override void _Ready()
	{
		_roomImage       = GetNode<TextureRect>("RoomImage");
		_roomName        = GetNode<Label>("RoomPanel/VBoxContainer/RoomName");
		_roomDescription = GetNode<Label>("RoomPanel/VBoxContainer/RoomDescription");
		_moveButton      = GetNode<Button>("ActionsPanel/ButtonRow/MoveButton");
		_townButton      = GetNode<Button>("ActionsPanel/ButtonRow/TownButton");
		_combatButton    = GetNode<Button>("ActionsPanel/ButtonRow/CombatButton");
		_actionsList     = GetNode<VBoxContainer>("ActionsPanel/ButtonRow/ActionList");
		
		_combatButton.Pressed += TryCombat;
		_moveButton.Pressed   += OnMovePressed;

		_compass = new CompassWidget();
		GetNode<VBoxContainer>("ActionsPanel/ButtonRow").AddChild(_compass);
		_compass.DirectionPressed += OnCompassDirection;

		_gameState = GetNode<GameState>("/root/GameState");

		_timeLabel = GetNode<Label>("TimeLabel");
		DungeonClock.Changed = RefreshTime;
		RefreshTime();

		_moveMenu = new MoveMenu();
		_moveMenu.Visible = false;
		_moveMenu.ZIndex  = 20;
		AddChild(_moveMenu);
		_moveMenu.RoomChosen += OnMoveToRoom;
		_moveMenu.Cancelled  += OnMoveCancelled;

		//Output to log
		_logScroll = GetNode<ScrollContainer>("RoomPanel/VBoxContainer/LogScroll");
		_logLabel  = GetNode<RichTextLabel>("RoomPanel/VBoxContainer/LogScroll/LogLabel");
		_logLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		// Register so other systems can log here
		DungeonLog.Sink = AddLogMessage;

		// Debug: auto form party if empty
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
			AutoFormParty();

		// Ability menu
		var abilityMenu = new AbilityMenu();
		abilityMenu.Visible = false;
		abilityMenu.ZIndex  = 25;
		AddChild(abilityMenu);
		DungeonAbilityUse.Menu = abilityMenu;

		_lootPanel = new RoomLootPanel();
		AddChild(_lootPanel);
		_lootPanel.Closed += RefreshActions;
		
		// Enter the dungeon
		DungeonManager.EnterDungeon("DwarvenBrewery", _gameState);
		RefreshRoomDisplay();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

		// Don't steal keys from the console or an open menu
		if (DebugConsole.Instance != null && DebugConsole.Instance.IsOpen) return;
		if (_moveMenu.Visible) return;

		Direction? dir = key.Keycode switch
		{
			Key.Up       => Direction.North,
			Key.Down     => Direction.South,
			Key.Left     => Direction.West,
			Key.Right    => Direction.East,
			Key.Pageup   => Direction.Up,
			Key.Pagedown => Direction.Down,
			_ => null
		};

		if (dir.HasValue)
		{
			TryMove(dir.Value);
			GetViewport().SetInputAsHandled();
		}
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

		var state = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		_compass?.Refresh(state?.Map?.GetRoom(room.Id));			
		RefreshActions();
		if (_lootPanel != null && _lootPanel.Visible) _lootPanel.Close();
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

		DungeonLog.Print($"The party moves to {room.Name}.", DungeonLog.Movement);		
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
	
	private void RefreshActions()
	{
		foreach (Node c in _actionsList.GetChildren()) c.QueueFree();

		var room = _gameState.CurrentRoom;
		if (room?.Actions == null) return;

		var state     = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		var roomState = state.GetRoomState(room.Id);

		var floorBtn = new Button();
		floorBtn.Text = roomState.HasLoot ? "Examine the Floor  (items here)" : "Examine the Floor";
		// Dimmed when empty, but still usable for dropping
		floorBtn.Modulate = roomState.HasLoot ? Colors.White : new Color(0.6f, 0.6f, 0.6f);
		floorBtn.Pressed += OnFloorPressed;
		_actionsList.AddChild(floorBtn);

		if (SearchController.CanSearch(roomState))
		{
			var searchBtn = new Button();
			searchBtn.Text = SearchController.LabelFor(room, roomState);
			searchBtn.Pressed += OnSearchPressed;
			_actionsList.AddChild(searchBtn);
		}
		
		foreach (var action in room.Actions)
		{
			if (!InteractionResolver.IsAvailable(action, _gameState, roomState)) continue;

			var btn = new Button();
			btn.Text = action.Name;
			var captured = action;
			btn.Pressed += () => OnActionPressed(captured);
			_actionsList.AddChild(btn);
		}
		
		_compass?.Refresh(state?.Map?.GetRoom(room.Id));
	}

	private void OnActionPressed(Interaction action)
	{
		var state     = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		var roomState = state.GetRoomState(_gameState.CurrentRoom.Id);

		InteractionResolver.Execute(action, _gameState, roomState);

		foreach (var msg in InteractionResolver.LastMessages)
			DungeonLog.Print(msg, DungeonLog.Interaction);

		RefreshActions();                                  // one-shots vanish
		GetNode<PartyHUD>("/root/PartyHud").Refresh();     // heal/damage outcomes
	}
	
	public void AddLogMessage(string message, Color? color = null)
	{
		if (string.IsNullOrWhiteSpace(message)) return;

		string line = color.HasValue
			? $"[color=#{color.Value.ToHtml(false)}]{message}[/color]"
			: message;

		_dungeonLog.Add(line);
		_logLabel.Text = string.Join("\n", _dungeonLog);
		CallDeferred(nameof(ScrollLogToBottom));
	}

	private async void ScrollLogToBottom()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var vbar = _logScroll.GetVScrollBar();
		_logScroll.ScrollVertical = (int)vbar.MaxValue;
	}
	
	public override void _ExitTree()
	{
		if (DungeonLog.Sink == AddLogMessage)
			DungeonLog.Sink = null;
			
		if (DungeonClock.Changed == RefreshTime) DungeonClock.Changed = null;
	}
	
	private void OnCompassDirection(int dir) => TryMove((Direction)dir);

	private void TryMove(Direction dir)
	{
		var state = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		var here  = state?.Map?.GetRoom(_gameState.CurrentRoom?.Id);
		var exit  = here?.GetExit(dir);

		if (exit == null || !exit.Discovered)
		{
			DungeonLog.Print($"There is no way {dir.ToString().ToLower()} from here.");
			return;
		}
		if (!exit.IsPassable)
		{
			string why = exit.State == ExitState.Locked ? "locked" : "blocked";
			DungeonLog.Print($"The way {dir.ToString().ToLower()} is {why}.", DungeonLog.Damage);
			return;
		}

		var room = DungeonManager.MoveThroughExit(_gameState, dir);
		if (room == null) return;

		DungeonClock.Advance(_gameState, exit.TravelTime, $"move {dir}");
		DungeonLog.Print($"The party moves {dir.ToString().ToLower()} to {room.Name}.",
						 DungeonLog.Movement);
		RefreshRoomDisplay();
	}
	
	private void RefreshTime()
	{
		_timeLabel.Text = DungeonClock.Format(DungeonClock.GetTicks(_gameState));
	}
	
	private void OnSearchPressed()
	{
		var room      = _gameState.CurrentRoom;
		var state     = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		var roomState = state.GetRoomState(room.Id);

		SearchController.Execute(room, roomState, _gameState);

		RefreshActions();                                  // relabel or hide the button
		_compass?.Refresh(state.Map?.GetRoom(room.Id));    // a reveal may have lit a direction
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
	}
	
	private void OnFloorPressed()
	{
		var room      = _gameState.CurrentRoom;
		var state     = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		var roomState = state.GetRoomState(room.Id);

		_lootPanel.Open(roomState.LootPile, room.Name);
	}
}
