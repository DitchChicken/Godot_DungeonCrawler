using Godot;

// What can pass a door while it's closed (party always passes if unlocked)
public enum ClosedPassRule
{
	None,        // nothing passes closed
	SmallOnly    // e.g. slimes ooze under a grate — placeholder for later
}

public class Door
{
	public bool Exists { get; set; } = true;    // false once broken down
	public bool Locked { get; set; } = false;
	public bool Open   { get; set; } = false;   // cosmetic/for monsters; party passes regardless when unlocked

	public string KeyId { get; set; } = "";
	public int LockDC  { get; set; } = 0;       // 0 = cannot be picked
	public int BreakDC { get; set; } = 0;       // 0 = cannot be broken
	public string UnlockFlag { get; set; } = "";

	public ClosedPassRule ClosedRule { get; set; } = ClosedPassRule.None;

	// Party passage: through a destroyed doorway, or any unlocked door
	// (they open it, walk through, close it behind them — state unchanged)
	public bool PartyCanPass => !Exists || !Locked;

	public bool CanPick  => Locked && LockDC  > 0;
	public bool CanBreak => Exists && BreakDC > 0;
}
