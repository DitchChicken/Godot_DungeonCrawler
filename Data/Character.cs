using Godot;

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

	// HP
	public int MaxHP => CalculateMaxHP();
	public int CurrentHP { get; set; }

	// Mana
	public int MaxMana => CalculateMaxMana();
	public int CurrentMana { get; set; }

	// Encumbrance
	public int MaxEncumbrance => CalculateEncumbrance();
	public int CurrentEncumbrance { get; set; } = 0;

	// Equipment slots — placeholder
	// public Equipment Weapon, Armor, Shield, Helmet, Gauntlets;

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
		int conModifier = (Constitution - 10) / 2;
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
			? (Intelligence - 10) / 2
			: (Wisdom - 10) / 2;

		return (baseMana + wisIntModifier) * Level;
	}

	private int CalculateEncumbrance()
	{
		return Strength * 10;
	}

	// --- Helper Methods ---

	public bool IsAlive => Status != Status.Dead
						&& Status != Status.Ashes
						&& Status != Status.Lost;

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

	public void InitializeHP()
	{
		CurrentHP = MaxHP;
		CurrentMana = MaxMana;
	}
}
