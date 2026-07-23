using Godot;

public partial class InventorySlotButton : Button
{
	public int SlotIndex { get; set; }
	public InventoryDragData.SourceType SourceType { get; set; }
	public Character Character { get; set; }
	public bool IsEquipmentSlot { get; set; } = false;
	public bool IsShopSlot { get; set; } = false;
		
	private Label _stackLabel;
	private PopupMenu _contextMenu;
	
	public Equipment Item
	{
		get => _item;
		set
		{
			_item = value;
			RefreshStackLabel();
		}
	}
	private Equipment _item;

	public override void _Ready()
	{
		// Create stack label as child
		_stackLabel = new Label();
		_stackLabel.AnchorLeft             = 0.0f;
		_stackLabel.AnchorTop              = 0.0f;
		_stackLabel.AnchorRight            = 1.0f;
		_stackLabel.AnchorBottom           = 1.0f;
		_stackLabel.OffsetRight            = 0f;
		_stackLabel.OffsetBottom           = 0f;
		_stackLabel.HorizontalAlignment    = HorizontalAlignment.Center;
		_stackLabel.VerticalAlignment      = VerticalAlignment.Center;
		_stackLabel.AddThemeColorOverride("font_color",
			new Color(1.0f, 0.85f, 0.0f, 1.0f));
		_stackLabel.AddThemeFontSizeOverride("font_size", 32);
		_stackLabel.MouseFilter            = MouseFilterEnum.Ignore;
		AddChild(_stackLabel);

		RefreshStackLabel();
	}

	private void RefreshStackLabel()
	{
		if (_stackLabel == null) return;

		if (_item != null && _item.IsStackable && _item.StackCount > 1)
		{
			_stackLabel.Text    = _item.StackCount.ToString();
			_stackLabel.Visible = true;
		}
		else
		{
			_stackLabel.Text    = "";
			_stackLabel.Visible = false;
		}
	}
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		
		//GD.Print($"_GetDragData: {Item?.Name} SourceType:{SourceType} SlotIndex:{SlotIndex}");
		
		// For equipment doll slots, get item from character's equipped items
		if (Item == null && Character != null)
		{
			var equipSlot = (EquipmentSlot)SlotIndex;
			Item = Character.GetEquipped(equipSlot);
		}

		if (Item == null) return new Variant();

		// Capture count NOW before any operations can zero it out
		int capturedCount = Item.StackCount;
		GD.Print($"_GetDragData: {Item.Name} x{capturedCount} fromEquip:{IsEquipmentSlot}");

		var preview = new TextureRect();
		preview.CustomMinimumSize = new Vector2(64, 64);
		preview.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
		preview.StretchMode       = TextureRect.StretchModeEnum.KeepAspect;
		if (Icon != null) 
			preview.Texture = Icon;
		else if (!string.IsNullOrEmpty(Item.Icon) && ResourceLoader.Exists(Item.Icon))
			preview.Texture = GD.Load<Texture2D>(Item.Icon);
		SetDragPreview(preview);

		var data = new InventoryDragData
		{
			Source          = SourceType,
			Item            = Item,
			Count           = capturedCount,
			SlotIndex       = SlotIndex,
			Character       = Character,
			IsEquipmentSlot = IsEquipmentSlot,
			FromShop        = IsShopSlot
		};

		return Variant.From(data);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		//GD.Print($"_CanDropData called");
		
		var dragData = data.As<InventoryDragData>();
		if (dragData == null) return false;

		// Can't drop on itself
		if (dragData.Source == SourceType && dragData.SlotIndex == SlotIndex)
			return false;

		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var dragData = data.As<InventoryDragData>();
		if (dragData == null) return;

		var gameState = GetNode<GameState>("/root/GameState");

