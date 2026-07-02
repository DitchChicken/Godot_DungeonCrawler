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

		return JsonSerializer.Deserialize<RoomData>(json,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
	}
		
	public static RoomData EnterDungeon(string dungeonId, GameState gameState)
	{
		var dungeon = LoadDungeon(dungeonId);
		if (dungeon == null) return null;

		gameState.CurrentDungeon = dungeonId;
		var state = gameState.GetDungeonState(dungeonId);

		// Only generate pool if this dungeon hasn't been visited this run
		if (state.RoomPool.Count == 0)
		{
			state.RoomPool = GenerateRoomPool(dungeon, state);
			GD.Print($"Generated new pool for {dungeon.Name} - {state.RoomPool.Count} rooms");
		}
		else
		{
			GD.Print($"Resuming {dungeon.Name} - {state.RoomPool.Count} rooms remaining");
		}

		// Pick entry room or resume last room
		RoomData currentRoom;
		if (!string.IsNullOrEmpty(state.LastRoomId))
		{
			currentRoom = LoadRoom(dungeonId, state.LastRoomId);
			GD.Print($"Resuming at {currentRoom?.Name}");
		}
		else
		{
			string entryRoomId = dungeon.EntryRooms[_rng.Next(dungeon.EntryRooms.Count)];
			currentRoom = LoadRoom(dungeonId, entryRoomId);
		}

		gameState.CurrentRoom = currentRoom;
		// Add room to explored rooms
		if (!state.ExploredRooms.Contains(currentRoom.Id))
			state.ExploredRooms.Add(currentRoom.Id);
		RegisterNextRoom(state, currentRoom);
		
		return currentRoom;
	}

	public static List<string> GenerateRoomPool(DungeonData dungeon, DungeonState state)
	{
		var pool = new List<string>();

		foreach (var roomId in dungeon.Rooms)
		{
			if (dungeon.EntryRooms.Contains(roomId)) continue;

			var room = LoadRoom(dungeon.Id, roomId);
			if (room == null) continue;

			if (room.Unique)
			{
				if (_rng.NextDouble() <= room.SpawnChance
					&& !state.UniqueRoomsFound.Contains(roomId))
					pool.Add(roomId);
			}
			else
			{
				int count = _rng.Next(room.MinOccurrences, room.MaxOccurrences + 1);
				for (int i = 0; i < count; i++)
					pool.Add(roomId);
			}
		}

		Shuffle(pool);
		return pool;
	}

	public static RoomData Explore(GameState gameState)
	{
		string dungeonId = gameState.CurrentDungeon;
		var state = gameState.GetDungeonState(dungeonId);
		
		RoomData room;

		// Forced next room takes priority over the random pool
		if (!string.IsNullOrEmpty(state.PendingNextRoom))
		{
			string nextId = state.PendingNextRoom;
			state.PendingNextRoom = ""; // consume it

			// Remove from pool if it happened to be there, so we don't draw it again
			state.RoomPool.Remove(nextId);

			room = LoadRoom(dungeonId, nextId);
			if (room == null)
			{
				GD.PrintErr($"Forced next room '{nextId}' failed to load — falling back to pool");
				// fall through to random draw below
			}
			else
			{
				FinalizeExploredRoom(state, room);
				return room;
			}
		}

		// Normal random draw from pool
		if (state.RoomPool.Count == 0)
		{
			GD.Print("Room pool empty — nowhere left to explore.");
			return null;
		}

		int index = _rng.Next(state.RoomPool.Count);
		string roomId = state.RoomPool[index];
		state.RoomPool.RemoveAt(index);

		room = LoadRoom(dungeonId, roomId);
		if (room == null) return null;

		FinalizeExploredRoom(state, room);
		return room;
	}

	// Shared post-load bookkeeping — track explored, uniques, and next-room pointer
	private static void FinalizeExploredRoom(DungeonState state, RoomData room)
	{
		if (!state.ExploredRooms.Contains(room.Id))
			state.ExploredRooms.Add(room.Id);

		if (room.Unique && !state.UniqueRoomsFound.Contains(room.Id))
			state.UniqueRoomsFound.Add(room.Id);

		state.LastRoomId = room.Id;

		RegisterNextRoom(state, room);
	}
	
	private static void Shuffle(List<string> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = _rng.Next(n + 1);
			(list[k], list[n]) = (list[n], list[k]);
		}
	}
	
	// Records a room's nextRoom pointer as the pending forced draw
	private static void RegisterNextRoom(DungeonState state, RoomData room)
	{
		if (room != null && !string.IsNullOrEmpty(room.NextRoom))
		{
			state.PendingNextRoom = room.NextRoom;
		}
		else
			state.PendingNextRoom = "";
	}	
	
	public static bool CanExplore(GameState gameState)
	{
		var state = gameState.GetDungeonState(gameState.CurrentDungeon);
		if (state == null) return false;

		// Can explore if there's a forced next room OR rooms left in the pool
		return !string.IsNullOrEmpty(state.PendingNextRoom) || state.RoomPool.Count > 0;
	}
}
