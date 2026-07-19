using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DebugConsole : CanvasLayer
{
	private Control _root;
	private RichTextLabel _output;
	private LineEdit _input;
	private bool _isOpen = false;

	// Command registry: name → (handler, help text)
	private Dictionary<string, (Func<string[], string> handler, string help)> _commands
		= new Dictionary<string, (Func<string[], string>, string)>(StringComparer.OrdinalIgnoreCase);

	// Input history
	private List<string> _history = new List<string>();
	private int _historyIndex = -1;

	public static DebugConsole Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		Layer    = 128;              // above everything
		ProcessMode = ProcessModeEnum.Always; // works even if game is paused

		BuildUI();
		RegisterBuiltInCommands();		
		Hide();
		CallDeferred(nameof(RegisterGameCommands));
	}

	private void BuildUI()
	{
		_root = new Control();
		_root.LayoutMode  = 1;
		_root.AnchorRight = 1.0f;
		_root.AnchorBottom = 0.45f;   // drops down over top 45% of screen
		_root.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(_root);

		// Dark backdrop
		var bg = new ColorRect();
		bg.Color        = new Color(0, 0, 0, 0.88f);
		bg.LayoutMode   = 1;
		bg.AnchorRight  = 1.0f;
		bg.AnchorBottom = 1.0f;
		bg.MouseFilter  = Control.MouseFilterEnum.Ignore;
		_root.AddChild(bg);

		var margin = new MarginContainer();
		margin.LayoutMode   = 1;
		margin.AnchorRight  = 1.0f;
		margin.AnchorBottom = 1.0f;
		margin.AddThemeConstantOverride("margin_left", 8);
		margin.AddThemeConstantOverride("margin_right", 8);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		_root.AddChild(margin);

		var vbox = new VBoxContainer();
		margin.AddChild(vbox);

		_output = new RichTextLabel();
		_output.SizeFlagsVertical   = Control.SizeFlags.ExpandFill;
		_output.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_output.BbcodeEnabled       = true;
		_output.ScrollFollowing     = true;
		_output.AddThemeFontSizeOverride("normal_font_size", 14);
		vbox.AddChild(_output);

		_input = new LineEdit();
		_input.PlaceholderText      = "enter command  (help for list)";
		_input.SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill;
		_input.TextSubmitted       += OnInputSubmitted;
		vbox.AddChild(_input);

		Print("[color=gray]Debug console ready. Type 'help' for commands.[/color]");
	}

	// --- Open / close ---

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			// Tilde / backquote toggles
			if (key.Keycode == Key.Quoteleft)
			{
				Toggle();
				GetViewport().SetInputAsHandled();
				return;
			}

			if (_isOpen)
			{
				if (key.Keycode == Key.Escape)
				{
					Close();
					GetViewport().SetInputAsHandled();
					return;
				}
				// History navigation
				if (key.Keycode == Key.Up)
				{
					NavigateHistory(-1);
					GetViewport().SetInputAsHandled();
				}
				else if (key.Keycode == Key.Down)
				{
					NavigateHistory(1);
					GetViewport().SetInputAsHandled();
				}
			}
		}
	}

	public void Toggle() { if (_isOpen) Close(); else Open(); }

	public void Open()
	{
		_isOpen      = true;
		_root.Visible = true;
		_input.Clear();
		_input.GrabFocus();
	}

	public void Close()
	{
		_isOpen       = false;
		_root.Visible = false;
		_input.ReleaseFocus();
	}

	private new void Hide() { _isOpen = false; _root.Visible = false; }

	private void NavigateHistory(int dir)
	{
		if (_history.Count == 0) return;

		if (_historyIndex == -1 && dir < 0) _historyIndex = _history.Count - 1;
		else _historyIndex = Mathf.Clamp(_historyIndex + dir, 0, _history.Count - 1);

		_input.Text = _history[_historyIndex];
		_input.CaretColumn = _input.Text.Length;
	}

	// --- Command execution ---

	private void OnInputSubmitted(string text)
	{
		_input.Clear();
		if (string.IsNullOrWhiteSpace(text)) return;

		_history.Add(text);
		_historyIndex = -1;

		Print($"[color=#88ccff]> {text}[/color]");
		Execute(text);

		_input.GrabFocus();
	}

	public void Execute(string commandLine)
	{
		var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return;

		string cmd  = parts[0];
		string[] args = parts.Skip(1).ToArray();

		if (!_commands.TryGetValue(cmd, out var entry))
		{
			Print($"[color=#ff8888]Unknown command: {cmd}[/color]");
			return;
		}

		try
		{
			string result = entry.handler(args);
			if (!string.IsNullOrEmpty(result))
				Print(result);
		}
		catch (Exception ex)
		{
			Print($"[color=#ff8888]Error: {ex.Message}[/color]");
		}
	}

	// --- Registration ---

	public void Register(string name, Func<string[], string> handler, string help = "")
	{
		_commands[name] = (handler, help);
	}

	public void Print(string message)
	{
		_output?.AppendText(message + "\n");
	}

	// --- Built-in commands ---

	private void RegisterBuiltInCommands()
	{
		Register("help", args =>
		{
			if (args.Length > 0 && _commands.TryGetValue(args[0], out var e))
				return $"{args[0]}: {e.help}";

			var lines = _commands
				.OrderBy(kv => kv.Key)
				.Select(kv => $"  [color=#ffdd88]{kv.Key}[/color] — {kv.Value.help}");
			return "Commands:\n" + string.Join("\n", lines);
		}, "List commands, or 'help <command>' for detail");

		Register("clear", args =>
		{
			_output.Clear();
			return "";
		}, "Clear the console output");

		Register("quit", args =>
		{
			GetTree().Quit();
			return "";
		}, "Exit the game");
	}
	
	private void RegisterGameCommands()
	{
		var gameState = GetNodeOrNull<GameState>("/root/GameState");
		if (gameState != null)
			DebugCommands.RegisterAll(gameState);
		else
			GD.PrintErr("DebugConsole: GameState not found for command registration");
	}
}
