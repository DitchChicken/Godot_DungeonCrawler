using Godot;
using System;

public static class DungeonClock
{
	public const int Base         = 8;
	public const int TicksPerShift = Base * Base * Base;   // 512

	// Fired whenever time advances, so the UI can refresh
	public static Action Changed;

	public static float GetTicks(GameState gs)
	{
		var state = gs.GetDungeonState(gs.CurrentDungeon);
		return state?.Ticks ?? 0f;
	}

	public static void Advance(GameState gs, float amount, string reason = "")
	{
		if (amount <= 0f) return;

		var state = gs.GetDungeonState(gs.CurrentDungeon);
		if (state == null) return;

		state.Ticks += amount;

//		if (!string.IsNullOrEmpty(reason))
//			GD.Print($"[clock] +{amount:0.##} ({reason}) → {state.Ticks:0.##}");

		Changed?.Invoke();
	}

	public static void Reset(GameState gs)
	{
		var state = gs.GetDungeonState(gs.CurrentDungeon);
		if (state != null) state.Ticks = 0f;
		Changed?.Invoke();
	}

	// Base-8 display: shift, then three octal digits
	public static string Format(float ticks)
	{
		int whole = Mathf.FloorToInt(ticks);
		int shift = whole / TicksPerShift;
		int rem   = whole % TicksPerShift;

		int d0 = rem / (Base * Base);
		int d1 = (rem / Base) % Base;
		int d2 = rem % Base;

		return $"Shift {shift} — {d0}:{d1}:{d2}";
	}
	
	// Current tick count, readable from anywhere. 0 when not in a dungeon.
	public static float Current
	{
		get
		{
			var gs = ((SceneTree)Engine.GetMainLoop()).Root
				.GetNodeOrNull<GameState>("/root/GameState");
			return gs == null ? 0f : GetTicks(gs);
		}
	}
}
