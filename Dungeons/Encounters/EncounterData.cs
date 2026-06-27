using System.Collections.Generic;

public class EncounterData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public List<List<string>> Formation { get; set; }
	public EncounterRewards Rewards { get; set; }
}

public class EncounterRewards
{
	public int GoldMin { get; set; } = 0;
	public int GoldMax { get; set; } = 0;
	public List<EncounterItemReward> Items { get; set; } = new List<EncounterItemReward>();
}

public class EncounterItemReward
{
	public string Id { get; set; }
	public float Chance { get; set; } = 1.0f;
	public int CountMin { get; set; } = 1;
	public int CountMax { get; set; } = 1;
}
