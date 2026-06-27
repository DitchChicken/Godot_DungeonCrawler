using Godot;
using System.Collections.Generic;

public partial class InventoryPanel : Control
{
	private Character _character;
	private GridContainer _grid;
	private List<InventorySlotButton> _slots = new List<InventorySlotButton>();
	private const int SlotCount = 12;

	[Signal] public delegate void ItemClickedEventHandler(int slotIndex);
	[Signal] public delegate void ItemDoubleClickedEventHandler(int slotIndex);

	public override void _Ready()
	{
		_grid = new GridContainer();
		_grid.Columns = 4;
		_grid.LayoutMode = 1;
		_grid.AnchorRight = 1.0f;
		_grid.AnchorBottom = 1.0f;
		_grid.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_grid.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_grid.AddThemeConstantOverride("h_separation", 4);
		_grid.AddThemeConstantOverride("v_separation", 4);
		AddChild(_grid);

		BuildSlots();
	}

	private void BuildSlots()
	{
		for (int i = 0; i < SlotCount; i++)
		{
			var btn = new InventorySlotButton();
			btn.SourceType          = InventoryDragData.SourceType.PersonalInventory;
			btn.SlotIndex           = i;
			btn.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
			btn.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
			btn.CustomMinimumSize   = new Vector2(48, 48);
			btn.ExpandIcon          = true;
			btn.IconAlignment       = HorizontalAlignment.Center;
			btn.MouseFilter         = Control.MouseFilterEnum.Stop;

			var capturedIndex = i;
			btn.Pressed    += () => EmitSignal(SignalName.ItemClicked, capturedIndex);
			btn.GuiInput   += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouse
					&& mouse.ButtonIndex == MouseButton.Left
					&& mouse.DoubleClick)
					EmitSignal(SignalName.ItemDoubleClicked, capturedIndex);
			};

			_grid.AddChild(btn);
			_slots.Add(btn);
		}
	}

	public void LoadCharacter(Character character)
	{
		_character = character;
		Refresh();
	}

	public void Refresh()
	{
		if (_character == null) return;
		var items = _character.PersonalInventory.Items;

		for (int i = 0; i < SlotCount; i++)
		{
			var btn  = _slots[i];
			btn.Character = _character;

			if (i < items.Count)
			{
				var item     = items[i];
				btn.Item     = item;
				btn.Modulate = new Color(1, 1, 1, 1);
				btn.Icon     = !string.IsNullOrEmpty(item.Icon)
							   && ResourceLoader.Exists(item.Icon)
							   ? GD.Load<Texture2D>(item.Icon) : null;
			}
			else
			{
				btn.Item     = null;
				btn.Modulate = new Color(1, 1, 1, 0.3f);
				btn.Icon     = null;
				btn.Text     = "";
			}
		}
	}
}
