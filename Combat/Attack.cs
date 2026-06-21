using System.Collections.Generic;

public enum AttackType { Physical, Magical, Healing, StatusEffect }
public enum DamageType { Slashing, Piercing, Bludgeoning, Fire, Ice, Lightning, Holy, Shadow }
public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }

public class Attack
{
	// Identity
	public string Name { get; set; }
	public string Description { get; set; }

	// Type
	public AttackType AttackType { get; set; }
	public DamageType DamageType { get; set; }
	public TargetType TargetType { get; set; }

	// Damage
	public int BaseDamage { get; set; }
	public int DamageVariance { get; set; }   // random +/- range

	// Mana cost
	public int ManaCost { get; set; } = 0;

	// Status effect chance (0.0 - 1.0)
	public float StatusChance { get; set; } = 0.0f;
	public MonsterStatus StatusEffect { get; set; } = MonsterStatus.Ok;

	// Accuracy (0.0 - 1.0)
	public float Accuracy { get; set; } = 0.95f;
}
