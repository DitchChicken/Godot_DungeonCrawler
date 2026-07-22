using Godot;
using System;

using System.Collections.Generic;

public class SearchData
{
	public float QuickTime { get; set; } = 2.0f;
	public float ThoroughTime { get; set; } = 8.0f;

	// Interactions run on each pass — reuses checks and outcomes wholesale
	public Interaction Quick { get; set; }
	public Interaction Thorough { get; set; }
}

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
	public SearchData Search { get; set; }

	// New fields — loaded now, used later
	public List<Interaction> Actions { get; set; } = new List<Interaction>();
	public List<ExitDef> Exits { get; set; } = new List<ExitDef>();

	// Convenience — joins the description lines into one string
	public string GetDescriptionText() => string.Join("", Description);	
}
