using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class ShopManager
{
	private const float BuyMultiplier  = 1.5f;   // buy at 150% of goldCost
	private const float SellMultiplier = 0.35f;  // sell at 35% of goldCost

	public static int GetBuyPrice(Equipment item)
		=> Mathf.RoundToInt(item.GoldCost * BuyMultiplier);

	public static int GetSellPrice(Equipment item)
		=> Mathf.RoundToInt(item.GoldCost * SellMultiplier);

	// Load a shop definition from JSON into runtime state
	public static ShopState LoadShop(string shopId)
	{
		string path = $"res://Data/Shops/{shopId}.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Shop not found: {path}");
			return null;
		}

		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var def = JsonSerializer.Deserialize<ShopData>(json, options);
		if (def == null) return null;

		var state = new ShopState { Id = def.Id, Name = def.Name };
		foreach (var entry in def.Stock)
			state.BaseStock[entry.Id] = entry;

		return state;
	}

	// Attempt a purchase. Returns true on success. Item goes to target inventory.
	public static bool Buy(
		ShopState shop, Equipment item, GameState gameState,
		Inventory targetInventory, out string message)
	{
		int price = GetBuyPrice(item);

		if (gameState.Gold < price)
		{
			message = "Insufficient Gold";
			return false;
		}

		// Check target can hold it (weight/space)
		var testClone = item.CloneEquipment();
		testClone.StackCount = 1;
		int leftover = targetInventory.AddItem(testClone, 1);
		if (leftover > 0)
		{
			// Couldn't fit — remove what we added, fail
			targetInventory.RemoveItem(testClone, 1 - leftover);
			message = "No room";
			return false;
		}

		gameState.Gold -= price;

		// If dynamic stock, decrement / remove
		if (!shop.IsBaseStock(item.Id))
		{
			var stack = shop.DynamicStock.Find(i => i.Id == item.Id);
			if (stack != null)
			{
				stack.StackCount--;
				if (stack.StackCount <= 0)
					shop.DynamicStock.Remove(stack);
			}
		}

		message = $"Bought {item.Name}";
		return true;
	}

	// Sell an item from a source inventory. Returns gold gained.
	public static int Sell(
		ShopState shop, Equipment item, int count,
		GameState gameState, Inventory sourceInventory, out string message)
	{
		int unitPrice = GetSellPrice(item);
		int total     = unitPrice * count;

		// Remove from source
		sourceInventory.RemoveItem(item, count);

		gameState.Gold += total;

		// Add to shop stock — but only if NOT unlimited base stock
		if (!shop.IsBaseStock(item.Id))
		{
			var stack = shop.DynamicStock.Find(i => i.Id == item.Id);
			if (stack != null)
			{
				stack.StackCount += count;
			}
			else
			{
				var newStack = item.CloneEquipment();
				newStack.StackCount = count;
				shop.DynamicStock.Add(newStack);
			}
		}
		// If base stock (unlimited), the sold item just vanishes into infinite supply

		message = $"Sold {item.Name} for {total} gold";
		return total;
	}
}
