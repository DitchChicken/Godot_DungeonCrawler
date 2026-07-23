using System.Collections.Generic;

public class Interaction
{
	public string Id { get; set; }
	public string Name { get; set; }              // button label
	public bool OneShot { get; set; } = true;
	public float TimeCost { get; set; } = 1.0f;
	public bool Hidden { get; set; } = false;

	// Requirements to show/enable (empty = always available)
	public List<Requirement> Requires { get; set; } = new List<Requirement>();

	// Optional domain check. Null = automatic success, use Outcomes.
	public Check Check { get; set; }

	// Flat outcomes when there's no check
	public List<Outcome> Outcomes { get; set; } = new List<Outcome>();

	// Tiered outcomes when there is a check
	public TieredOutcomes CheckOutcomes { get; set; }
}

public class Requirement
{
	public string Type { get; set; }   // "Flag", "Item", "ClassInParty", "ActionCompleted"
	public string Value { get; set; }
}

public class Check
{
	public string Domain { get; set; } = "";      // "" = no domain gate (level 0)
	public string Stat { get; set; } = "Intelligence";
	public int Difficulty { get; set; } = 10;     // DC
	public int Level { get; set; } = 0;           // required domain level to attempt
	public int CriticalThreshold { get; set; } = -1; // -1 = no crit tier
}

public class TieredOutcomes
{
	public List<Outcome> CriticalSuccess { get; set; } = new List<Outcome>();
	public List<Outcome> Success { get; set; } = new List<Outcome>();
	public List<Outcome> Failure { get; set; } = new List<Outcome>();
	public List<Outcome> CriticalFailure { get; set; } = new List<Outcome>();
}

public class Outcome
{
	public string Type { get; set; }        // see OutcomeType strings below
	public string Text { get; set; } = "";
	public string Room { get; set; } = "";
	public string Direction { get; set; } = "";
	public string State { get; set; } = "";
	public string Flag { get; set; } = "";
	public string ItemId { get; set; } = "";
	public int Amount { get; set; } = 0;
	public string EncounterId { get; set; } = "";
	public string ActionId { get; set; } = "";
}
