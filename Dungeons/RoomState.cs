using System.Collections.Generic;

public class RoomState
{
	public string RoomId { get; set; }
	public bool Visited { get; set; } = false;

	// Unclaimed loot left behind in this room
	public List<Equipment> LootPile { get; set; } = new List<Equipment>();

	// Room interactions already performed (for the `actions` field later)
	public HashSet<string> CompletedActions { get; set; } = new HashSet<string>();
}
