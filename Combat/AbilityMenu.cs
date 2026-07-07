using Godot;
using System;
using System.Collections.Generic;

public partial class AbilityMenu : PanelContainer
{
	private VBoxContainer _list;
	private Character _caster;

	// Fired when the player picks an ability; null-ish handled by caller
	[Signal] public delegate void AbilitySelectedEventHandler(string abilityId);
	[Signal] public delegate void CancelledEventHandler();

	public override void _Ready()
	{
		// Center panel
		LayoutMode  = 1;
		AnchorLeft  = 0.35f;
		AnchorRight = 0.65f;
		AnchorTop   = 0.25f;
		AnchorBottom = 0.6f;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		AddChild(margin);

		_list = new VBoxContainer();
		_list.AddThemeConstantOverride("separation", 6);
		margin.AddChild(_list);
	}

	public void Open(Character caster)
	{
		_caster = caster;
		MouseFilter = MouseFilterEnum.Stop;
		BuildList();
		Visible = true;
	}

	private void BuildList()
	{
		foreach (Node child in _list.GetChildren())
			child.QueueFree();

		var title = new Label();
		title.Text = "Spells & Skills";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		_list.AddChild(title);

		var abilities = AbilityLoader.LoadAbilities(_caster.KnownAbilities);
		if (abilities.Count == 0)
		{
			var none = new Label();
			none.Text = "No abilities known.";
			none.HorizontalAlignment = HorizontalAlignment.Center;
			_list.AddChild(none);
		}

		foreach (var ability in abilities)
		{
			var btn = new Button();
			bool usable = _caster.CanUseAbility(ability);

			string label = $"{ability.Name}  ({ability.ManaCost} MP)";
			// Show cooldown if active
			if (_caster.AbilityCooldowns.TryGetValue(ability.Id, out int cd) && cd > 0)
				label += $"  [CD {cd}]";

			btn.Text     = label;
			btn.Disabled = !usable;

			string capturedId = ability.Id;
			btn.Pressed += () => EmitSignal(SignalName.AbilitySelected, capturedId);

			_list.AddChild(btn);
		}

		// Cancel button
		var cancel = new Button();
		cancel.Text = "Cancel";
		cancel.Pressed += () => EmitSignal(SignalName.Cancelled);
		_list.AddChild(cancel);
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;

		if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			EmitSignal(SignalName.Cancelled);
			GetViewport().SetInputAsHandled();
		}
	}
}
