using Godot;
using System.Collections.Generic;
using System.Linq;

public class Inventory
{
	public int MaxSlots { get; set; }
	public float MaxWeight { get; set; }    // 0 = unlimited
	public int MaxStackSize { get; set; }   // 0 = unlimited
	public List<Equipment> Items { get; set; } = new List<Equipment>();

	public Inventory(int maxSlots, float maxWeight = 0, int maxStackSize = 0)
	{
		MaxSlots     = maxSlots;
		MaxWeight    = maxWeight;
		MaxStackSize = maxStackSize;
	}

	public float CurrentWeight => Items.Sum(i => i.Weight * i.StackCount);
	public int UsedSlots       => Items.Count;
	public bool IsFull         => UsedSlots >= MaxSlots;

	// Add item — returns remainder count if couldn't fit all
	public int AddItem(Equipment item, int count = 1)
	{
		int remaining = count;

		// Try to stack on existing stack first
		if (item.MaxStack > 1)
		{
			var existing = Items.FirstOrDefault(i => 
				i.Id == item.Id && i.StackCount < EffectiveMaxStack(item));

			if (existing != null)
			{
				int canAdd  = EffectiveMaxStack(item) - existing.StackCount;
				int adding  = System.Math.Min(canAdd, remaining);
				existing.StackCount += adding;
				remaining           -= adding;
			}
		}

		// Add new stacks for remainder
		while (remaining > 0 && !IsFull)
		{
			if (MaxWeight > 0 && CurrentWeight + item.Weight > MaxWeight)
				break;

			var newStack = CloneItem(item);
			int stackSize = System.Math.Min(remaining, EffectiveMaxStack(item));
			newStack.StackCount = stackSize;
			remaining          -= stackSize;
			Items.Add(newStack);
		}

		return remaining; // returns how many couldn't fit
	}

	// Remove item — returns how many were actually removed
	public int RemoveItem(Equipment item, int count = 1)
	{
		int remaining = count;
		var stacks    = Items.Where(i => i.Id == item.Id).ToList();

		foreach (var stack in stacks)
		{
			if (remaining <= 0) break;
			int removing = System.Math.Min(stack.StackCount, remaining);
			stack.StackCount -= removing;
			remaining        -= removing;

			if (stack.StackCount <= 0)
				Items.Remove(stack);
		}

		return count - remaining; // how many were removed
	}

	public bool HasItem(string itemId) =>
		Items.Any(i => i.Id == itemId);

	public int CountItem(string itemId) =>
		Items.Where(i => i.Id == itemId).Sum(i => i.StackCount);

	private int EffectiveMaxStack(Equipment item) =>
		MaxStackSize == 0 ? item.MaxStack : System.Math.Min(item.MaxStack, MaxStackSize);

	private Equipment CloneItem(Equipment item)
	{
		// Shallow clone for stacking
		return new Equipment
		{
			Id                   = item.Id,
			Name                 = item.Name,
			Description          = item.Description,
			Slot                 = item.Slot,
			Rarity               = item.Rarity,
			GoldCost             = item.GoldCost,
			Weight               = item.Weight,
			IsCursed             = item.IsCursed,
			IsIdentified         = item.IsIdentified,
			IsStackable          = item.IsStackable,
			MaxStack             = item.MaxStack,
			StackCount           = 1,
			Durability           = item.Durability,
			MaxDurability        = item.MaxDurability,
			Charges              = item.Charges,
			RequiredStrength     = item.RequiredStrength,
			RequiredDexterity    = item.RequiredDexterity,
			RequiredIntelligence = item.RequiredIntelligence,
			RequiredLevel        = item.RequiredLevel,
			BonusStrength        = item.BonusStrength,
			BonusConstitution    = item.BonusConstitution,
			BonusDexterity       = item.BonusDexterity,
			BonusIntelligence    = item.BonusIntelligence,
			BonusWisdom          = item.BonusWisdom,
			BonusCharisma        = item.BonusCharisma,
			BonusHP              = item.BonusHP,
			BonusMana            = item.BonusMana,
			ArmorClass           = item.ArmorClass,
			IsLargeShield        = item.IsLargeShield,
			BaseDamageMin        = item.BaseDamageMin,
			BaseDamageMax        = item.BaseDamageMax,
			MagicBonus           = item.MagicBonus,
			IsTwoHanded          = item.IsTwoHanded,
			Range                = item.Range,
			Element              = item.Element,
			Abilities            = item.Abilities,
			Icon                 = item.Icon,
			UnknownName          = item.UnknownName
		};
	}
}
