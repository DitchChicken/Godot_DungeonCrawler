using Godot;

public partial class RoomLootPanel : PanelContainer
{
	private const int SlotCount = 32;
	private GridContainer _grid;
	private Label _header;
	private Inventory _loot;

	[Signal] public delegate void ClosedEventHandler();

	public override void _Ready()
	{
		LayoutMode  = 1;
		AnchorLeft  = 0.02f; AnchorRight  = 0.48f;
		AnchorTop   = 0.10f; AnchorBottom = 0.62f;
		ZIndex      = 20;
		Visible     = false;

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		AddChild(margin);

		var vbox = new VBoxContainer();
		margin.AddChild(vbox);

		_header = new Label();
		_header.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_header);
		vbox.AddChild(new HSeparator());

		_grid = new GridContainer();
		_grid.Columns = 8;
		_grid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		vbox.AddChild(_grid);

		var close = new Button();
		close.Text = "Close";
		close.Pressed += Close;
		vbox.AddChild(close);
	}

	public void Open(Inventory loot, string roomName)
	{
		_loot = loot;
		_header.Text = $"On the floor — {roomName}";

		LootContext.Set(_loot, Refresh);
		Refresh();
		Visible = true;
	}

	public void Close()
	{
		LootContext.Clear();
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	public void Refresh()
	{
		foreach (Node c in _grid.GetChildren()) c.QueueFree();
		if (_loot == null) return;

		for (int i = 0; i < SlotCount; i++)
		{
			var slot = new InventorySlotButton();
			slot.SourceType        = InventoryDragData.SourceType.Loot;
			slot.SlotIndex         = i;
			slot.CustomMinimumSize = new Vector2(48, 48);
			slot.CustomMaximumSize = new Vector2(96, 96);
			slot.ExpandIcon        = true;
			slot.IconAlignment     = HorizontalAlignment.Center;
			slot.MouseFilter       = Control.MouseFilterEnum.Stop;

			if (i < _loot.Items.Count)
			{
				var item = _loot.Items[i];
				slot.Item = item;
				if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
					slot.Icon = GD.Load<Texture2D>(item.Icon);
			}

			_grid.AddChild(slot);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape)
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
