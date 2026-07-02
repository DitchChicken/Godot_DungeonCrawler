using Godot;
using System.Collections.Generic;

public partial class StatusIconRow : HBoxContainer
{
	private const float IconSize = 32f;

	public void Refresh(List<StatusEffect> effects)
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		if (effects == null) return;

		GD.Print($"StatusIconRow.Refresh: {effects.Count} effects");

		AddThemeConstantOverride("separation", 2);

		foreach (var effect in effects)
		{
			GD.Print($"  Effect {effect.Type}, icon: {effect.Icon}, exists: {ResourceLoader.Exists(effect.Icon)}");

			if (string.IsNullOrEmpty(effect.Icon) || !ResourceLoader.Exists(effect.Icon))
				continue;

			// Container for icon + potency label
			var iconControl = new Control();
			iconControl.CustomMinimumSize = new Vector2(IconSize, IconSize);

			var icon = new TextureRect();
			icon.Texture      = GD.Load<Texture2D>(effect.Icon);
			icon.StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered;
			icon.ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize;
			icon.LayoutMode   = 1;
			icon.AnchorRight  = 1.0f;
			icon.AnchorBottom = 1.0f;
			icon.MouseFilter  = MouseFilterEnum.Ignore;
			iconControl.AddChild(icon);
			
			if (effect.Potency > 1)
			{
				var potency = new Label();
				potency.Text = effect.Potency.ToString();
				potency.LayoutMode   = 1;
				potency.AnchorLeft   = 0.0f;
				potency.AnchorTop    = 0.0f;
				potency.AnchorRight  = 1.0f;
				potency.AnchorBottom = 0.4f;  // top portion of the icon area
				potency.HorizontalAlignment = HorizontalAlignment.Center;
				potency.VerticalAlignment   = VerticalAlignment.Top;
				potency.AddThemeColorOverride("font_color", effect.PotencyColor);
				potency.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0));
				potency.AddThemeConstantOverride("shadow_offset_x", 1);
				potency.AddThemeConstantOverride("shadow_offset_y", 1);
				potency.AddThemeFontSizeOverride("font_size", 14);
				potency.MouseFilter = MouseFilterEnum.Ignore;
				iconControl.AddChild(potency);
			}

			AddChild(iconControl);
		}
	}
}
