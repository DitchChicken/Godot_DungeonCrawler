using Godot;
using System;
using System.Collections.Generic;

public partial class EquipmentDoll : Control
{
	private Character _character;
	private EquipmentDollDef _dollDef;
	private TextureRect _baseImage;
	private Dictionary<EquipmentSlot, Button> _slotButtons 
		= new Dictionary<EquipmentSlot, Button>();

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

			var btn = new Button();
			btn.LayoutMode   = 1;
			btn.AnchorLeft   = slotDef.X;
			btn.AnchorTop    = slotDef.Y;
			btn.AnchorRight  = slotDef.X + slotDef.Width;
			btn.AnchorBottom = slotDef.Y + slotDef.Height;

			// Style based on equipped state
			UpdateSlotVisual(btn, slot);

			// Wire clicks
			var capturedSlot = slot;
			btn.Pressed += () => OnSlotPressed(capturedSlot);

			// Double click via timer
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

	private void UpdateSlotVisual(Button btn, EquipmentSlot slot)
	{
		var item = _character?.GetEquipped(slot);

		// Gray out OffHand if two-handed weapon equipped
		bool greyedOut = slot == EquipmentSlot.OffHand
			&& _character?.GetEquipped(EquipmentSlot.WeaponMain)?.IsTwoHanded == true;

		if (greyedOut)
		{
			btn.Text      = "—";
			btn.Modulate  = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			btn.Disabled  = true;
		}
		else if (item != null)
		{
			btn.Text     = item.DisplayName;
			btn.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.9f);
			btn.Disabled = false;
		}
		else
		{
			btn.Text     = "";
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
