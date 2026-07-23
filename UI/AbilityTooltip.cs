using Godot;

public partial class AbilityTooltip : PanelContainer
{
	private Label _name;
	private Label _type;
	private Label _cost;
	private Label _description;

	public override void _Ready()
	{
		ZIndex      = 100;
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(260, 0);
		Visible = false;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 3);
		margin.AddChild(vbox);

		_name = new Label();
		_name.AddThemeFontSizeOverride("font_size", 18);
		vbox.AddChild(_name);

		_type = new Label();
		_type.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
		vbox.AddChild(_type);

		_cost = new Label();
		_cost.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f));
		vbox.AddChild(_cost);

		_description = new Label();
		_description.AutowrapMode = TextServer.AutowrapMode.Word;
		_description.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.7f));
		vbox.AddChild(_description);
	}

	public void ShowFor(Ability ability, Character owner, Vector2 globalPos)
	{
		if (ability == null) { Visible = false; return; }

		_name.Text = ability.Name;
		_type.Text = ability.Type == AbilityType.Spell ? "Spell" : "Technique";

		var costs = new System.Collections.Generic.List<string>();
		if (ability.ManaCost > 0)   costs.Add($"{ability.ManaCost} MP");
		if (ability.HealthCost > 0) costs.Add($"{ability.HealthCost} HP");
		if (ability.CombatCooldown > 0)      costs.Add($"CD {ability.CombatCooldown} rounds");
		if (ability.ExplorationCooldown > 0) costs.Add($"Explore CD {ability.ExplorationCooldown}");

		float remaining = owner?.GetExplorationCooldownRemaining(ability.Id) ?? 0f;
		if (remaining > 0f) costs.Add($"[ready in {remaining:0.#}]");

		_cost.Text    = string.Join("   ", costs);
		_cost.Visible = costs.Count > 0;

		_description.Text = ability.Description ?? "";

		Position = globalPos + new Vector2(16, 16);
		Visible  = true;
	}

	public void HideTooltip() => Visible = false;
}
