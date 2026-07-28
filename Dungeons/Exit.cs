public enum ExitVisibility
{
	Visible,        // party can see it
	HiddenToParty,  // findable by searching
	Hidden          // needs a trigger to reveal
}

public class Exit
{
	public Direction Direction { get; set; }
	public string TargetRoomId { get; set; }
	public ExitVisibility Visibility { get; set; } = ExitVisibility.Visible;
	public string Label { get; set; } = "";
	public int CorridorLength { get; set; } = 1;
	public float TravelTime { get; set; } = 1.0f;

	public Door Door { get; set; }   // null = open passage, no door

	public bool Discovered { get; set; } = true;

	// Party can go this way: discovered, and either no door or an unlocked/destroyed one
	public bool IsPassable => Discovered && (Door == null || Door.PartyCanPass);

	// Shown on the compass (discovered, regardless of whether it's currently passable)
	public bool IsVisibleToParty => Discovered && Visibility == ExitVisibility.Visible;
}
