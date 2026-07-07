public class AbilityDef
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string AbilityType { get; set; } = "Spell";
	public string TargetType { get; set; }
	public string EffectType { get; set; }
	public int Power { get; set; }
	public int ManaCost { get; set; }
	public int HealthCost { get; set; }
	public int Cooldown { get; set; }
	public string Element { get; set; } = "None";
	public string StatusEffect { get; set; } = "";
	public string ClassRestriction { get; set; } = "";
	public string Icon { get; set; } = "";
}
