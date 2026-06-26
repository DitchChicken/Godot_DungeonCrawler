using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class AttackLoader
{
	private static Dictionary<string, Attack> _cache = new Dictionary<string, Attack>();

	public static Attack LoadAttack(string attackId)
	{
		if (_cache.ContainsKey(attackId)) return _cache[attackId];

		string path = $"res://Data/Monsters/Attacks/{attackId}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load attack: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var def     = JsonSerializer.Deserialize<AttackDef>(json, options);
		if (def == null) return null;

		var attack = new Attack
		{
			Id           = def.Id,
			Name         = def.Name,
			Description  = def.Description,
			AttackType   = Enum.Parse<AttackType>(def.AttackType ?? "Physical"),
			DamageType   = Enum.Parse<DamageType>(def.DamageType ?? "Bludgeoning"),
			TargetType   = Enum.Parse<TargetType>(def.TargetType ?? "SingleEnemy"),
			BaseDamageMin = def.BaseDamageMin,
			BaseDamageMax = def.BaseDamageMax,
			StatusEffect = def.StatusEffect ?? "",
			StatusChance = def.StatusChance,
			ManaCost     = def.ManaCost,
			HealthCost   = def.HealthCost,
			Accuracy     = def.Accuracy,
			Weight       = def.Weight,
			Range        = Enum.Parse<WeaponRange>(def.Range ?? "Melee")
		};

		_cache[attackId] = attack;
		return attack;
	}

	public static List<Attack> LoadAttacks(List<string> attackIds)
	{
		var attacks = new List<Attack>();
		foreach (var id in attackIds)
		{
			var attack = LoadAttack(id);
			if (attack != null)
				attacks.Add(attack);
		}
		return attacks;
	}

	// Select an attack from a list using weighted random
	public static Attack SelectWeightedAttack(List<Attack> attacks, Random rng)
	{
		if (attacks == null || attacks.Count == 0) return null;

		int totalWeight = 0;
		foreach (var a in attacks) totalWeight += a.Weight;

		int roll      = rng.Next(0, totalWeight);
		int cumulative = 0;

		foreach (var attack in attacks)
		{
			cumulative += attack.Weight;
			if (roll < cumulative)
				return attack;
		}

		return attacks[attacks.Count - 1];
	}
}
