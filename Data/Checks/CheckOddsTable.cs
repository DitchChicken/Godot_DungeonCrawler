using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class CheckOddsTable
{
	public const int MinNeed    = 3;
	public const int MaxNeed    = 18;
	public const int MaxRerolls = 4;

	private const string Path = "res://Data/Checks/CheckOddsTable.json";

	// [rerolls][need - MinNeed] = P(success)
	private static double[][] _odds;
	private static bool _loaded = false;

	private class TableDef
	{
		public int MinNeed { get; set; } = 3;
		public int MaxNeed { get; set; } = 18;
		public Dictionary<string, List<double>> Odds { get; set; } = new();
	}

	public static void Load()
	{
		if (_loaded) return;

		if (FileAccess.FileExists(Path) && TryLoadFromDisk())
		{
			_loaded = true;
			return;
		}

		GD.Print("CheckOddsTable: no table found, computing...");
		_odds = Compute();
		_loaded = true;
		SaveToDisk();
	}

	private static bool TryLoadFromDisk()
	{
		var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
		if (file == null) return false;

		string json = file.GetAsText();
		file.Close();

		try
		{
			var def = JsonSerializer.Deserialize<TableDef>(json,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			_odds = new double[MaxRerolls + 1][];
			for (int r = 0; r <= MaxRerolls; r++)
			{
				if (!def.Odds.TryGetValue(r.ToString(), out var row))
				{
					GD.PrintErr($"CheckOddsTable: missing row for {r} rerolls");
					return false;
				}
				_odds[r] = row.ToArray();
			}
			return true;
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON error in {Path}\n  {ex.Message}");
			return false;
		}
	}

	private static void SaveToDisk()
	{
		var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			// res:// is read-only in exported builds — fine, the runtime compute covers it
			GD.PrintErr($"CheckOddsTable: could not write {Path} (read-only?)");
			return;
		}

		var sb = new System.Text.StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine($"    \"minNeed\": {MinNeed},");
		sb.AppendLine($"    \"maxNeed\": {MaxNeed},");
		sb.AppendLine("    \"odds\": {");
		for (int r = 0; r <= MaxRerolls; r++)
		{
			var vals = new List<string>();
			foreach (double v in _odds[r])
				vals.Add(v.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
			string comma = r < MaxRerolls ? "," : "";
			sb.AppendLine($"        \"{r}\": [{string.Join(", ", vals)}]{comma}");
		}
		sb.AppendLine("    }");
		sb.AppendLine("}");

		file.StoreString(sb.ToString());
		file.Close();
		GD.Print($"CheckOddsTable: wrote {Path}");
	}

	// Exact enumeration over sorted dice states — cheap because order doesn't matter
	private static double[][] Compute()
	{
		var dist = new Dictionary<(int, int, int), double>();
		for (int a = 1; a <= 6; a++)
		for (int b = 1; b <= 6; b++)
		for (int c = 1; c <= 6; c++)
		{
			var key = Sorted(a, b, c);
			dist.TryGetValue(key, out double p);
			dist[key] = p + 1.0 / 216.0;
		}

		var result = new double[MaxRerolls + 1][];
		for (int r = 0; r <= MaxRerolls; r++)
		{
			if (r > 0) dist = RerollStep(dist);
			result[r] = RowFrom(dist);
		}
		return result;
	}

	// Reroll the lowest die, keeping the better of old/new
	private static Dictionary<(int, int, int), double> RerollStep(
		Dictionary<(int, int, int), double> dist)
	{
		var next = new Dictionary<(int, int, int), double>();
		foreach (var kv in dist)
		{
			var (lo, mid, hi) = kv.Key;
			for (int face = 1; face <= 6; face++)
			{
				int newLo = Math.Max(lo, face);
				var key = Sorted(newLo, mid, hi);
				next.TryGetValue(key, out double p);
				next[key] = p + kv.Value / 6.0;
			}
		}
		return next;
	}

	private static double[] RowFrom(Dictionary<(int, int, int), double> dist)
	{
		var row = new double[MaxNeed - MinNeed + 1];
		foreach (var kv in dist)
		{
			int total = kv.Key.Item1 + kv.Key.Item2 + kv.Key.Item3;
			for (int need = MinNeed; need <= total; need++)
				row[need - MinNeed] += kv.Value;
		}
		return row;
	}

	private static (int, int, int) Sorted(int a, int b, int c)
	{
		if (a > b) (a, b) = (b, a);
		if (b > c) (b, c) = (c, b);
		if (a > b) (a, b) = (b, a);
		return (a, b, c);
	}

	// P(success) for a given effective DC and reroll count
	public static double SuccessChance(int effectiveDC, int rerolls)
	{
		if (!_loaded) Load();

		if (effectiveDC <= MinNeed) return 1.0;
		if (effectiveDC > MaxNeed)  return 0.0;

		rerolls = Mathf.Clamp(rerolls, 0, MaxRerolls);
		return _odds[rerolls][effectiveDC - MinNeed];
	}

	// Player-facing hint — never expose the raw number
	public static string DifficultyLabel(double chance) => chance switch
	{
		>= 0.90 => "Trivial",
		>= 0.70 => "Simple",
		>= 0.50 => "Uncertain",
		>= 0.30 => "Difficult",
		>= 0.10 => "Daunting",
		_       => "Near hopeless"
	};
}
