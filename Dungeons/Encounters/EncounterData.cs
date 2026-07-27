using System.Collections.Generic;

public class EncounterData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public List<List<string>> Formation { get; set; }
	public EncounterRewards Rewards { get; set; }
	public string Disposition { get; set; } = "Hostile";
	public string InteractionRequirement { get; set; } = "Mandatory";
	public string SceneId { get; set; } = "";
	public string OverlayImage { get; set; } = "";
	public float OverlayX { get; set; } = 0.5f;
	public float OverlayY { get; set; } = 0.5f;
	public float OverlayScale { get; set; } = 1.0f;
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
