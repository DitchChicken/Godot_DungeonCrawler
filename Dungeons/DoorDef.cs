public class DoorDef
{
	public bool Locked { get; set; } = false;
	public bool Open   { get; set; } = false;
	public string KeyId { get; set; } = "";
	public int LockDC  { get; set; } = 0;
	public int BreakDC { get; set; } = 0;
	public string UnlockFlag { get; set; } = "";
	public string ClosedRule { get; set; } = "None";
}
