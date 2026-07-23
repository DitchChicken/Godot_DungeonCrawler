using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class DomainRegistry
{
	private static HashSet<string> _domains = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
	private static bool _loaded = false;

	private class DomainListDef { public List<string> Domains { get; set; } = new List<string>(); }

	public static void Load()
	{
		string path = "res://Data/Domains/DomainMasterList.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load domain master list: {path}");
			return;
		}

		string json = file.GetAsText();
		file.Close();

		try
		{
			var def = JsonSerializer.Deserialize<DomainListDef>(json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			_domains = new HashSet<string>(def.Domains, System.StringComparer.OrdinalIgnoreCase);
			_loaded = true;
			GD.Print($"DomainRegistry loaded {_domains.Count} domains.");
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON error in {path}\n  {ex.Message}");
		}
	}

	public static bool IsValid(string domain)
	{
		if (!_loaded) Load();
		return !string.IsNullOrEmpty(domain) && _domains.Contains(domain);
	}

	public static bool Validate(string domain, string source)
	{
		if (IsValid(domain)) return true;
		GD.PrintErr($"Unknown domain '{domain}' referenced in {source}. " +
					$"Check DomainMasterList.json or fix the typo.");
		return false;
	}

	public static IReadOnlyCollection<string> All
	{
		get { if (!_loaded) Load(); return _domains; }
	}
}
