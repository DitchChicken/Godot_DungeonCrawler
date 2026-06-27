using Godot;

public static class InventoryTransfer
{
	// Result of a transfer attempt
	public enum Result { Success, PartialSuccess, Failed }

	// Main entry point — moves an item from a source to a target.
	// Returns true if anything was transferred.
	public static bool Transfer(
		InventoryDragData dragData,
		TransferTarget target,
		GameState gameState)
	{
		var item  = dragData.Item;
		int count = dragData.Count;

		if (item == null || count <= 0)
			return false;

		// Determine how many we'll move
		int moveCount = CalculateMoveCount(item, count, target, gameState);
		if (moveCount <= 0)
		{
			GD.Print($"Nothing can be transferred (target full or capped)");
			return false;
		}

		// Remove from source first
		RemoveFromSource(dragData, moveCount, gameState);

		// Add to target — returns leftover that didn't fit
		int leftover = AddToTarget(item, moveCount, target, gameState);

		// Return leftover to source
		if (leftover > 0)
		{
			GD.Print($"{leftover} couldn't fit — returning to source");
			ReturnToSource(dragData, leftover, gameState);
		}

		return leftover < moveCount;
	}

	// How many items should move given the target's constraints
	private static int CalculateMoveCount(
		Equipment item, int available, TransferTarget target, GameState gameState)
	{
		switch (target.Type)
		{
			case TransferTarget.TargetType.Vault:
				// Vault accepts everything
				return available;

			case TransferTarget.TargetType.PersonalInventory:
				// Cap stackables at one max stack — but don't merge into existing
				// partials. A full or partial incoming stack lands in its own slot.
				if (item.MaxStack > 1)
					return System.Math.Min(available, item.MaxStack);
				return available;

			case TransferTarget.TargetType.EquipmentSlot:
				// Equipment ammo slot still caps at one max stack
				if (item.MaxStack > 1)
					return System.Math.Min(available, item.MaxStack);
				return available;
				
			case TransferTarget.TargetType.Loot:
				return available;
				
			default:
				return available;
		}
	}

	private static void RemoveFromSource(
		InventoryDragData dragData, int count, GameState gameState)
	{
		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.RemoveItem(dragData.Item, count);
		}
		else if (dragData.Source == InventoryDragData.SourceType.Loot)
		{
			VictoryScreen.ActiveLootInventory?.RemoveItem(dragData.Item, count);
		}
		else
		{
			dragData.Character?.PersonalInventory.RemoveItem(dragData.Item, count);
		}
	}

	// Returns how many couldn't be added
	private static int AddToTarget(
		Equipment item, int count, TransferTarget target, GameState gameState)
	{
		switch (target.Type)
		{
			case TransferTarget.TargetType.Vault:
				gameState.PartyVault.AddItem(item, count);
				return 0;

			case TransferTarget.TargetType.Loot:
				VictoryScreen.ActiveLootInventory?.AddItem(item, count);
				return 0;
				
			case TransferTarget.TargetType.PersonalInventory:
				var inv = target.Character?.PersonalInventory;
				if (inv == null) return count;

				// Check if dropping onto a specific slot with a mergeable stack
				if (target.SlotIndex >= 0 && target.SlotIndex < inv.Items.Count)
				{
					var targetStack = inv.Items[target.SlotIndex];

					// Same item and stackable — merge up to max
					if (targetStack.Id == item.Id 
						&& item.MaxStack > 1 
						&& targetStack.StackCount < item.MaxStack)
					{
						int room   = item.MaxStack - targetStack.StackCount;
						int merged = System.Math.Min(room, count);
						targetStack.StackCount += merged;
						return count - merged; // overflow returns to source
					}

					// Different item in the slot, or non-stackable — don't merge,
					// fall through to placing in a new slot
				}

				return inv.AddItemNoMerge(item, count);
			case TransferTarget.TargetType.EquipmentSlot:
				if (target.Character == null) return count;
				// Validate slot match
				if (item.Slot != target.EquipmentSlot) return count;

				// Unequip whatever's there back to the same character's inventory
				var existing = target.Character.GetEquipped(target.EquipmentSlot);
				if (existing != null)
				{
					target.Character.Unequip(target.EquipmentSlot);
					int back = target.Character.PersonalInventory.AddItem(existing, existing.StackCount);
					// If old item can't fit inventory, it's lost — rare but log it
					if (back > 0)
						GD.PrintErr($"Couldn't return {existing.Name} to inventory");
				}

				// Equip a clone so we don't mutate the source stack
				var toEquip = item.CloneEquipment();
				toEquip.StackCount = count;
				target.Character.Equip(toEquip);
				return 0;

			default:
				return count;
		}
	}

	private static void ReturnToSource(
		InventoryDragData dragData, int count, GameState gameState)
	{
		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			// Re-equip what we took
			var toEquip = dragData.Item.CloneEquipment();
			toEquip.StackCount = count;
			if (!dragData.Character.Equip(toEquip))
				dragData.Character.PersonalInventory.AddItem(dragData.Item, count);
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.AddItem(dragData.Item, count);
		}
		else
		{
			dragData.Character?.PersonalInventory.AddItem(dragData.Item, count);
		}
	}
}
