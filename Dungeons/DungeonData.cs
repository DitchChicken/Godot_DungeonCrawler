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
