using Godot;
using System.Linq;

public partial class DomainPanel : VBoxContainer
{
	public void LoadCharacter(Character character)
	{
		foreach (Node c in GetChildren()) c.QueueFree();
		if (character == null) return;

		AddThemeConstantOverride("separation", 4);

		if (character.Domains.Count == 0)
		{
			var none = new Label();
			none.Text = "No domains known.";
			none.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			AddChild(none);
			return;
		}

		foreach (var kv in character.Domains.OrderBy(k => k.Key))
		{
			var row = new HBoxContainer();

			var name = new Label();
			name.Text = kv.Key;
			name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(name);

			var level = new Label();
			level.Text = $"Level {kv.Value}";
			level.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.5f));
			row.AddChild(level);

			AddChild(row);
		}
	}
}
