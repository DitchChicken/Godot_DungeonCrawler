using Godot;
using System.Collections.Generic;

public partial class MoveMenu : PanelContainer
{
	private VBoxContainer _list;

	// Emits the room id to move to, or the special "explore" sentinel
	[Signal] public delegate void RoomChosenEventHandler(string roomId);
	[Signal] public delegate void CancelledEventHandler();
	
	public const string ExploreSentinel = "__EXPLORE__";

	public override void _Ready()
	{
		LayoutMode  = 1;
		AnchorLeft  = 0.35f; AnchorRight = 0.65f;
		AnchorTop   = 0.15f; AnchorBottom = 0.6f;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		AddChild(margin);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
		margin.AddChild(scroll);

		_list = new VBoxContainer();
		_list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(_list);
	}

	public void Open(List<(string id, string name, bool explored)> rooms, string currentRoomId)
	{
		foreach (Node c in _list.GetChildren()) c.QueueFree();

		var title = new Label();
		title.Text = "Move to (debug)";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		_list.AddChild(title);

		foreach (var (id, name, explored) in rooms)
		{
			var btn = new Button();

			if (id == currentRoomId)      btn.Text = $"{name}  (here)";
			else if (!explored)           btn.Text = $"{name}  (unexplored)";
			else                          btn.Text = name;

			btn.Disabled = (id == currentRoomId);

			string capturedId = id;
			btn.Pressed += () => EmitSignal(SignalName.RoomChosen, capturedId);
			_list.AddChild(btn);
		}

		var cancel = new Button();
		cancel.Text = "Cancel";
		cancel.Pressed += () => EmitSignal(SignalName.Cancelled);
		_list.AddChild(cancel);

		Visible = true;
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
