using System.Collections.Generic;

public class DungeonState
{
	public List<string> RoomPool { get; set; }            = new List<string>();
	public List<string> UniqueRoomsFound { get; set; }    = new List<string>();
	public string LastRoomId { get; set; }                = "";
	public HashSet<string> CompletedEncounters { get; set; } = new HashSet<string>();
	public List<string> ExploredRooms { get; set; } = new List<string>();
}
