using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class DungeonManager
{
	private static Random _rng = new Random();

	public static DungeonData LoadDungeon(string dungeonId)
	{
		string path = $"res://Dungeons/{dungeonId}/{dungeonId}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load dungeon: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		return JsonSerializer.Deserialize<DungeonData>(json, JsonConfig.Options);
	}

	public static RoomData LoadRoom(string dungeonId, string roomId)
	{
		string path = $"res://Dungeons/{dungeonId}/Rooms/{roomId}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load room: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		try
		{
			RoomData room = JsonSerializer.Deserialize<RoomData>(json, JsonConfig.Options);
			ValidateRoomDomains(room, roomId);
			return room;
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON error in {path}\n  {ex.Message}");
			return null;
		}
	}

	public static RoomData EnterDungeon(string dungeonId, GameState gameState)
	{
		var dungeon = LoadDungeon(dungeonId);
		if (dungeon == null) return null;

		gameState.CurrentDungeon = dungeonId;
		var state = gameState.GetDungeonState(dungeonId);

		// Build the full graph once, on first entry this run
		if (state.Map == null)
		{
			string mapEntryId = dungeon.EntryRooms[_rng.Next(dungeon.EntryRooms.Count)];
			state.Map = DungeonMapBuilder.Build(dungeonId, mapEntryId);
			GD.Print($"Built map for {dungeon.Name}: {state.Map.Rooms.Count} rooms");

			state.Encounters.PopulateDungeon(dungeonId, state.Map.AllRoomIds);
		}

		// Resume where we left off, or start at the map's entry room
		string startRoomId = !string.IsNullOrEmpty(state.LastRoomId)
			? state.LastRoomId
			: state.Map.EntryRoomId;

		var currentRoom = LoadRoom(dungeonId, startRoomId);
		if (currentRoom == null) return null;

		gameState.CurrentRoom = currentRoom;
		MarkExplored(state, dungeonId, currentRoom.Id, gameState);

		return currentRoom;
	}

	// Move through an exit from the current room. Returns the new room, or null.
	public static RoomData MoveThroughExit(GameState gameState, Direction direction)
	{
		string dungeonId = gameState.CurrentDungeon;
		var state = gameState.GetDungeonState(dungeonId);
		if (state?.Map == null) return null;

		var here = state.Map.GetRoom(gameState.CurrentRoom?.Id);
		if (here == null) return null;

		var exit = here.GetExit(direction);
		if (exit == null) { DungeonLog.Print($"No exit {direction}."); return null; }

		if (exit.Door != null && !exit.Door.Exists) { /* walk through the broken doorway — passable */ }

		if (!exit.IsPassable)
		{
			string why = exit.Door is { Locked: true } ? "locked" : "blocked";
			DungeonLog.Print($"The way {direction.ToString().ToLower()} is {why}.", DungeonLog.Damage);
			return null;
		}

		return MoveToRoom(gameState, exit.TargetRoomId);
	}

	// Direct jump to a room by id (used by the cheater Move menu and flee).
	public static RoomData MoveToRoom(GameState gameState, string roomId)
	{
		string dungeonId = gameState.CurrentDungeon;
		var state = gameState.GetDungeonState(dungeonId);

		var room = LoadRoom(dungeonId, roomId);
		if (room == null) return null;

		gameState.CurrentRoom = room;
		MarkExplored(state, dungeonId, room.Id, gameState);

		return room;
	}

	private static void MarkExplored(DungeonState state, string dungeonId, string roomId, GameState gameState)
	{
		bool firstVisit = !state.ExploredRooms.Contains(roomId);
		if (firstVisit) state.ExploredRooms.Add(roomId);

		state.LastRoomId = roomId;

		var mapRoom = state.Map?.GetRoom(roomId);
		if (mapRoom != null)
		{
			mapRoom.Discovered = true;
			foreach (var exit in mapRoom.Exits)
				if (exit.IsVisibleToParty) exit.Discovered = true;
		}

		if (firstVisit)
		{
			var roomData = LoadRoom(dungeonId, roomId);
			if (roomData?.Search != null)
				state.GetRoomState(roomId).Searched = roomData.Search.InitialLevel;
		}
		
		foreach (var exit in mapRoom.Exits)
		{
			var door = exit.Door;
			if (door == null || !door.Locked) continue;
			if (string.IsNullOrEmpty(door.KeyId) || !gameState.HasKey(door.KeyId)) continue;

			door.Locked = false;
			if (!string.IsNullOrEmpty(door.UnlockFlag))
				state.Flags.Add(door.UnlockFlag);
		}
	}

	// Are there any passable exits from the current room?
	public static bool CanExplore(GameState gameState)
	{
		var state = gameState.GetDungeonState(gameState.CurrentDungeon);
		var here  = state?.Map?.GetRoom(gameState.CurrentRoom?.Id);
		if (here == null) return false;

		return here.Exits.Exists(e => e.IsPassable);
	}

	// Passable, party-visible exits from the current room — for the Move menu later.
	public static List<Exit> GetAvailableExits(GameState gameState)
	{
		var state = gameState.GetDungeonState(gameState.CurrentDungeon);
		var here  = state?.Map?.GetRoom(gameState.CurrentRoom?.Id);
		if (here == null) return new List<Exit>();

		return here.Exits.FindAll(e => e.IsPassable && e.IsVisibleToParty);
	}
	
	private static void ValidateRoomDomains(RoomData room, string roomId)
	{
		if (room?.Actions == null) return;

		foreach (var action in room.Actions)
			ValidateInteractionDomain(action, roomId);

		if (room.Search?.Quick != null)    ValidateInteractionDomain(room.Search.Quick, roomId);
		if (room.Search?.Thorough != null) ValidateInteractionDomain(room.Search.Thorough, roomId);
	}

	private static void ValidateInteractionDomain(Interaction action, string roomId)
	{
		if (action?.Check == null) return;
		// Empty domain = level-0 check, no domain required — valid by design
		if (string.IsNullOrEmpty(action.Check.Domain)) return;

		DomainRegistry.Validate(action.Check.Domain, $"room '{roomId}' action '{action.Id}'");
	}
}
