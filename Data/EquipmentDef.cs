using System.Collections.Generic;

public class EquipmentDef
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string Slot { get; set; }
	public string Rarity { get; set; }
	public int GoldCost { get; set; }
	public float Weight { get; set; }
	public bool IsCursed { get; set; }
	public bool IsIdentified { get; set; } = true;
	public bool IsStackable { get; set; }
	public int Durability { get; set; } = 100;
	public int Charges { get; set; }
	public int RequiredStrength { get; set; }
	public int RequiredDexterity { get; set; }
	public int RequiredIntelligence { get; set; }
	public int RequiredLevel { get; set; }
	public int BonusStrength { get; set; }
	public int BonusConstitution { get; set; }
	public int BonusDexterity { get; set; }
	public int BonusIntelligence { get; set; }
	public int BonusWisdom { get; set; }
	public int BonusCharisma { get; set; }
	public int BonusHP { get; set; }
	public int BonusMana { get; set; }
	public int ArmorClass { get; set; }
	public bool IsLargeShield { get; set; }
	public int BaseDamageMin { get; set; }
	public int BaseDamageMax { get; set; }
	public int MagicBonus { get; set; }
	public bool IsTwoHanded { get; set; }
	public string Range { get; set; }
	public string Element { get; set; }
	public List<string> Abilities { get; set; }
	public string UnknownName { get; set; }
	public string Icon { get; set; }
	public int MaxStack { get; set; } = 1;
	public int InitiativeModifier { get; set; } = 0;
	public string ConsumableType { get; set; } = "None";
	public string UseAbility { get; set; } = "";
}
