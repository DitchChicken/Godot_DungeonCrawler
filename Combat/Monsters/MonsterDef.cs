using System.Collections.Generic;

public class MonsterDef
{
	public string Name { get; set; }
	public int Level { get; set; }
	public int MaxHP { get; set; }
	public int MaxMP { get; set; }
	public int ExperienceReward { get; set; }
	public int GoldReward { get; set; }
	public int Initiative { get; set; }
	public Dictionary<string, float> Resistances { get; set; }
	public List<string> Attacks { get; set; }  // attack IDs, resolved later
	public string Sprite { get; set; }
}
