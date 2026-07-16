using Godot;
using System.Collections.Generic;

public partial class MoveMenu : PanelContainer
{
	private VBoxContainer _list;

	// Emits the room id to move to, or the special "explore" sentinel
	[Signal] public delegate void RoomChosenEventHandler(string roomId);
	[Signal] public delegate void ExploreChosenEventHandler();
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

	// exploredRooms: list of (roomId, displayName). canExplore: whether "Explore Further" is offered.
	public void Open(List<(string id, string name)> exploredRooms, bool canExplore, string currentRoomId)
	{
		foreach (Node c in _list.GetChildren()) c.QueueFree();

		var title = new Label();
		title.Text = "Where to?";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		_list.AddChild(title);

		// Explore Further first (primary action)
		if (canExplore)
		{
			var exploreBtn = new Button();
			exploreBtn.Text = "Explore Further";
			exploreBtn.Pressed += () => EmitSignal(SignalName.ExploreChosen);
			_list.AddChild(exploreBtn);

			_list.AddChild(new HSeparator());
		}

		// Explored rooms
		foreach (var (id, name) in exploredRooms)
		{
			var btn = new Button();
			// Mark the current room so the player knows where they are
			btn.Text = (id == currentRoomId) ? $"{name}  (here)" : name;
			btn.Disabled = (id == currentRoomId); // can't move to where you already are

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
