using Godot;
using System;
using System.Collections.Generic;

public partial class DungeonTargetController : Control
{
	private Label _promptLabel;
	private GameState _gameState;
	private PartyHUD _hud;

	private Ability _ability;
	private Action<Character> _onChosen;
	private List<Character> _validTargets = new List<Character>();
	private bool _selecting = false;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_hud       = GetNode<PartyHUD>("/root/PartyHud");

		// Center prompt overlay
		_promptLabel = new Label();
		_promptLabel.LayoutMode = 1;
		_promptLabel.AnchorLeft = 0.3f; _promptLabel.AnchorRight = 0.7f;
		_promptLabel.AnchorTop  = 0.4f; _promptLabel.AnchorBottom = 0.5f;
		_promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_promptLabel.VerticalAlignment   = VerticalAlignment.Center;
		_promptLabel.AddThemeFontSizeOverride("font_size", 28);
		_promptLabel.AddThemeColorOverride("font_shadow_color", new Color(0,0,0));
		_promptLabel.Visible = false;
		_promptLabel.ZIndex  = 50;
		AddChild(_promptLabel);

		DungeonItemUse.TargetController = this;
	}

	public void BeginPartyTargetSelect(Ability ability, string itemName, Action<Character> onChosen)
	{
		_ability  = ability;
		_onChosen = onChosen;

		// Determine valid targets by ability type
		_validTargets = GetValidTargets(ability);
		if (_validTargets.Count == 0)
		{
			GD.Print("No valid targets");
			return;
		}

		_selecting = true;
		_promptLabel.Text    = $"Select target for {itemName}";
		_promptLabel.Visible = true;

		// Highlight valid portraits, enter HUD target mode
		_hud.BeginTargetSelect(_validTargets, OnPortraitClicked);
	}

	private List<Character> GetValidTargets(Ability ability)
	{
		var list = new List<Character>();
		foreach (var c in _gameState.Party)
		{
			if (!c.IsAlive) continue; // heal only on living, per design

			// Could refine per effect type (e.g. don't offer heal to full-HP later)
			list.Add(c);
		}
		return list;
	}

	private void OnPortraitClicked(Character target)
	{
		if (!_selecting) return;
		if (!_validTargets.Contains(target)) return;

		EndSelect();
		_onChosen?.Invoke(target);
	}

	private void EndSelect()
	{
		_selecting = false;
		_promptLabel.Visible = false;
		_hud.EndTargetSelect();
	}

	public override void _Input(InputEvent @event)
	{
		if (!_selecting) return;

		// Right-click or escape cancels
		if ((@event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Right)
			|| (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape))
		{
			EndSelect();
			DungeonItemUse.Cancel();
			GetViewport().SetInputAsHandled();
		}
	}
}
