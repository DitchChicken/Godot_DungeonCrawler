using Godot;
using System.Collections.Generic;

public partial class Vault : Control
{
	private GridContainer _vaultGrid;
	private Label _weightLabel;
	private GameState _gameState;
	private List<InventorySlotButton> _vaultSlots = new List<InventorySlotButton>();

	public override void _Ready()
	{
		_gameState   = GetNode<GameState>("/root/GameState");
		_vaultGrid   = GetNode<GridContainer>("VaultPanel/VaultGrid");
		_weightLabel = GetNode<Label>("VaultPanel/WeightLabel");

		BuildVaultSlots();
		RefreshVault();

		// Open character sheet for first party member
		CallDeferred(nameof(OpenFirstCharacter));
	}

	private void BuildVaultSlots()
	{
		for (int i = 0; i < 64; i++)
		{
			var btn = new InventorySlotButton();
			btn.SourceType          = InventoryDragData.SourceType.Vault;
			btn.SlotIndex           = i;
			btn.CustomMinimumSize   = new Vector2(64, 64);
			btn.ExpandIcon          = true;
			btn.IconAlignment       = HorizontalAlignment.Center;
			btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			btn.MouseFilter         = Control.MouseFilterEnum.Stop;

			var capturedIndex = i;
			btn.Pressed  += () => OnVaultSlotPressed(capturedIndex);
			btn.GuiInput += (e) =>
			{
				if (e is InputEventMouseButton m
					&& m.ButtonIndex == MouseButton.Left
					&& m.DoubleClick)
					OnVaultSlotDoubleClicked(capturedIndex);
			};

			_vaultGrid.AddChild(btn);
			_vaultSlots.Add(btn);
		}
	}
	private Button CreateSlotButton()
	{
		var btn = new Button();
		btn.CustomMinimumSize   = new Vector2(64, 64);
		btn.ExpandIcon          = true;
		btn.IconAlignment       = HorizontalAlignment.Center;
		btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		return btn;
	}

	private void RefreshVault()
	{
		var items = _gameState.PartyVault.Items;

		for (int i = 0; i < _vaultSlots.Count; i++)
		{
			var btn = _vaultSlots[i];
			if (i < items.Count)
			{
				var item     = items[i];
				btn.Item     = item;
				btn.Modulate = new Color(1, 1, 1, 1);
				btn.Icon     = !string.IsNullOrEmpty(item.Icon)
							   && ResourceLoader.Exists(item.Icon)
							   ? GD.Load<Texture2D>(item.Icon) : null;
				btn.Text     = item.StackCount > 1 ? $"x{item.StackCount}" : "";
			}
			else
			{
				btn.Item     = null;
				btn.Icon     = null;
				btn.Text     = "";
				btn.Modulate = new Color(1, 1, 1, 0.3f);
			}
		}

		_weightLabel.Text = $"Items: {_gameState.PartyVault.UsedSlots}/{_gameState.PartyVault.MaxSlots}";
	}

	private void OnVaultSlotPressed(int index)
	{
		GD.Print($"Vault slot {index} clicked");
		// TODO: item details tooltip
	}

	// Double click vault item → transfer to selected character
	private void OnVaultSlotDoubleClicked(int index)
	{
		var items = _gameState.PartyVault.Items;
		if (index >= items.Count) return;

		// Get currently displayed character from CharacterSheet
		var sheet = GetNode<CharacterSheet>("/root/CharacterSheet");
		var character = sheet.CurrentCharacter;

		if (character == null)
		{
			GD.Print("No character selected — click a portrait in the HUD first");
			return;
		}

		var item      = items[index];
		int remainder = character.PersonalInventory.AddItem(item);

		if (remainder < item.StackCount)
		{
			_gameState.PartyVault.RemoveItem(item, item.StackCount - remainder);
			GD.Print($"Moved {item.Name} to {character.Name}");
			RefreshVault();
			sheet.RefreshInventory();
		}
		else
		{
			GD.Print($"{character.Name} can't carry {item.Name}");
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key
			&& key.Pressed
			&& key.Keycode == Key.Escape)
		{
			GetNode<CharacterSheet>("/root/CharacterSheet").Hide();
			GetNode<Main>("/root/Main")
				.CallDeferred(nameof(Main.SwitchScene), "res://Town/Town.tscn");
			GetViewport().SetInputAsHandled();
		}
	}

	private void _on_back_button_pressed()
	{
		GetNode<CharacterSheet>("/root/CharacterSheet").Hide();
		GetNode<Main>("/root/Main")
			.CallDeferred(nameof(Main.SwitchScene), "res://Town/Town.tscn");
	}
	
