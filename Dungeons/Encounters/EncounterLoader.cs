using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class EncounterLoader
{
	private static Dictionary<string, EncounterData> _cache 
		= new Dictionary<string, EncounterData>();

	public static EncounterData LoadEncounter(string dungeonId, string encounterId)
	{
		string key = $"{dungeonId}/{encounterId}";
		if (_cache.ContainsKey(key)) return _cache[key];

		// Try dungeon-specific folder first
		string path = $"res://Data/Encounters/{dungeonId}/{encounterId}.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Encounter not found: {path}");
			return null;
		}

		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null) return null;

		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var data    = JsonSerializer.Deserialize<EncounterData>(json, options);
		if (data != null)
			_cache[key] = data;

		return data;
	}

	// Roll encounter from a room's encounter list
	// Returns the triggered EncounterData or null if none triggered
	public static EncounterData RollRoomEncounter(
		RoomData room, string dungeonId, System.Random rng)
	{
		if (room.Encounters == null || room.Encounters.Count == 0)
			return null;

		foreach (var entry in room.Encounters)
		{
			if (rng.NextDouble() < entry.Chance)
			{
				GD.Print($"Room encounter triggered: {entry.Id}");
				return LoadEncounter(dungeonId, entry.Id);
			}
		}

		return null;
	}

	// Roll wandering monster encounter
	public static EncounterData RollWanderingEncounter(
		string dungeonId, System.Random rng)
	{
		string tablePath = $"res://Data/Encounters/{dungeonId}/WanderingTable.json";
		if (!FileAccess.FileExists(tablePath)) return null;

		var file = FileAccess.Open(tablePath, FileAccess.ModeFlags.Read);
		if (file == null) return null;

		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var table   = JsonSerializer.Deserialize<List<RoomEncounterEntry>>(json, options);
		if (table == null) return null;

		foreach (var entry in table)
		{
			if (rng.NextDouble() < entry.Chance)
			{
				GD.Print($"Wandering encounter triggered: {entry.Id}");
				return LoadEncounter(dungeonId, entry.Id);
			}
		}

		return null;
	}
}
