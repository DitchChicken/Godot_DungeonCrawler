using Godot;
using System.Collections.Generic;

public partial class AbilityIconGrid : GridContainer
{
	private AbilityTooltip _tooltip;
	private AbilityType _filter;

	public void Setup(AbilityType filter, AbilityTooltip tooltip)
	{
		_filter  = filter;
		_tooltip = tooltip;
		Columns  = 5;
		AddThemeConstantOverride("h_separation", 6);
		AddThemeConstantOverride("v_separation", 6);
	}

	public void LoadCharacter(Character character)
	{
		foreach (Node c in GetChildren()) c.QueueFree();
		if (character == null) return;

		var abilities = AbilityLoader.LoadAbilities(character.KnownAbilities);

		foreach (var ability in abilities)
		{
			if (ability.Type != _filter) continue;

			var btn = new TextureButton();
			btn.CustomMinimumSize = new Vector2(48, 48);
			btn.IgnoreTextureSize = true;
			btn.StretchMode       = TextureButton.StretchModeEnum.KeepAspectCentered;

			if (!string.IsNullOrEmpty(ability.Icon) && ResourceLoader.Exists(ability.Icon))
				btn.TextureNormal = GD.Load<Texture2D>(ability.Icon);

			var capturedAbility = ability;
			var capturedOwner   = character;
			btn.MouseEntered += () =>
				_tooltip?.ShowFor(capturedAbility, capturedOwner, btn.GlobalPosition);
			btn.MouseExited  += () => _tooltip?.HideTooltip();

			AddChild(btn);
		}

		// Placeholder when nothing known
		if (GetChildCount() == 0)
		{
			var none = new Label();
			none.Text = _filter == AbilityType.Spell ? "No spells known." : "No techniques known.";
			none.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			AddChild(none);
		}
	}
}