		TransferTarget target;
		if (IsEquipmentSlot)
		{
			var equipSlot = (EquipmentSlot)SlotIndex;
			// Validate fit before building target
			if (dragData.Item.Slot != equipSlot)
			{
				GD.Print($"{dragData.Item.Name} doesn't fit {equipSlot} — snapping back");
				return;
			}
			target = TransferTarget.ToEquipment(Character, equipSlot);
		}
		else if (IsShopSlot)
		{
			target = TransferTarget.ToShop(GetNode<GameState>("/root/GameState").CurrentShop);
		}
		else if (SourceType == InventoryDragData.SourceType.Vault)
		{
			target = TransferTarget.ToVault();
		}
		else if (SourceType == InventoryDragData.SourceType.Loot)
		{
			target = TransferTarget.ToLoot();
		}
		else
		{
			// Personal inventory — pass the specific slot for merge detection
			target = TransferTarget.ToInventorySlot(Character, SlotIndex);
		}

		InventoryTransfer.Transfer(dragData, target, gameState);
		RefreshAllUI();
	}

	private void RefreshAllUI()
	{
		var sheet = GetTree().Root.GetNodeOrNull<CharacterSheet>("/root/CharacterSheet");
		sheet?.RefreshDoll();
		sheet?.RefreshInventory();

		var vault = GetTree().Root.GetNodeOrNull<Vault>("Vault");
		vault?.RefreshAll();

		LootContext.Refresh();
	}

	private void RemoveFromSource(InventoryDragData dragData)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			// Source was an equipment slot — unequip
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
			GD.Print($"Unequipped {dragData.Item.Name} from {equipSlot}");
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.RemoveItem(dragData.Item, dragData.Count);
		}
		else
		{
			// Source was personal inventory
			dragData.Character?.PersonalInventory.RemoveItem(dragData.Item, dragData.Count);
		}
	}
	
	private void ReturnToSource(InventoryDragData dragData, Equipment item)
	{
		var gameState = GetNode<GameState>("/root/GameState");
		if (dragData.Source == InventoryDragData.SourceType.Vault)
			gameState.PartyVault.AddItem(item, item.StackCount);
		else
			dragData.Character?.PersonalInventory.AddItem(item, item.StackCount);
	}
	
	private void RemoveCountFromSource(InventoryDragData dragData, int count)
	{
		var gameState = GetNode<GameState>("/root/GameState");

		if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			// Source was an equipment slot — unequip whole thing
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
		}
		else if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			gameState.PartyVault.RemoveItem(dragData.Item, count);
		}
		else
		{
			dragData.Character?.PersonalInventory.RemoveItem(dragData.Item, count);
		}
	}	
	
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse
			&& mouse.Pressed
			&& mouse.ButtonIndex == MouseButton.Right
			&& Item != null)
		{
			ShowContextMenu(mouse.GlobalPosition);
			AcceptEvent();
		}
	}

	private void ShowContextMenu(Vector2 globalPos)
	{
		// Only offer dungeon item use when actually in the dungeon
		var GameState = GetNode<GameState>("/root/GameState");
		if (GameState.CurrentDungeon == "") return;
		if (Item == null) return;

		// Is this item usable in the dungeon?
		if (string.IsNullOrEmpty(Item.UseAbility)) return;
		var ability = AbilityLoader.LoadAbility(Item.UseAbility);
		if (ability == null || !ability.CanUseIn("Dungeon")) return;

		if (_contextMenu == null)
		{
			_contextMenu = new PopupMenu();
			AddChild(_contextMenu);
			_contextMenu.IdPressed += OnContextMenuId;
		}

		_contextMenu.Clear();
		_contextMenu.AddItem("Use Item", 0);
		// Future: AddItem("Drop", 1), etc.

		_contextMenu.Position = (Vector2I)globalPos;
		_contextMenu.Popup();
	}

	private void OnContextMenuId(long id)
	{
		if (id == 0) // Use Item
		{
			DungeonItemUse.BeginItemUse(this.Item, this.Character, this.SourceType, this.SlotIndex);
		}
	}
}
