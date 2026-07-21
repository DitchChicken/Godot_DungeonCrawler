using System.Collections.Generic;

public class DungeonState
{
	public DungeonMap Map { get; set; }
	public List<string> UniqueRoomsFound { get; set; }    = new List<string>();
	public string LastRoomId { get; set; }                = "";
	public List<string> ExploredRooms { get; set; } = new List<string>();
	
	public Dictionary<string, RoomState> RoomStates { get; set; } = new Dictionary<string, RoomState>();

	public EncounterManager Encounters { get; set; } = new EncounterManager();

	public RoomState GetRoomState(string roomId)
	{
		if (!RoomStates.TryGetValue(roomId, out var state))
		{
			state = new RoomState { RoomId = roomId };
			RoomStates[roomId] = state;
		}
		return state;
	}

}
