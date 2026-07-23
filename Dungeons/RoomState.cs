using System.Collections.Generic;

public enum SearchLevel { Unsearched, Quick, Thorough }

public class RoomState
{
	public string RoomId { get; set; }
	public bool Visited { get; set; } = false;	
	public SearchLevel Searched { get; set; } = SearchLevel.Unsearched;
		
	// Unclaimed loot left behind in this room
	public Inventory LootPile { get; set; } = new Inventory(64, 0, 0);
	public bool HasLoot => LootPile.UsedSlots > 0;
	
	// Room interactions already performed (for the `actions` field later)
	public HashSet<string> RevealedActions { get; set; } = new HashSet<string>();
	public HashSet<string> CompletedActions { get; set; } = new HashSet<string>();
}
