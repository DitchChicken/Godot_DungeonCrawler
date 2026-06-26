public class AttackDef
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string AttackType { get; set; }
	public string DamageType { get; set; }
	public string TargetType { get; set; }
	public int BaseDamageMin { get; set; }
	public int BaseDamageMax { get; set; }
	public string StatusEffect { get; set; } = "";
	public float StatusChance { get; set; } = 0.0f;
	public int ManaCost { get; set; } = 0;
	public int HealthCost { get; set; } = 0;
	public float Accuracy { get; set; } = 0.95f;
	public int Weight { get; set; } = 1;
	public string Range { get; set; } = "Melee";
}
