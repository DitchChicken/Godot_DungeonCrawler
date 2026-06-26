using System.Collections.Generic;

public enum WeaponRange  { Melee, Short, Medium, Long }
public enum Rarity       { Common, Uncommon, Rare, Legendary }
public enum DamageElement { None, Fire, Ice, Lightning, Holy, Shadow }

public class Equipment
{
	// Identity
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public EquipmentSlot Slot { get; set; }
	public Rarity Rarity { get; set; } = Rarity.Common;
	
	//Graphics
	public string Icon { get; set; }
	
	// Economy
	public int GoldCost { get; set; }
	public float Weight { get; set; }

	// State flags
	public bool IsCursed { get; set; }      = false;
	public bool IsIdentified { get; set; }  = true;  // false for mystery loot
	public bool IsStackable { get; set; }   = false;
	public int Durability { get; set; }     = 100;
	public int MaxDurability { get; set; }  = 100;
	public int Charges { get; set; }        = 0;     // 0 = not a charged item

	// Stat requirements
	public int RequiredStrength { get; set; }     = 0;
	public int RequiredDexterity { get; set; }    = 0;
	public int RequiredIntelligence { get; set; } = 0;
	public int RequiredLevel { get; set; }        = 0;

	// Stat bonuses (positive or negative)
	public int BonusStrength { get; set; }     = 0;
	public int BonusConstitution { get; set; } = 0;
	public int BonusDexterity { get; set; }    = 0;
	public int BonusIntelligence { get; set; } = 0;
	public int BonusWisdom { get; set; }       = 0;
	public int BonusCharisma { get; set; }     = 0;
	public int BonusHP { get; set; }           = 0;
	public int BonusMana { get; set; }         = 0;

	// Defense
	public int ArmorClass { get; set; }        = 0;
	public bool IsLargeShield { get; set; }    = false;  // shields only

	// Weapon specific
	public int BaseDamageMin { get; set; }     = 0;
	public int BaseDamageMax { get; set; }     = 0;
	public int MagicBonus { get; set; }        = 0;      // the +1/+2/+3
	public bool IsTwoHanded { get; set; }      = false;
	public WeaponRange Range { get; set; }     = WeaponRange.Melee;
	public DamageElement Element { get; set; } = DamageElement.None;

	//Stacking
	public int MaxStack { get; set; } = 1;      // 1 = not stackable
	public int StackCount { get; set; } = 1;    // current stack size
	
	// Special abilities
	public List<string> Abilities { get; set; } = new List<string>();
	// e.g. "Cleave", "Parry", "Backstab" - resolved by combat system

	// Unidentified display name
	public string UnknownName { get; set; } = "Unknown Item";

	// Display name respects identification state
	public string DisplayName => IsIdentified ? Name : UnknownName;

	public int InitiativeModifier { get; set; } = 0;
	
	// Can this character equip this item?
	public bool CanEquip(Character character)
	{
		if (character.Strength     < RequiredStrength)     return false;
		if (character.Dexterity    < RequiredDexterity)    return false;
		if (character.Intelligence < RequiredIntelligence) return false;
		if (character.Level        < RequiredLevel)        return false;
		return true;
	}
	
	public Equipment CloneEquipment()
	{
		return new Equipment
		{
			Id = Id, Name = Name, Description = Description, Slot = Slot,
			Rarity = Rarity, GoldCost = GoldCost, Weight = Weight,
			IsCursed = IsCursed, IsIdentified = IsIdentified, IsStackable = IsStackable,
			MaxStack = MaxStack, StackCount = StackCount,
			Durability = Durability, MaxDurability = MaxDurability, Charges = Charges,
			RequiredStrength = RequiredStrength, RequiredDexterity = RequiredDexterity,
			RequiredIntelligence = RequiredIntelligence, RequiredLevel = RequiredLevel,
			BonusStrength = BonusStrength, BonusConstitution = BonusConstitution,
			BonusDexterity = BonusDexterity, BonusIntelligence = BonusIntelligence,
			BonusWisdom = BonusWisdom, BonusCharisma = BonusCharisma,
			BonusHP = BonusHP, BonusMana = BonusMana,
			ArmorClass = ArmorClass, IsLargeShield = IsLargeShield,
			BaseDamageMin = BaseDamageMin, BaseDamageMax = BaseDamageMax,
			MagicBonus = MagicBonus, IsTwoHanded = IsTwoHanded,
			Range = Range, Element = Element, Abilities = Abilities,
			Icon = Icon, UnknownName = UnknownName, InitiativeModifier = InitiativeModifier
		};
	}
}
