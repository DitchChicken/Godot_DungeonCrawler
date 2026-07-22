using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class SkillRegistry
{
	private static HashSet<string> _skills = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
	private static bool _loaded = false;

	private class SkillListDef { public List<string> Skills { get; set; } = new List<string>(); }

	public static void Load()
	{
		string path = "res://Data/Skills/SkillMasterList.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load skill master list: {path}");
			return;
		}

		string json = file.GetAsText();
		file.Close();

		try
		{
			var def = JsonSerializer.Deserialize<SkillListDef>(json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			_skills = new HashSet<string>(def.Skills, System.StringComparer.OrdinalIgnoreCase);
			_loaded = true;
			GD.Print($"SkillRegistry loaded {_skills.Count} skills.");
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON error in {path}\n  {ex.Message}");
		}
	}

	public static bool IsValid(string skill)
	{
		if (!_loaded) Load();
		return !string.IsNullOrEmpty(skill) && _skills.Contains(skill);
	}

	// Validate a skill name, logging an error with context if unknown. Returns validity.
	public static bool Validate(string skill, string source)
	{
		if (IsValid(skill)) return true;
		GD.PrintErr($"Unknown skill '{skill}' referenced in {source}. " +
					$"Check SkillMasterList.json or fix the typo.");
		return false;
	}

	public static IReadOnlyCollection<string> All
	{
		get { if (!_loaded) Load(); return _skills; }
	}
}
