using Godot;
using System;

using System.Collections.Generic;

public class RoomData
{
	public string Id { get; set; }
	public string Name { get; set; }

	// Description is stored as an array in JSON for editing readability,
	// joined into a single string on load.
	public List<string> Description { get; set; } = new List<string>();

	public string Image { get; set; }
	public bool Unique { get; set; }
	public int MinOccurrences { get; set; } = 1;
	public int MaxOccurrences { get; set; } = 1;
	public float SpawnChance { get; set; } = 1.0f;
	public List<RoomEncounterEntry> Encounters { get; set; } = new List<RoomEncounterEntry>();

	// New fields — loaded now, used later
	public List<RoomAction> Actions { get; set; } = new List<RoomAction>();
	public string NextRoom { get; set; } = "";

	// Convenience — joins the description lines into one string
	public string GetDescriptionText() => string.Join("", Description);
}

public class RoomAction
{
	public string Name { get; set; }
}
