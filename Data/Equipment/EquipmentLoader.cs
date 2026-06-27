using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;


public static class EquipmentLoader
{	
	private static Dictionary<string, Equipment> _cache 
		= new Dictionary<string, Equipment>();

	public static Equipment LoadEquipment(string equipmentId)
	{
		if (_cache.ContainsKey(equipmentId))
			return _cache[equipmentId].CloneEquipment(); // always clone

		// Return cached version if already loaded
		if (_cache.ContainsKey(equipmentId))
			return _cache[equipmentId];

		// Search both Weapons and Armor folders
		string[] searchPaths =
		{
			$"res://Data/Equipment/Weapons/{equipmentId}.json",
			$"res://Data/Equipment/Armor/{equipmentId}.json",
			$"res://Data/Equipment/Misc/{equipmentId}.json",
			$"res://Data/Equipment/Treasure/{equipmentId}.json"
		};

		string json = null;
		foreach (var path in searchPaths)
		{
			var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (file != null)
			{
				json = file.GetAsText();
				file.Close();
				break;
			}
		}

		if (json == null)
		{
			GD.PrintErr($"Could not find equipment: {equipmentId}");
			return null;
		}

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		var def = JsonSerializer.Deserialize<EquipmentDef>(json, options);
		if (def == null)
		{
			GD.PrintErr($"Could not deserialize equipment: {equipmentId}");
			return null;
		}

		var equipment = new Equipment
		{
			Id                  = def.Id,
			Name                = def.Name,
			Description         = def.Description,
			Slot                = Enum.Parse<EquipmentSlot>(def.Slot),
			Rarity              = Enum.Parse<Rarity>(def.Rarity ?? "Common"),
			GoldCost            = def.GoldCost,
			Weight              = def.Weight,
			IsCursed            = def.IsCursed,
			IsIdentified        = def.IsIdentified,
			IsStackable         = def.IsStackable,
			Durability          = def.Durability > 0 ? def.Durability : 100,
			MaxDurability       = def.Durability > 0 ? def.Durability : 100,
			Charges             = def.Charges,
			RequiredStrength    = def.RequiredStrength,
			RequiredDexterity   = def.RequiredDexterity,
			RequiredIntelligence = def.RequiredIntelligence,
			RequiredLevel       = def.RequiredLevel,
			BonusStrength       = def.BonusStrength,
			BonusConstitution   = def.BonusConstitution,
			BonusDexterity      = def.BonusDexterity,
			BonusIntelligence   = def.BonusIntelligence,
			BonusWisdom         = def.BonusWisdom,
			BonusCharisma       = def.BonusCharisma,
			BonusHP             = def.BonusHP,
			BonusMana           = def.BonusMana,
			ArmorClass          = def.ArmorClass,
			IsLargeShield       = def.IsLargeShield,
			BaseDamageMin       = def.BaseDamageMin,
			BaseDamageMax       = def.BaseDamageMax,
			MagicBonus          = def.MagicBonus,
			IsTwoHanded         = def.IsTwoHanded,
			Range               = Enum.Parse<WeaponRange>(def.Range ?? "Melee"),
			Element             = Enum.Parse<DamageElement>(def.Element ?? "None"),
			Abilities           = def.Abilities ?? new System.Collections.Generic.List<string>(),
			UnknownName         = def.UnknownName ?? "Unknown Item",
			MaxStack            = def.MaxStack,
			InitiativeModifier = def.InitiativeModifier,
			Icon                = def.Icon ?? ""			
		};

		_cache[equipmentId] = equipment; // cache template
		return equipment.CloneEquipment(); // return clone
	}

	public static List<Equipment> LoadEquipmentList(List<string> equipmentIds)
	{
		var items = new List<Equipment>();
		foreach (var id in equipmentIds)
		{
			var item = LoadEquipment(id);
			if (item != null)
				items.Add(item);
		}
		return items;
	}

	public static void ClearCache()
	{
		_cache.Clear();
	}
}