	private void OpenFirstCharacter()
	{
		// Find first party member regardless of slot position
		Character first = null;
		foreach (var character in _gameState.Party)
		{
			if (character != null)
			{
				first = character;
				break;
			}
		}

		if (first != null)
		{
			GetNode<CharacterSheet>("/root/CharacterSheet")
				.Open(first, CharacterSheetMode.Right);
			//GD.Print($"Vault opened with {first.Name}");
		}
		else
		{
			GD.Print("No party members to display");
		}
	}
	
	public void HandleDrop(
		InventoryDragData dragData,
		InventoryDragData.SourceType targetSource,
		int targetSlot,
		Character targetCharacter)
	{
		var item  = dragData.Item;
		int count = dragData.Count;

		GD.Print($"HandleDrop: {item.Name} x{count}");

		// Remove from source
		if (dragData.Source == InventoryDragData.SourceType.Vault)
		{
			_gameState.PartyVault.RemoveItem(item, count);
		}
		else if (dragData.IsEquipmentSlot && dragData.Character != null)
		{
			var equipSlot = (EquipmentSlot)dragData.SlotIndex;
			dragData.Character.Unequip(equipSlot);
		}
		else
		{
			dragData.Character?.PersonalInventory.RemoveItem(item, count);
		}

		// Use captured count everywhere below instead of item.StackCount
		if (targetSource == InventoryDragData.SourceType.Vault)
		{
			var vaultItems = _gameState.PartyVault.Items;
			Equipment swappedItem = null;
			if (targetSlot < vaultItems.Count)
			{
				swappedItem = vaultItems[targetSlot];
				_gameState.PartyVault.RemoveItem(swappedItem, swappedItem.StackCount);
			}
			_gameState.PartyVault.AddItem(item, count);  // use count not item.StackCount

			if (swappedItem != null)
			{
				if (dragData.IsEquipmentSlot && dragData.Character != null)
				{
					if (!dragData.Character.Equip(swappedItem))
						dragData.Character.PersonalInventory.AddItem(swappedItem, swappedItem.StackCount);
				}
				else if (dragData.Source == InventoryDragData.SourceType.Vault)
					_gameState.PartyVault.AddItem(swappedItem, swappedItem.StackCount);
				else
					dragData.Character?.PersonalInventory.AddItem(swappedItem, swappedItem.StackCount);
			}
		}
		else
		{
			var inv = targetCharacter?.PersonalInventory;
			if (inv != null)
			{
				Equipment swappedItem = null;
				var invItems = inv.Items;
				if (targetSlot < invItems.Count)
				{
					swappedItem = invItems[targetSlot];
					inv.RemoveItem(swappedItem, swappedItem.StackCount);
				}

				int remainder = inv.AddItem(item, count);  // use count not item.StackCount
				if (remainder > 0)
				{
					GD.Print($"Could not fit {item.Name} — snapping back");
					if (dragData.IsEquipmentSlot && dragData.Character != null)
						dragData.Character.Equip(item);
					else if (dragData.Source == InventoryDragData.SourceType.Vault)
						_gameState.PartyVault.AddItem(item, count);
					else
						dragData.Character?.PersonalInventory.AddItem(item, count);

					if (swappedItem != null)
						inv.AddItem(swappedItem, swappedItem.StackCount);

					RefreshAll();
					return;
				}

				if (swappedItem != null)
				{
					if (dragData.IsEquipmentSlot && dragData.Character != null)
					{
						if (!dragData.Character.Equip(swappedItem))
							dragData.Character.PersonalInventory.AddItem(swappedItem, swappedItem.StackCount);
					}
					else if (dragData.Source == InventoryDragData.SourceType.Vault)
						_gameState.PartyVault.AddItem(swappedItem, swappedItem.StackCount);
					else
						dragData.Character?.PersonalInventory.AddItem(swappedItem, swappedItem.StackCount);
				}
			}
		}

		RefreshAll();
	}

	public void RefreshAll()
	{
		GD.Print($"RefreshAll - vault items: {_gameState.PartyVault.Items.Count}");
		foreach (var i in _gameState.PartyVault.Items)
			GD.Print($"  {i.Name} x{i.StackCount}");
		RefreshVault();
		var sheet = GetTree().Root.GetNodeOrNull<CharacterSheet>("/root/CharacterSheet");
		sheet?.RefreshDoll();
		sheet?.RefreshInventory();
	}
}
