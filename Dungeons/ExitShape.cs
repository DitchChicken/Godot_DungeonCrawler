public class ExitDef
{
	public string Direction { get; set; }
	public string Target { get; set; }
	public string State { get; set; } = "Open";
	public string KeyId { get; set; } = "";
	public string Label { get; set; } = "";
	public int CorridorLength { get; set; } = 1;
	public float TravelTime { get; set; } = 1.0f;
}
