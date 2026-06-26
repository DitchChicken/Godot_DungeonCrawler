using System.Collections.Generic;

public enum AttackType { Physical, Magical, Healing, StatusEffect }
public enum DamageType { Slashing, Piercing, Bludgeoning, Fire, Ice, Lightning, Holy, Shadow }
public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self, EnemyRow }

public class Attack
{
	// Identity
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }

	// Type
	public AttackType AttackType { get; set; }
	public DamageType DamageType { get; set; }
	public TargetType TargetType { get; set; }

	// Damage range
	public int BaseDamageMin { get; set; } = 0;
	public int BaseDamageMax { get; set; } = 0;

	// Status effect
	public string StatusEffect { get; set; } = "";    // blank = no status
	public float StatusChance { get; set; } = 0.0f;  // 0.0-1.0

	// Mana/health cost (for monster spellcasters)
	public int ManaCost { get; set; } = 0;
	public int HealthCost { get; set; } = 0;

	// Accuracy
	public float Accuracy { get; set; } = 0.95f;

	// Weighting for AI selection
	public int Weight { get; set; } = 1;

	// Range
	public WeaponRange Range { get; set; } = WeaponRange.Melee;
}
