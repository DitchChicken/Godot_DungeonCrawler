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
		var frontRow = GetNode<HBoxContainer>(FrontRowPath);
		var backRow  = GetNode<HBoxContainer>(BackRowPath);

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
	}
}
