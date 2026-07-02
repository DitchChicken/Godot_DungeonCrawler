using Godot;

public enum StatusType
{
	// Incapacitating
	Asleep,
	Paralyzed,
	Poisoned,
	// Permanent/out-of-combat
	Dead,
	Lost,
	// (buffs/debuffs added here later — Blessed, Slowed, Shielded, etc.)
}

public class StatusEffect
{
	public StatusType Type { get; set; }
	public int Duration { get; set; }       // rounds remaining; -1 = permanent
	public int Potency { get; set; } = 1;   // stacks (e.g. poison x6)
	public Color PotencyColor { get; set; } = new Color(1.0f, 0.85f, 0.0f); // default golden
	
	// Whether this effect is removed when combat ends
	public bool ClearsAtCombatEnd { get; set; } = true;

	// Icon path for display
	public string Icon { get; set; } = "";

	public StatusEffect(StatusType type, int duration, int potency = 1)
	{
		Type     = type;
		Duration = duration;
		Potency  = potency;
		SetDefaults();
	}

	private void SetDefaults()
	{
		switch (Type)
		{
			case StatusType.Poisoned:
				Icon              = "res://Combat/StatusIcons/Poison.png";
				ClearsAtCombatEnd = false;
				PotencyColor      = new Color(0.3f, 1.0f, 0.3f); // green
				break;
			case StatusType.Asleep:
				Icon              = "res://Combat/StatusIcons/Sleep.png";
				ClearsAtCombatEnd = true;
				PotencyColor      = new Color(1.0f, 0.85f, 0.0f); // golden
				break;
			case StatusType.Paralyzed:
				Icon              = "res://Combat/StatusIcons/Paralyze.png";
				ClearsAtCombatEnd = true;
				PotencyColor      = new Color(1.0f, 0.85f, 0.0f); // golden
				break;
				
		}
	}

	// True if this status prevents the combatant from acting this turn
	public bool PreventsAction()
	{
		return Type == StatusType.Asleep
			|| Type == StatusType.Paralyzed
			|| Type == StatusType.Dead
			|| Type == StatusType.Lost;
	}
}
