	using Godot;
using System;
using System.Collections.Generic;

public static class DungeonMapBuilder
{
	// Walks the exit graph from the entry room, placing every reachable room
	// (including those behind locked/hidden exits) and deriving coordinates.
	public static DungeonMap Build(string dungeonId, string entryRoomId)
	{
		var map = new DungeonMap { DungeonId = dungeonId, EntryRoomId = entryRoomId };

		var queue   = new Queue<(string roomId, Vector3I coord)>();
		var visited = new HashSet<string>();

		queue.Enqueue((entryRoomId, Vector3I.Zero));
		visited.Add(entryRoomId);

		while (queue.Count > 0)
		{
			var (roomId, coord) = queue.Dequeue();

			var roomData = DungeonManager.LoadRoom(dungeonId, roomId);
			if (roomData == null)
			{
				GD.PrintErr($"Map build: room '{roomId}' failed to load");
				continue;
			}

			var mapRoom = new MapRoom { RoomId = roomId, Coordinates = coord };

			foreach (var def in roomData.Exits)
			{
				if (!Enum.TryParse<Direction>(def.Direction, true, out var dir))
				{
					GD.PrintErr($"Map build: bad direction '{def.Direction}' in {roomId}");
					continue;
				}
				if (!System.Enum.TryParse<ExitVisibility>(def.Visibility ?? "Visible", true, out var vis))
					vis = ExitVisibility.Visible;

				var exit = new Exit
					{
						Direction      = dir,
						TargetRoomId   = def.Target,
						Visibility     = vis,
						Label          = def.Label ?? "",
						CorridorLength = System.Math.Max(1, def.CorridorLength),
						TravelTime     = def.TravelTime,
						// Visible exits are known from the start; hidden ones must be found
						Discovered     = (vis == ExitVisibility.Visible)
					};
					
			   if (def.Door != null)
	  			  exit.Door = BuildDoor(def.Door);

				mapRoom.Exits.Add(exit);

				// Place the neighbour, offset by corridor length
				if (!visited.Contains(def.Target))
				{
					visited.Add(def.Target);
					var neighbourCoord = coord + dir.Offset() * exit.CorridorLength;
					queue.Enqueue((def.Target, neighbourCoord));
				}
			}

			// Share door instances across reciprocal exits: whichever side declared the
			// door owns it; the other side points at the same object.
			foreach (var room in map.Rooms.Values)
			{
				foreach (var exit in room.Exits)
				{
					if (exit.Door == null) continue;

					var farRoom = map.GetRoom(exit.TargetRoomId);
					var backExit = farRoom?.GetExit(exit.Direction.Opposite());
					if (backExit == null) continue;

					// If the far side didn't declare its own door, share this one
					if (backExit.Door == null)
						backExit.Door = exit.Door;
					// If both declared, that's an authoring error — warn
					else if (!ReferenceEquals(backExit.Door, exit.Door))
						GD.PrintErr($"Door declared on both sides of {room.RoomId}<->{exit.TargetRoomId}; " +
									$"using {room.RoomId}'s.");
				}
			}
			
			map.Rooms[roomId] = mapRoom;
		}

		ValidateMap(map);
		return map;
	}

	// Sanity checks — reports contradictions rather than silently producing a broken map
	private static void ValidateMap(DungeonMap map)
	{
		// Duplicate coordinates
		var seen = new Dictionary<Vector3I, string>();
		foreach (var kv in map.Rooms)
		{
			if (seen.TryGetValue(kv.Value.Coordinates, out var other))
				GD.PrintErr($"Map: '{kv.Key}' and '{other}' both at {kv.Value.Coordinates}");
			else
				seen[kv.Value.Coordinates] = kv.Key;
		}

		// Missing reciprocal exits
		foreach (var kv in map.Rooms)
		{
			foreach (var exit in kv.Value.Exits)
			{
				var target = map.GetRoom(exit.TargetRoomId);
				if (target == null)
				{
					GD.PrintErr($"Map: '{kv.Key}' exits to unknown room '{exit.TargetRoomId}'");
					continue;
				}

				var back = target.GetExit(exit.Direction.Opposite());
				if (back == null || back.TargetRoomId != kv.Key)
					GD.PrintErr($"Map: '{kv.Key}' → {exit.Direction} → '{exit.TargetRoomId}' " +
								$"has no matching return exit");
			}
		}
	}
	
	private static Door BuildDoor(DoorDef def)
	{
		System.Enum.TryParse<ClosedPassRule>(def.ClosedRule ?? "None", true, out var rule);
		return new Door
		{
			Locked     = def.Locked,
			Open       = def.Open,
			KeyId      = def.KeyId ?? "",
			LockDC     = def.LockDC,
			BreakDC    = def.BreakDC,
			UnlockFlag = def.UnlockFlag ?? "",
			ClosedRule = rule
		};
	}
}
