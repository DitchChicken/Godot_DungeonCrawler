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
		var state = gameState.GetDungeonState(gameState.CurrentDungeon);

		if (state.RoomPool.Count == 0)
		{
			GD.PrintErr("Explore called with empty room pool");
			return null;
		}

		int index = _rng.Next(state.RoomPool.Count);
		string roomId = state.RoomPool[index];
		state.RoomPool.RemoveAt(index);

		var room = LoadRoom(gameState.CurrentDungeon, roomId);

		if (room != null && room.Unique)
			state.UniqueRoomsFound.Add(roomId);

		state.LastRoomId = room?.Id ?? "";
		gameState.CurrentRoom = room;

		GD.Print($"Exploring: {room?.Name} - {state.RoomPool.Count} rooms remaining");
		return room;
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
}
