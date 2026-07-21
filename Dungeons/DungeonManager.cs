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

		return JsonSerializer.Deserialize<DungeonData>(json,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
			return JsonSerializer.Deserialize<RoomData>(json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
		MarkExplored(state, currentRoom.Id);

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
		if (exit == null)
		{
			GD.Print($"No exit {direction} from {here.RoomId}");
			return null;
		}
		if (!exit.IsPassable)
		{
			GD.Print($"The {direction} exit is {exit.State}.");
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
		MarkExplored(state, room.Id);

		return room;
	}

	private static void MarkExplored(DungeonState state, string roomId)
	{
		if (!state.ExploredRooms.Contains(roomId))
			state.ExploredRooms.Add(roomId);

		state.LastRoomId = roomId;

		var mapRoom = state.Map?.GetRoom(roomId);
		if (mapRoom != null) mapRoom.Discovered = true;
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
}
