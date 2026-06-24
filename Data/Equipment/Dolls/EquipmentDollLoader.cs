using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class EquipmentDollLoader
{
	private static Dictionary<string, EquipmentDollDef> _cache 
		= new Dictionary<string, EquipmentDollDef>();

	public static EquipmentDollDef LoadDoll(Race race)
	{
		string id = race.ToString();
		if (_cache.ContainsKey(id)) return _cache[id];

		string path = $"res://Data/Equipment/Dolls/{id}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load doll: {path}, falling back to Human");
			// Fall back to Human if race-specific doll not found
			path = "res://Data/EquipmentDolls/Human.json";
			file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (file == null) return null;
		}

		string json = file.GetAsText();
		file.Close();

		var def = JsonSerializer.Deserialize<EquipmentDollDef>(json,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		_cache[id] = def;
		return def;
	}
}
