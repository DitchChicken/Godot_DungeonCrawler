using Godot;

public partial class InventoryDragData : GodotObject
{
	public enum SourceType { Vault, PersonalInventory }

	public SourceType Source { get; set; }
	public Equipment Item { get; set; }
	public int Count { get; set; } = 1;  // captured at drag start
	public int SlotIndex { get; set; }
	public Character Character { get; set; }
	public bool IsEquipmentSlot { get; set; } = false;
}
