public enum AbilityType { Spell, Skill }
public enum AbilityTargetType { SingleEnemy, AllEnemies, EnemyRow, SingleAlly, AllAllies, Self }
public enum AbilityEffectType { Damage, Heal, ApplyStatus, CureStatus, Buff }

public class Ability
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public AbilityType Type { get; set; } = AbilityType.Spell;
	public AbilityTargetType TargetType { get; set; }
	public AbilityEffectType EffectType { get; set; }

	public int Power { get; set; } = 0;
	public int ManaCost { get; set; } = 0;
	public int HealthCost { get; set; } = 0;
	public int Cooldown { get; set; } = 0;

	public string Element { get; set; } = "None";
	public string StatusEffect { get; set; } = "";
	public string ClassRestriction { get; set; } = "";

	public string Icon { get; set; } = "";
}
