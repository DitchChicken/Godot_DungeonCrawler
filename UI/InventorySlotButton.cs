using Godot;

public partial class InventorySlotButton : Button
{
	public Equipment Item { get; set; }
	public int SlotIndex { get; set; }
	public InventoryDragData.SourceType SourceType { get; set; }
	public Character Character { get; set; }
	public bool IsEquipmentSlot { get; set; } = false;
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		
		//GD.Print($"_GetDragData: {Item?.Name} SourceType:{SourceType} SlotIndex:{SlotIndex}");
		
		// For equipment doll slots, get item from character's equipped items
		if (Item == null && Character != null)
		{
			var equipSlot = (EquipmentSlot)SlotIndex;
			Item = Character.GetEquipped(equipSlot);
		}

		if (Item == null) return new Variant();

		// Capture count NOW before any operations can zero it out
		int capturedCount = Item.StackCount;
		GD.Print($"_GetDragData: {Item.Name} x{capturedCount} fromEquip:{IsEquipmentSlot}");

		var preview = new TextureRect();
		preview.CustomMinimumSize = new Vector2(64, 64);
		preview.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
		preview.StretchMode       = TextureRect.StretchModeEnum.KeepAspect;
		if (Icon != null) 
			preview.Texture = Icon;
		else if (!string.IsNullOrEmpty(Item.Icon) && ResourceLoader.Exists(Item.Icon))
			preview.Texture = GD.Load<Texture2D>(Item.Icon);
		SetDragPreview(preview);

		var data = new InventoryDragData
		{
			Source          = SourceType,
			Item            = Item,
			Count           = capturedCount,  // store captured count
			SlotIndex       = SlotIndex,
			Character       = Character,
			IsEquipmentSlot = IsEquipmentSlot
		};

		return Variant.From(data);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		//GD.Print($"_CanDropData called");
		
		var dragData = data.As<InventoryDragData>();
		if (dragData == null) return false;

		// Can't drop on itself
		if (dragData.Source == SourceType && dragData.SlotIndex == SlotIndex)
			return false;

		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dragData = data.As<InventoryDragData>();
		if (dragData == null) return;

		//GD.Print($"_DropData on slot {SlotIndex} IsEquipSlot:{IsEquipmentSlot}");
		//GD.Print($"_DropData: SourceType:{dragData.Source} SlotIndex:{dragData.SlotIndex}");
		
		if (IsEquipmentSlot)
		{
			var equipSlot = (EquipmentSlot)SlotIndex;
			if (dragData.Item.Slot != equipSlot)
			{
				GD.Print($"Item {dragData.Item.Name} doesn't fit slot {equipSlot} — snapping back");
				return;
			}

			var gameState = GetNode<GameState>("/root/GameState");

			// Determine how many to equip — cap stackable items at one max stack
			int equipCount = dragData.Count;
			if (dragData.Item.MaxStack > 1)
				equipCount = System.Math.Min(dragData.Count, dragData.Item.MaxStack);

			// Unequip existing item in this slot
			var existing = Character?.GetEquipped(equipSlot);
			if (existing != null)
			{
				Character.Unequip(equipSlot);
				ReturnToSource(dragData, existing);
			}

			// Remove only the capped amount from source
			RemoveCountFromSource(dragData, equipCount);

			// Create an independent copy to equip (don't mutate the source stack)
			var itemToEquip = CloneForEquip(dragData.Item);
			itemToEquip.StackCount = equipCount;
			Character?.Equip(itemToEquip);

			var sheet = GetTree().Root.GetNodeOrNull<CharacterSheet>("/root/CharacterSheet");
			sheet?.RefreshDoll();
			var vault = GetTree().Root.GetNodeOrNull<Vault>("Vault");
			vault?.RefreshAll();
		}
		else
		{
			// Inventory slot — no equipment validation needed
			var vault = GetTree().Root.GetNodeOrNull<Vault>("Vault");
			GD.Print("Dropping from inventory to Vault src:{dragData.Source}");
			vault?.HandleDrop(dragData, SourceType, SlotIndex, Character);
		}
	}

	private void RemoveFromSource(InventoryDragData dragData)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			// Source was an equipment slot — unequip
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
			GD.Print($"Unequipped {dragData.Item.Name} from {equipSlot}");
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.RemoveItem(dragData.Item, dragData.Count);
		}
		else
		{
			// Source was personal inventory
			dragData.Character?.PersonalInventory.RemoveItem(dragData.Item, dragData.Count);
		}
	}
	
	private void ReturnToSource(InventoryDragData dragData, Equipment item)
	{
		var gameState = GetNode<GameState>("/root/GameState");
		if (dragData.Source == InventoryDragData.SourceType.Vault)
			gameState.PartyVault.AddItem(item, item.StackCount);
		else
			dragData.Character?.PersonalInventory.AddItem(item, item.StackCount);
	}
	
	private void RemoveCountFromSource(InventoryDragData dragData, int count)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			// Source was an equipment slot — unequip whole thing
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.RemoveItem(dragData.Item, count);
		}
		else
		{
			dragData.Character?.PersonalInventory.RemoveItem(dragData.Item, count);
		}
	}	
	
	private Equipment CloneForEquip(Equipment source)
	{
		return new Equipment
		{
			Id                   = source.Id,
			Name                 = source.Name,
			Description          = source.Description,
			Slot                 = source.Slot,
			Rarity               = source.Rarity,
			GoldCost             = source.GoldCost,
			Weight               = source.Weight,
			IsCursed             = source.IsCursed,
			IsIdentified         = source.IsIdentified,
			IsStackable          = source.IsStackable,
			MaxStack             = source.MaxStack,
			StackCount           = source.StackCount,
			Durability           = source.Durability,
			MaxDurability        = source.MaxDurability,
			Charges              = source.Charges,
			RequiredStrength     = source.RequiredStrength,
			RequiredDexterity    = source.RequiredDexterity,
			RequiredIntelligence = source.RequiredIntelligence,
			RequiredLevel        = source.RequiredLevel,
			BonusStrength        = source.BonusStrength,
			BonusConstitution    = source.BonusConstitution,
			BonusDexterity       = source.BonusDexterity,
			BonusIntelligence    = source.BonusIntelligence,
			BonusWisdom          = source.BonusWisdom,
			BonusCharisma        = source.BonusCharisma,
			BonusHP              = source.BonusHP,
			BonusMana            = source.BonusMana,
			ArmorClass           = source.ArmorClass,
			IsLargeShield        = source.IsLargeShield,
			BaseDamageMin        = source.BaseDamageMin,
			BaseDamageMax        = source.BaseDamageMax,
			MagicBonus           = source.MagicBonus,
			IsTwoHanded          = source.IsTwoHanded,
			Range                = source.Range,
			Element              = source.Element,
			Abilities            = source.Abilities,
			Icon                 = source.Icon,
			UnknownName          = source.UnknownName
		};
	}	
}
