using Godot;
using System.Collections.Generic;

public class MapRoom
{
	public string RoomId { get; set; }
	public Vector3I Coordinates { get; set; }
	public List<Exit> Exits { get; set; } = new List<Exit>();
	public bool Discovered { get; set; } = false;

	public Exit GetExit(Direction dir) => Exits.Find(e => e.Direction == dir);
}

public class DungeonMap
{
	public string DungeonId { get; set; }
	public string EntryRoomId { get; set; }
	public Dictionary<string, MapRoom> Rooms { get; set; } = new Dictionary<string, MapRoom>();

	public MapRoom GetRoom(string roomId)
		=> Rooms.TryGetValue(roomId, out var r) ? r : null;

	public List<string> AllRoomIds => new List<string>(Rooms.Keys);
}
