using Godot;
using System;
using System.Collections.Generic;

public partial class CompassWidget : VBoxContainer
{
	private const string IconDir = "res://UI/Compass/";

	private static readonly Color ColorNone    = new Color(0.35f, 0.35f, 0.35f);
	private static readonly Color ColorOpen    = new Color(0.35f, 0.95f, 0.35f);
	private static readonly Color ColorBlocked = new Color(1.0f,  0.6f,  0.15f);

	private Dictionary<Direction, TextureButton> _icons = new();

	[Signal] public delegate void DirectionPressedEventHandler(int direction);

	public override void _Ready()
	{
		AddThemeConstantOverride("separation", 6);
		Alignment = AlignmentMode.Center;

		// Cardinal rose — 3x3 with the centre blank
		var grid = new GridContainer { Columns = 3 };
		grid.AddThemeConstantOverride("h_separation", 2);
		grid.AddThemeConstantOverride("v_separation", 2);
		AddChild(grid);

		grid.AddChild(Blank());
		grid.AddChild(MakeIcon(Direction.North));
		grid.AddChild(Blank());

		grid.AddChild(MakeIcon(Direction.West));
		grid.AddChild(Blank());
		grid.AddChild(MakeIcon(Direction.East));

		grid.AddChild(Blank());
		grid.AddChild(MakeIcon(Direction.South));
		grid.AddChild(Blank());

		// Up / Down below the rose
		var stairs = new HBoxContainer();
		stairs.Alignment = BoxContainer.AlignmentMode.Center;
		stairs.AddThemeConstantOverride("separation", 8);
		AddChild(stairs);

		stairs.AddChild(MakeIcon(Direction.Up));
		stairs.AddChild(MakeIcon(Direction.Down));
	}

	private Control Blank() => new Control { CustomMinimumSize = new Vector2(40, 40) };

	private TextureButton MakeIcon(Direction dir)
	{
		var btn = new TextureButton
		{
			CustomMinimumSize = new Vector2(40, 40),
			IgnoreTextureSize = true,
			StretchMode       = TextureButton.StretchModeEnum.KeepAspectCentered,
			Modulate          = ColorNone
		};

		string path = $"{IconDir}{dir}.png";
		if (ResourceLoader.Exists(path))
			btn.TextureNormal = GD.Load<Texture2D>(path);

		var captured = dir;
		btn.Pressed += () => EmitSignal(SignalName.DirectionPressed, (int)captured);

		_icons[dir] = btn;
		return btn;
	}

	// Recolour from the current room's exits
	public void Refresh(MapRoom room)
	{
		foreach (var kv in _icons)
		{
			var exit = room?.GetExit(kv.Key);

			Color color;
			if (exit == null || !exit.Discovered)      color = ColorNone;
			else if (exit.State == ExitState.Open)     color = ColorOpen;
			else                                       color = ColorBlocked;

			kv.Value.Modulate = color;
		}
	}
}
