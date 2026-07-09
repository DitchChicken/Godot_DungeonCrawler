using Godot;
using System.Collections.Generic;

public class ShopState
{
	public string Id { get; set; }
	public string Name { get; set; }

	// Base stock — unlimited items always available (item id → entry)
	public Dictionary<string, ShopStockEntry> BaseStock { get; set; }
		= new Dictionary<string, ShopStockEntry>();

	// Dynamic stock — player-sold items not in base stock (item id → Equipment with StackCount)
	public List<Equipment> DynamicStock { get; set; } = new List<Equipment>();

	// Is this item part of the unlimited base stock?
	public bool IsBaseStock(string itemId) => BaseStock.ContainsKey(itemId);

	// All items currently purchasable (base + dynamic), as display entries
	public List<Equipment> GetDisplayStock()
	{
		var result = new List<Equipment>();

		// Base stock — one display entry each (unlimited)
		foreach (var entry in BaseStock.Values)
		{
			var item = EquipmentLoader.LoadEquipment(entry.Id);
			if (item != null)
			{
				item.StackCount = 1; // display; unlimited so count is cosmetic
				result.Add(item);
			}
		}

		// Dynamic stock — actual finite stacks
		foreach (var item in DynamicStock)
			result.Add(item);

		return result;
	}
}
