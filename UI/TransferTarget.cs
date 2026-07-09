public class TransferTarget
{
	public enum TargetType { Vault, PersonalInventory, EquipmentSlot, Loot, Slot, Shop }

	public TargetType Type { get; set; }
	public Character Character { get; set; }       // for inventory or equipment targets
	public EquipmentSlot EquipmentSlot { get; set; } // for equipment targets	
	public int SlotIndex { get; set; } = -1;

	public static TransferTarget ToVault() =>
		new TransferTarget { Type = TargetType.Vault };

	public static TransferTarget ToInventory(Character c) =>
		new TransferTarget { Type = TargetType.PersonalInventory, Character = c };

	public static TransferTarget ToEquipment(Character c, EquipmentSlot slot) =>
		new TransferTarget { Type = TargetType.EquipmentSlot, Character = c, EquipmentSlot = slot };		

	public ShopState Shop { get; set; }  // set when Type == Shop

	public static TransferTarget ToShop(ShopState shop) =>
		new TransferTarget { Type = TargetType.Shop, Shop = shop };
		
	public static TransferTarget ToInventorySlot(Character c, int slotIndex) =>
		new TransferTarget 
		{ 
			Type = TargetType.PersonalInventory, 
			Character = c, 
			SlotIndex = slotIndex 
		};		
		

	public static TransferTarget ToLoot() =>
		new TransferTarget { Type = TargetType.Loot };
}
