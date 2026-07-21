public enum ExitState
{
	Open,           // passable
	Locked,         // visible, needs a key
	HiddenToParty,  // exists, findable by searching
	Hidden,         // exists, needs a specific trigger to reveal
	Blocked         // visible, impassable
}

public class Exit
{
	public Direction Direction { get; set; }
	public string TargetRoomId { get; set; }
	public ExitState State { get; set; } = ExitState.Open;
	public string KeyId { get; set; } = "";
	public string Label { get; set; } = "";
	public bool Discovered { get; set; } = false;

	// Map spacing — how many cells of corridor between the two rooms
	public int CorridorLength { get; set; } = 1;

	// Minutes (or whatever unit) to traverse — for the future time system
	public float TravelTime { get; set; } = 1.0f;

	public bool IsPassable => State == ExitState.Open;
	public bool IsVisibleToParty => State == ExitState.Open
								 || State == ExitState.Locked
								 || State == ExitState.Blocked;
}
