using System;
using System.Text.RegularExpressions;

public static class Dice
{
	private static Random _rng = new Random();

	// Parses "3d10", "2d6+3", "d20". Returns 0 on a malformed string.
	public static int Roll(string expression)
	{
		if (string.IsNullOrWhiteSpace(expression)) return 0;

		var m = Regex.Match(expression.Trim().ToLower(),
			@"^(\d*)d(\d+)([+-]\d+)?$");
		if (!m.Success)
		{
			Godot.GD.PrintErr($"Bad dice expression: '{expression}'");
			return 0;
		}

		int count = m.Groups[1].Value == "" ? 1 : int.Parse(m.Groups[1].Value);
		int sides = int.Parse(m.Groups[2].Value);
		int mod   = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;

		int total = mod;
		for (int i = 0; i < count; i++)
			total += _rng.Next(1, sides + 1);

		return Math.Max(0, total);
	}
}
