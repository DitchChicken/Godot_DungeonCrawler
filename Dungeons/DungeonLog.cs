using Godot;
using System;

public static class DungeonLog
{
	public static Action<string, Color?> Sink;

	// Common message colors
	public static readonly Color Movement    = new Color(0.5f, 1.0f, 0.5f);   // green
	public static readonly Color Interaction = new Color(1.0f, 0.85f, 0.4f);  // gold
	public static readonly Color Healing     = new Color(0.5f, 1.0f, 0.8f);   // teal
	public static readonly Color Damage      = new Color(1.0f, 0.45f, 0.45f); // red
	public static readonly Color Flavor      = new Color(0.75f, 0.75f, 0.85f);// pale lavender

	public static void Print(string message, Color? color = null)
	{
		if (Sink != null) Sink(message, color);
		else GD.Print($"[dungeon log, no sink] {message}");
	}
}
