using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class AbilityLoader
{
	private static Dictionary<string, Ability> _cache = new Dictionary<string, Ability>();

	public static Ability LoadAbility(string abilityId)
	{
		if (_cache.ContainsKey(abilityId)) return _cache[abilityId];

		string path = $"res://Data/Abilities/{abilityId}.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Ability not found: {path}");
			return null;
		}

		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var def = JsonSerializer.Deserialize<AbilityDef>(json, options);
		if (def == null) return null;

		var ability = new Ability
		{
			Id          = def.Id,
			Name        = def.Name,
			Description = def.Description,
			Type        = Enum.Parse<AbilityType>(def.AbilityType ?? "Spell"),
			TargetType  = Enum.Parse<AbilityTargetType>(def.TargetType ?? "SingleAlly"),
			EffectType  = Enum.Parse<AbilityEffectType>(def.EffectType ?? "Heal"),
			Power       = def.Power,
			ManaCost    = def.ManaCost,
			HealthCost  = def.HealthCost,
			Cooldown    = def.Cooldown,
			Element     = def.Element ?? "None",
			StatusEffect = def.StatusEffect ?? "",
			ClassRestriction = def.ClassRestriction ?? "",
			UsableIn = def.UsableIn ?? new List<string>(),
			Icon        = def.Icon ?? ""
		};

		_cache[abilityId] = ability;
		return ability;
	}

	public static List<Ability> LoadAbilities(List<string> ids)
	{
		var list = new List<Ability>();
		if (ids == null) return list;
		foreach (var id in ids)
		{
			var a = LoadAbility(id);
			if (a != null) list.Add(a);
		}
		return list;
	}
	
}
