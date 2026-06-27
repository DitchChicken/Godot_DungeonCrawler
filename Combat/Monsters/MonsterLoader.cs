using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class MonsterLoader
{
	public static Monster LoadMonster(string monsterId)
	{
		string path = $"res://Data/Monsters/{monsterId}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load monster: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		var def = JsonSerializer.Deserialize<MonsterDef>(json, options);
		if (def == null)
		{
			GD.PrintErr($"Could not deserialize monster: {path}");
			return null;
		}

		var monster = new Monster
		{
			Id                = def.Id ?? def.Name.ToLower(),
			Name              = def.Name,
			Level             = def.Level,
			MaxHP             = def.MaxHP,
			MaxMP             = def.MaxMP,
			ExperienceReward  = def.ExperienceReward,
			GoldReward        = def.GoldReward,
			Initiative        = def.Initiative,
			Resistances       = def.Resistances ?? new Dictionary<string, float>(),
			Sprite            = def.Sprite,
			Portrait          = def.Portrait ?? "",
			AttackIds         = def.Attacks ?? new List<string>()
		};

		// Load attacks
		monster.Attacks = AttackLoader.LoadAttacks(monster.AttackIds);
		monster.Initialize();

		//GD.Print($"Loaded monster: {monster.Name} HP:{monster.MaxHP} Level:{monster.Level}");
		return monster;
	}

	public static List<Monster> LoadEncounter(List<string> monsterIds)
	{
		var monsters = new List<Monster>();
		foreach (var id in monsterIds)
		{
			var monster = LoadMonster(id);
			if (monster != null)
				monsters.Add(monster);
		}
		GD.Print($"Loaded encounter with {monsters.Count} monsters");
		return monsters;
	}
}
