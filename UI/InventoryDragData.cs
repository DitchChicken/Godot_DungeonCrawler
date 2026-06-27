using Godot;

public partial class InventoryDragData : GodotObject
{
	public enum SourceType { PersonalInventory, Vault, Loot }

	public int SourceInt { get; set; } = 0;  // store as int
	public SourceType Source 
	{ 
		get => (SourceType)SourceInt; 
		set => SourceInt = (int)value; 
	}

	public Equipment Item { get; set; }
	public int Count { get; set; } = 1;
	public int SlotIndex { get; set; }
	public Character Character { get; set; }
	public bool IsEquipmentSlot { get; set; } = false;
}
