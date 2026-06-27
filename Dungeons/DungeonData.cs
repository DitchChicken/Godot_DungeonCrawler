using Godot;
using System;
using System.Collections.Generic;

public class DungeonData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public int Levels { get; set; }
	public List<string> EntryRooms { get; set; }
	public List<string> Rooms { get; set; }
}

public class RoomData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string Image { get; set; }
	public bool Unique { get; set; }
	public int MinOccurrences { get; set; } = 1;
	public int MaxOccurrences { get; set; } = 1;
	public float SpawnChance { get; set; } = 1.0f;
	public List<RoomEncounterEntry> Encounters { get; set; } = new List<RoomEncounterEntry>();
}
