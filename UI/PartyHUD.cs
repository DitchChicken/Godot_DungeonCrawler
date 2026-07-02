using Godot;
using System.Collections.Generic;

public partial class PartyHUD : CanvasLayer
{
	[Export] public NodePath FrontRowPath;
	[Export] public NodePath BackRowPath;

	private List<PartySlot> _slots = new List<PartySlot>();
	private PackedScene _slotScene = GD.Load<PackedScene>("res://UI/PartySlot.tscn");
	
	
	public override void _Ready()
	{
		var frontRow = GetNode<HBoxContainer>("Panel/VBoxContainer/FrontRow");
		var backRow  = GetNode<HBoxContainer>("Panel/VBoxContainer/BackRow");

		// Create 3 slots per row
		for (int i = 0; i < 3; i++)
		{
			var slot = _slotScene.Instantiate<PartySlot>();
			slot.IsHudSlot = true;
			frontRow.AddChild(slot);
			_slots.Add(slot);
		}

		for (int i = 0; i < 3; i++)
		{
			var slot = _slotScene.Instantiate<PartySlot>();
			slot.IsHudSlot = true;
			backRow.AddChild(slot);
			_slots.Add(slot);
		}

		Refresh();
	}

	public void DebugLayout()
	{
		var panel = GetNode<Control>("Panel");
		GD.Print($"HUD Panel rect: {panel.GetRect()}");
		GD.Print($"HUD Panel global pos: {panel.GlobalPosition}");
		GD.Print($"HUD Panel size: {panel.Size}");
		
		var frontRow = GetNode<HBoxContainer>("Panel/VBoxContainer/FrontRow");
		GD.Print($"FrontRow rect: {frontRow.GetRect()}");
		GD.Print($"FrontRow global pos: {frontRow.GlobalPosition}");
		
		// Check each slot size
		foreach (var child in frontRow.GetChildren())
		{
			if (child is Control c)
				GD.Print($"Slot size: {c.Size} global pos: {c.GlobalPosition}");
		}
	}
	
	public List<PartySlot> GetAllSlots()
	{
		return _slots;
	}
	
	public void Refresh()
	{
		var party = GetNode<GameState>("/root/GameState").Party;

		for (int i = 0; i < _slots.Count; i++)
		{
			if (i < party.Count)
				_slots[i].SetCharacter(party[i]);
			else
				_slots[i].Clear();
		}
		foreach (var slot in _slots)
			slot.RefreshStatus();
	}
	
	public void HighlightSlot(Character character)
	{
		foreach (var slot in _slots)
		{
			if (slot.Character == character)
				slot.Modulate = new Color(0.6f, 1.0f, 0.6f, 1.0f); // green
			else
				slot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f); // normal
		}
	}

	public void ClearHighlights()
	{
		foreach (var slot in _slots)
			slot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	}	
}
