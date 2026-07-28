public class ExitDef
{
	public string Direction { get; set; }
	public string Target { get; set; }
	public string Visibility { get; set; } = "Visible";
	public string Label { get; set; } = "";
	public int CorridorLength { get; set; } = 1;
	public float TravelTime { get; set; } = 1.0f;
	public DoorDef Door { get; set; }   // null = no door
}
