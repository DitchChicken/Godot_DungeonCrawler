using Godot;
using System;
using System.Collections.Generic;

public partial class EquipmentDoll : Control
{
	private Character _character;
	private EquipmentDollDef _dollDef;
	private TextureRect _baseImage;
	private Dictionary<EquipmentSlot, InventorySlotButton> _slotButtons 
	= new Dictionary<EquipmentSlot, InventorySlotButton>();

	// Signals
	[Signal] public delegate void SlotClickedEventHandler(int slot);
	[Signal] public delegate void SlotDoubleClickedEventHandler(int slot);

	public override void _Ready()
	{
		_baseImage = new TextureRect();
		_baseImage.StretchMode  = TextureRect.StretchModeEnum.KeepAspect;
		_baseImage.ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize;
		_baseImage.AnchorRight  = 1.0f;
		_baseImage.AnchorBottom = 1.0f;
		_baseImage.LayoutMode   = 1;
		AddChild(_baseImage);
	}

	public void LoadCharacter(Character character)
	{
		_character = character;
		_dollDef   = EquipmentDollLoader.LoadDoll(character.Race);
		if (_dollDef == null) return;

		// Load base image
		if (ResourceLoader.Exists(_dollDef.BaseImage))
			_baseImage.Texture = GD.Load<Texture2D>(_dollDef.BaseImage);

		// Clear old slot buttons
		foreach (var btn in _slotButtons.Values)
			btn.QueueFree();
		_slotButtons.Clear();

		// Create slot buttons
		CallDeferred(nameof(BuildSlots));
	}

	private void BuildSlots()
	{
		if (_dollDef == null) return;

		foreach (var slotDef in _dollDef.Slots)
		{
			if (!Enum.TryParse<EquipmentSlot>(slotDef.Slot, out var slot)) continue;

			var btn = new InventorySlotButton();
			btn.LayoutMode   = 1;
			btn.AnchorLeft   = slotDef.X;
			btn.AnchorTop    = slotDef.Y;
			btn.AnchorRight  = slotDef.X + slotDef.Width;
			btn.AnchorBottom = slotDef.Y + slotDef.Height;
			btn.MouseFilter  = Control.MouseFilterEnum.Stop;
			btn.ExpandIcon   = true;
			btn.IconAlignment = HorizontalAlignment.Center;

			// Set drag data source
			btn.SourceType  = InventoryDragData.SourceType.PersonalInventory;
			btn.SlotIndex   = (int)slot;
			btn.Character   = _character;
			btn.IsEquipmentSlot = true;
			
			// Update visual
			UpdateSlotVisual(btn, slot);

			// Wire clicks
			var capturedSlot = slot;
			btn.Pressed += () => OnSlotPressed(capturedSlot);
			btn.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouse
					&& mouse.ButtonIndex == MouseButton.Left
					&& mouse.DoubleClick)
					OnSlotDoubleClicked(capturedSlot);
			};

			AddChild(btn);
			_slotButtons[slot] = btn;
		}
	}

	private void UpdateSlotVisual(InventorySlotButton btn, EquipmentSlot slot)
	{
		var item = _character?.GetEquipped(slot);

		bool greyedOut = slot == EquipmentSlot.OffHand
			&& _character?.GetEquipped(EquipmentSlot.WeaponMain)?.IsTwoHanded == true;

		if (greyedOut)
		{
			btn.Text          = "—";
			btn.Icon          = null;
			btn.Modulate      = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			btn.Disabled      = true;
		}
		else if (item != null)
		{
			btn.Text     = "";
			btn.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.9f);
			btn.Disabled = false;

			// Load icon if available
			if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
			{
				btn.Icon              = GD.Load<Texture2D>(item.Icon);
				btn.ExpandIcon        = true;
				btn.IconAlignment     = HorizontalAlignment.Center;
			}
			else
			{
				btn.Icon = null;
				btn.Text = item.DisplayName; // fallback to text if no icon
			}
		}
		else
		{
			btn.Text     = "";
			btn.Icon     = null;
			btn.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.3f);
			btn.Disabled = false;
		}
	}

	public void Refresh()
	{
		foreach (var kvp in _slotButtons)
			UpdateSlotVisual(kvp.Value, kvp.Key);
	}

	private void OnSlotPressed(EquipmentSlot slot)
	{
		EmitSignal(SignalName.SlotClicked, (int)slot);
	}

	private void OnSlotDoubleClicked(EquipmentSlot slot)
	{
		EmitSignal(SignalName.SlotDoubleClicked, (int)slot);
	}
}
