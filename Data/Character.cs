using Godot;
using System.Collections.Generic;

public enum Race { Human, Dwarf, Halfling }
public enum ClassType { Fighter, Thief, Priest, Mage }
public enum Alignment { Good, Neutral, Evil }
public enum Status { Ok, Asleep, Paralyzed, Poisoned, Dead, Ashes, Lost }
public enum Location { Party, Stable, Dungeon }

public partial class Character : GodotObject
{
	//Graphics
	public string Portrait { get; set; }
	public string BattleSprite { get; set; }
	public float SpriteTopOffset { get; set; } = 0f;
	public float SpriteRightOffset { get; set; } = 0f;
	
	// Identity
	public string Name { get; set; }
	public Race Race { get; set; }
	public ClassType ClassType { get; set; }
	public Alignment Alignment { get; set; } = Alignment.Neutral;
	public string Gender { get; set; }
	
	// Flavor
	public int Age { get; set; }
	public float Height { get; set; }  // in inches, convert for display
	public float Weight { get; set; }  // in lbs
	public string Backstory { get; set; }

	// Core Stats
	public int Strength { get; set; }
	public int Constitution { get; set; }
	public int Dexterity { get; set; }
	public int Intelligence { get; set; }
	public int Wisdom { get; set; }
	public int Charisma { get; set; }

	// Progression
	public int Level { get; set; } = 1;
	public int Experience { get; set; } = 0;
	public int ExperienceToNextLevel => Level * 1000;

	// State
	public Status Status { get; set; } = Status.Ok;
	public Location Location { get; set; } = Location.Stable;
	public List<StatusEffect> ActiveEffects { get; set; } = new List<StatusEffect>();

	// HP
	public int MaxHP => CalculateMaxHP();
	public int CurrentHP { get; set; }

	// Mana
	public int MaxMana => CalculateMaxMana();
	public int CurrentMana { get; set; }

	// Encumbrance
	public int MaxEncumbrance => CalculateEncumbrance();
	public int CurrentEncumbrance { get; set; } = 0;

	// Equipment slots
	public Dictionary<EquipmentSlot, Equipment> EquippedItems { get; set; } 
		= new Dictionary<EquipmentSlot, Equipment>();
	public Inventory PersonalInventory { get; set; }
	
	//Abilities
	public List<string> KnownAbilities { get; set; } = new List<string>();
	public Dictionary<string, int> CombatCooldowns { get; set; } = new Dictionary<string, int>();
	public Dictionary<string, float> ExplorationCooldowns { get; set; } = new Dictionary<string, float>();


	// Current weapon — main hand, or null
	public Equipment CurrentWeapon => GetEquipped(EquipmentSlot.WeaponMain);
	
	// --- Derived Stat Methods ---
	private int CalculateMaxHP()
	{
		// Base HP by class + CON modifier
		int baseHP = ClassType switch
		{
			ClassType.Fighter => 10,
			ClassType.Thief   => 6,
			ClassType.Priest  => 8,
			ClassType.Mage    => 4,
			_             => 6
		};
		int conModifier = (TotalConstitution() - 10) / 2;
		return (baseHP + conModifier) * Level;
	}

	private int CalculateMaxMana()
	{
		// Non-casters have no mana
		if (ClassType != ClassType.Priest && ClassType != ClassType.Mage) return 0;

		int baseMana = ClassType switch
		{
			ClassType.Mage   => 10,
			ClassType.Priest => 8,
			_            => 0
		};
		int wisIntModifier = ClassType == ClassType.Mage
			? (TotalIntelligence() - 10) / 2
			: (TotalWisdom() - 10) / 2;

		return (baseMana + wisIntModifier) * Level;
	}

	private int CalculateEncumbrance()
	{
		return Strength * 10;
	}

	// --- Helper Methods ---

	public bool IsAlive => Status != Status.Dead && Status != Status.Lost;

	public bool CanCast => IsAlive
						&& (ClassType == ClassType.Mage || ClassType == ClassType.Priest)
						&& CurrentMana > 0;

