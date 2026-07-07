using Godot;
using System.Collections.Generic;

public partial class ItemMenu : PanelContainer
{
	private VBoxContainer _list;
	private Character _user;

	[Signal] public delegate void ItemSelectedEventHandler(string itemId);
	[Signal] public delegate void CancelledEventHandler();

	public override void _Ready()
	{
		LayoutMode = 1;
		AnchorLeft = 0.35f; AnchorRight = 0.65f;
		AnchorTop  = 0.25f; AnchorBottom = 0.6f;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		AddChild(margin);

		_list = new VBoxContainer();
		_list.AddThemeConstantOverride("separation", 6);
		margin.AddChild(_list);
	}

	public void Open(Character user)
	{
		_user = user;
		BuildList();
		Visible = true;
	}

	private void BuildList()
	{
		foreach (Node child in _list.GetChildren())
			child.QueueFree();

		var title = new Label();
		title.Text = "Items";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		_list.AddChild(title);

		// Gather combat-usable consumables from the user's inventory
		var usable = new List<Equipment>();
		foreach (var item in _user.PersonalInventory.Items)
			if (item.IsCombatConsumable)
				usable.Add(item);

		if (usable.Count == 0)
		{
			var none = new Label();
			none.Text = "No usable items.";
			none.HorizontalAlignment = HorizontalAlignment.Center;
			_list.AddChild(none);
		}

		foreach (var item in usable)
		{
			var btn = new Button();
			btn.Text = $"{item.Name}  x{item.StackCount}";
			string capturedId = item.Id;
			btn.Pressed += () => EmitSignal(SignalName.ItemSelected, capturedId);
			_list.AddChild(btn);
		}

		var cancel = new Button();
		cancel.Text = "Cancel";
		cancel.Pressed += () => EmitSignal(SignalName.Cancelled);
		_list.AddChild(cancel);
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
