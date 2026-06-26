public class TransferTarget
{
	public enum TargetType { Vault, PersonalInventory, EquipmentSlot }

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
		
	public static TransferTarget ToInventorySlot(Character c, int slotIndex) =>
		new TransferTarget 
		{ 
			Type = TargetType.PersonalInventory, 
			Character = c, 
			SlotIndex = slotIndex 
		};		
}
