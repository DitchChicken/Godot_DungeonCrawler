using Godot;
using System.Collections.Generic;

public partial class TurnTracker : Control
{
	private const int SlotCount = 6;

	private TextureRect _background;
	private HBoxContainer _strip;
	private CombatState _combatState;
	private Combat _combat;
	private float _slotSize;

	private List<TurnTrackerSlot> _slots = new List<TurnTrackerSlot>();

	[Signal] public delegate void SlotHoveredEventHandler(int turnOrderIndex);
	[Signal] public delegate void SlotUnhoveredEventHandler();

	public override void _Ready()
	{
		_slotSize = GetViewport().GetVisibleRect().Size.Y * 0.1f;
		BuildLayout();
	}

	private void BuildLayout()
	{
		// Background texture behind everything
		_background = new TextureRect();
		_background.LayoutMode    = 1;
		_background.AnchorRight   = 1.0f;
		_background.AnchorBottom  = 1.0f;
		_background.MouseFilter   = MouseFilterEnum.Ignore;
		_background.StretchMode   = TextureRect.StretchModeEnum.Scale;
		_background.ExpandMode    = TextureRect.ExpandModeEnum.IgnoreSize;

		string bgPath = "res://Data/Menus/PartySlot_Background.png";
		if (ResourceLoader.Exists(bgPath))
			_background.Texture = GD.Load<Texture2D>(bgPath);

		AddChild(_background);

		// Strip holding the 6 slots
		_strip = new HBoxContainer();
		_strip.AddThemeConstantOverride("separation", 4);
		_strip.MouseFilter  = MouseFilterEnum.Pass;
		_strip.LayoutMode   = 1;
		_strip.AnchorRight  = 1.0f;
		_strip.AnchorBottom = 1.0f;
		_strip.Alignment    = BoxContainer.AlignmentMode.Center;
		AddChild(_strip);

		// Create 6 fixed empty slots
		for (int i = 0; i < SlotCount; i++)
		{
			var slot = new TurnTrackerSlot();
			slot.InitializeEmpty(_slotSize);

			int capturedIndex = i;
			slot.Hovered   += () => OnSlotHover(capturedIndex);
			slot.Unhovered += () => EmitSignal(SignalName.SlotUnhovered);

			_strip.AddChild(slot);
			_slots.Add(slot);
		}

		// Fixed size and centered position
		float width  = SlotCount * (_slotSize + 4) + 8;
		float height = _slotSize + 8;
		Size     = new Vector2(width, height);
		Position = new Vector2(
			(GetViewport().GetVisibleRect().Size.X - width) / 2, 0);
	}

	public void Initialize(CombatState combatState, Combat combat)
	{
		_combatState = combatState;
		_combat      = combat;
		Refresh();
	}

	public void Refresh()
	{
		if (_combatState == null) return;

		var order        = _combatState.TurnOrder;
		int currentIndex = _combatState.CurrentTurnIndex;
		int total        = order.Count;

		// Gather living combatants starting from current turn
		var living = new List<(Combatant combatant, int turnIndex)>();
		for (int i = 0; i < total; i++)
		{
			int idx = (currentIndex + i) % total;
			if (order[idx].IsAlive)
				living.Add((order[idx], idx));
		}

		// Fill the 6 slots
		for (int i = 0; i < SlotCount; i++)
		{
			if (i < living.Count)
				_slots[i].SetCombatant(living[i].combatant, living[i].turnIndex, i == 0);
			else
				_slots[i].SetEmpty();
		}
	}

	private void OnSlotHover(int slotIndex)
	{
		// Map slot index to turn order index via the slot's stored data
		if (slotIndex < 0 || slotIndex >= _slots.Count) return;
		int turnIndex = _slots[slotIndex].TurnIndex;
		if (turnIndex < 0) return; // empty slot
		EmitSignal(SignalName.SlotHovered, turnIndex);
	}
}
