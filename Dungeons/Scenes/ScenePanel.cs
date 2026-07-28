using Godot;
using System.Collections.Generic;

public partial class ScenePanel : Control
{
	public class OptionDisplay
	{
		public string Label;
		public bool Available;
		public SceneOption Option;
	}

	private TextureRect _image;
	private RichTextLabel _text;
	private VBoxContainer _options;
	private SceneController _controller;
	private EncounterInstance _encounter;

	[Signal] public delegate void SceneClosedEventHandler();
	[Signal] public delegate void CombatStartedEventHandler();
	
	public override void _Ready()
	{
		LayoutMode  = 1;
		AnchorLeft  = 0.12f; AnchorRight  = 0.88f;
		AnchorTop   = 0.05f; AnchorBottom = 0.63f;
		ZIndex      = 40;
		Visible     = false;

		var bg = new ColorRect();
		bg.Color = new Color(0.05f, 0.05f, 0.07f, 0.96f);
		bg.LayoutMode = 1; bg.AnchorRight = 1; bg.AnchorBottom = 1;
		bg.MouseFilter = MouseFilterEnum.Stop;
		AddChild(bg);

		var margin = new MarginContainer();
		margin.LayoutMode = 1; margin.AnchorRight = 1; margin.AnchorBottom = 1;
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		AddChild(margin);

		// Horizontal split: image left, text+options right
		var split = new HBoxContainer();
		split.AddThemeConstantOverride("separation", 16);
		margin.AddChild(split);

		// LEFT — image, square, fills its half
		_image = new TextureRect();
		_image.StretchMode        = TextureRect.StretchModeEnum.KeepAspectCentered;
		_image.ExpandMode         = TextureRect.ExpandModeEnum.IgnoreSize;
		_image.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_image.SizeFlagsVertical   = SizeFlags.ExpandFill;
		_image.SizeFlagsStretchRatio = 1.0f;
		split.AddChild(_image);

		// RIGHT — vertical: text (top, expanding) + options (bottom)
		var right = new VBoxContainer();
		right.SizeFlagsHorizontal   = SizeFlags.ExpandFill;
		right.SizeFlagsStretchRatio  = 1.0f;
		right.AddThemeConstantOverride("separation", 10);
		split.AddChild(right);

		var textScroll = new ScrollContainer();
		textScroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
		textScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		right.AddChild(textScroll);

		_text = new RichTextLabel();
		_text.BbcodeEnabled = true;
		_text.FitContent    = true;
		_text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		textScroll.AddChild(_text);

		_options = new VBoxContainer();
		_options.SizeFlagsVertical = SizeFlags.ShrinkEnd;   // pinned to bottom
		_options.AddThemeConstantOverride("separation", 4);
		right.AddChild(_options);
	}
	public void Open(Scene scene, EncounterInstance encounter, GameState gs)
	{
		_encounter  = encounter;
		_controller = new SceneController(scene, encounter, gs, this);
		_text.Text  = "";
		Visible     = true;
		_controller.Start();
	}

	public void ShowNode(SceneNode node, List<OptionDisplay> options)
	{
		string img = !string.IsNullOrEmpty(node.Image) ? node.Image : _encounter?.OverlayImage;
		if (!string.IsNullOrEmpty(img) && ResourceLoader.Exists(img))
		{
			_image.Texture = GD.Load<Texture2D>(img);
			_image.Visible = true;
		}
		else _image.Visible = false;


		// Node text — appended so prior exchanges scroll back
		AppendText(node.GetText());

		// Rebuild options
		foreach (Node c in _options.GetChildren()) c.QueueFree();
		foreach (var od in options)
		{
			var btn = new Button();
			btn.Text     = od.Label;
			btn.Disabled = !od.Available;
			var captured = od.Option;
			btn.Pressed += () => _controller.ChooseOption(captured);
			_options.AddChild(btn);
		}
	}

	public void AppendText(string line)
	{
		if (string.IsNullOrWhiteSpace(line)) return;
		if (_text.Text.Length > 0) _text.Text += "\n\n";
		_text.Text += line;
	}

	public void CloseScene()
	{
		Visible = false;
		if (_controller != null && _controller.CombatRequested)
			EmitSignal(SignalName.CombatStarted);
		else
			EmitSignal(SignalName.SceneClosed);
	}

	public override void _Input(InputEvent @event)
	{
		// No escape-to-close — scenes resolve through their options
	}
}
