using Godot;
using System.Collections.Generic;

public enum MonsterStatus { Ok, Asleep, Paralyzed, Poisoned, Dead }

public partial class Monster : GodotObject
{
	// Identity
	public string Name { get; set; }
	public int Level { get; set; }
	public string Id { get; set; } = "";
	public string Portrait { get; set; } = "";
	
	// HP
	public int MaxHP { get; set; }
	public int CurrentHP { get; set; }

	// Mana
	public int MaxMP { get; set; }
	public int CurrentMP { get; set; }

	// State
	public MonsterStatus Status { get; set; } = MonsterStatus.Ok;

	// Rewards
	public int ExperienceReward { get; set; }
	public int GoldReward { get; set; }

	// Combat
	public int Initiative { get; set; }
	public List<Attack> Attacks { get; set; } = new List<Attack>();
	public List<string> AttackIds { get; set; } = new List<string>();
	public string CombatLabel { get; set; } = "";  // "Skeleton A"
	public int ArmorClass { get; set; } = 0;

	// Resistances - damage type to multiplier
	// 1.0 = normal, 0.5 = resistant, 0.0 = immune, 1.5 = vulnerable
	public Dictionary<string, float> Resistances { get; set; } = new Dictionary<string, float>();

	// Derived
	public bool IsAlive => Status != MonsterStatus.Dead && CurrentHP > 0;

	//Graphics
	public string Sprite { get; set; }	

	public void TakeDamage(int amount)
	{
		CurrentHP = Mathf.Max(0, CurrentHP - amount);
		if (CurrentHP <= 0)
			Status = MonsterStatus.Dead;
	}

	public void Initialize()
	{
		CurrentHP = MaxHP;
		CurrentMP = MaxMP;
	}
}