	public void GainExperience(int amount)
	{
		Experience += amount;
		while (Experience >= ExperienceToNextLevel)
		{
			Experience -= ExperienceToNextLevel;
			LevelUp();
		}
	}

	private void LevelUp()
	{
		Level++;
		// Restore HP and Mana on level up
		CurrentHP = MaxHP;
		CurrentMana = MaxMana;
		GD.Print($"{Name} reached level {Level}!");
	}

	public void Initialize()
	{
		CurrentHP = MaxHP;
		CurrentMana = MaxMana;
		PersonalInventory = new Inventory(12, MaxEncumbrance, 50);
	}
	
	// Equip an item — returns false if requirements not met or slot conflict
	public bool Equip(Equipment item)
	{
		if (!item.CanEquip(this)) return false;

		// Two handed weapons block off-hand slot
		if (item.IsTwoHanded && EquippedItems.ContainsKey(EquipmentSlot.OffHand))
			Unequip(EquipmentSlot.OffHand);

		// Equipping to main hand while two-hander equipped — remove two-hander
		if (item.Slot == EquipmentSlot.OffHand 
			&& EquippedItems.ContainsKey(EquipmentSlot.WeaponMain)
			&& EquippedItems[EquipmentSlot.WeaponMain].IsTwoHanded)
			Unequip(EquipmentSlot.WeaponMain);

		EquippedItems[item.Slot] = item;
		return true;
	}
	
	// Unequip a slot
	public Equipment Unequip(EquipmentSlot slot)
	{
		if (!EquippedItems.ContainsKey(slot)) return null;
		var item = EquippedItems[slot];
		EquippedItems.Remove(slot);
		return item;
	}
	
	// Get item in a slot
	public Equipment GetEquipped(EquipmentSlot slot)
	{
		return EquippedItems.ContainsKey(slot) ? EquippedItems[slot] : null;
	}

	// Derived stat helpers that account for equipment
	public int TotalArmorClass()
	{
		int ac = 0;
		foreach (var item in EquippedItems.Values)
			ac += item.ArmorClass;
		return ac;
	}
	
	public int TotalStrength()     => Strength     + EquipmentBonus(e => e.BonusStrength);
	public int TotalConstitution() => Constitution + EquipmentBonus(e => e.BonusConstitution);
	public int TotalDexterity()    => Dexterity    + EquipmentBonus(e => e.BonusDexterity);
	public int TotalIntelligence() => Intelligence + EquipmentBonus(e => e.BonusIntelligence);
	public int TotalWisdom()       => Wisdom       + EquipmentBonus(e => e.BonusWisdom);
	public int TotalCharisma()     => Charisma     + EquipmentBonus(e => e.BonusCharisma);

	private int EquipmentBonus(System.Func<Equipment, int> selector)
	{
		int total = 0;
		foreach (var item in EquippedItems.Values)
			total += selector(item);
		return total;
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
	
	// Combat usability — checks mana, health, and COMBAT cooldown
	public bool CanUseAbility(Ability ability)
	{
		if (CurrentMana < ability.ManaCost) return false;
		if (CurrentHP <= ability.HealthCost) return false;
		if (CombatCooldowns.TryGetValue(ability.Id, out int cd) && cd > 0) return false;
		return true;
	}

	// Dungeon usability — checks mana, health, dungeon-legality, and EXPLORATION cooldown
	public bool CanUseAbilityInDungeon(Ability ability)
	{
		if (!ability.CanUseIn("Dungeon")) return false;
		if (CurrentMana < ability.ManaCost) return false;
		if (CurrentHP <= ability.HealthCost) return false;
		if (GetExplorationCooldownRemaining(ability.Id) > 0f) return false;
		return true;
	}
	
	public float GetExplorationCooldownRemaining(string abilityId)
	{
		if (!ExplorationCooldowns.TryGetValue(abilityId, out float expiry)) return 0f;
		return Mathf.Max(0f, expiry - DungeonClock.Current);
	}
}
