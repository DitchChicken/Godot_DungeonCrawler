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
	public float SpriteTopOffset { get; set; } = 0f;
	public float SpriteRightOffset { get; set; } = 0f;
	
	// HP
	public int MaxHP { get; set; }
	public int CurrentHP { get; set; }

	// Mana
	public int MaxMP { get; set; }
	public int CurrentMP { get; set; }

	// State
	public MonsterStatus Status { get; set; } = MonsterStatus.Ok;
	public List<StatusEffect> ActiveEffects { get; set; } = new List<StatusEffect>();

	// Rewards
	public int ExperienceReward { get; set; }
	public int GoldReward { get; set; }
	public List<EncounterItemReward> Drops { get; set; } = new List<EncounterItemReward>();

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
	
	// Add or stack an effect
	public void AddStatusEffect(StatusEffect effect)
	{
		// If same type already present, stack potency and refresh duration
		foreach (var existing in ActiveEffects)
		{
			if (existing.Type == effect.Type)
			{
				existing.Potency += effect.Potency;
				existing.Duration = System.Math.Max(existing.Duration, effect.Duration);
				return;
			}
		}
		ActiveEffects.Add(effect);
	}

	public bool HasStatus(StatusType type)
	{
		return ActiveEffects.Exists(e => e.Type == type);
	}

	public void RemoveStatus(StatusType type)
	{
		ActiveEffects.RemoveAll(e => e.Type == type);
	}

	// Can this character take an action right now?
	public bool CanAct()
	{
		if (!IsAlive) return false;
		foreach (var e in ActiveEffects)
			if (e.PreventsAction()) return false;
		return true;
	}
}
