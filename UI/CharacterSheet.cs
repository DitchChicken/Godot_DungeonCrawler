using Godot;

public enum CharacterSheetMode { Right, Center };

public partial class CharacterSheet : CanvasLayer
{
	private Character _currentCharacter;

	// Left panel
	private TextureRect _portrait;
	private Label _nameLabel;
	private Label _raceClassLabel;
	private Label _alignmentLabel;
	private Label _levelExpLabel;
	private Label _statusLabel;
	private Label _strLabel;
	private Label _conLabel;
	private Label _dexLabel;
	private Label _intLabel;
	private Label _wisLabel;
	private Label _chaLabel;
	private Label _hpLabel;
	private Label _manaLabel;
	private Label _encumbranceLabel;
	private Label _backstoryLabel;

	// Right panel
	private EquipmentDoll _equipmentDoll;
	private InventoryPanel _inventoryPanel;
	
	public Character CurrentCharacter => _currentCharacter;
	
	public override void _Ready()
	{
		// Wire up all nodes
		string left = "Panel/MainContainer/LeftPanel/";
		string stats = left + "StatsPanel/";
		//string right = "Panel/MainContainer/RightPanel/EquipmentPanel/";

		_portrait          = GetNode<TextureRect>(left + "Portrait");
		_nameLabel         = GetNode<Label>(stats + "NameLabel");
		_raceClassLabel    = GetNode<Label>(stats + "RaceClassLabel");
		_alignmentLabel    = GetNode<Label>(stats + "AlignmentLabel");
		_levelExpLabel     = GetNode<Label>(stats + "LevelExpLabel");
		_statusLabel       = GetNode<Label>(stats + "StatusLabel");
		_strLabel          = GetNode<Label>(stats + "StrLabel");
		_conLabel          = GetNode<Label>(stats + "ConLabel");
		_dexLabel          = GetNode<Label>(stats + "DexLabel");
		_intLabel          = GetNode<Label>(stats + "IntLabel");
		_wisLabel          = GetNode<Label>(stats + "WisLabel");
		_chaLabel          = GetNode<Label>(stats + "ChaLabel");
		_hpLabel           = GetNode<Label>(stats + "HpLabel");
		_manaLabel         = GetNode<Label>(stats + "ManaLabel");
		_encumbranceLabel  = GetNode<Label>(stats + "EncumbranceLabel");
		_backstoryLabel    = GetNode<Label>(left + "BackstoryLabel");
	
		// Replace right panel with doll
		var rightPanel = GetNode<VBoxContainer>("Panel/MainContainer/RightPanel");
		
		// Remove old equipment labels
		foreach (Node child in rightPanel.GetChildren())
			child.QueueFree();

		// Equipment doll - top portion
		_equipmentDoll = new EquipmentDoll();
		_equipmentDoll.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_equipmentDoll.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_equipmentDoll.SlotClicked        += OnSlotClicked;
		_equipmentDoll.SlotDoubleClicked  += OnSlotDoubleClicked;
		rightPanel.AddChild(_equipmentDoll);

		// Wire signals
		_equipmentDoll.SlotClicked       += OnSlotClicked;
		_equipmentDoll.SlotDoubleClicked += OnSlotDoubleClicked;

		// Separator
		var separator = new HSeparator();
		rightPanel.AddChild(separator);

		// Inventory label
		var label = new Label();
		label.Text                = "Inventory";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		rightPanel.AddChild(label);

		// Inventory panel - bottom portion
		_inventoryPanel = new InventoryPanel();
		_inventoryPanel.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_inventoryPanel.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_inventoryPanel.ItemClicked        += OnInventoryItemClicked;
		_inventoryPanel.ItemDoubleClicked  += OnInventoryItemDoubleClicked;
		rightPanel.AddChild(_inventoryPanel);		

		Hide();
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event is InputEventKey key 
			&& key.Pressed 
			&& key.Keycode == Key.Escape)
		{
			Hide();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnSlotClicked(int slot)
	{
		var equipmentSlot = (EquipmentSlot)slot;
		GD.Print($"Slot clicked: {equipmentSlot}");
		// TODO: Open inventory overlay
	}

	private void OnSlotDoubleClicked(int slot)
	{
		var equipmentSlot = (EquipmentSlot)slot;
		var item = _currentCharacter?.GetEquipped(equipmentSlot);
		if (item != null)
		{
			_currentCharacter.Unequip(equipmentSlot);
			_equipmentDoll.Refresh();
			GD.Print($"Unequipped: {item.Name}");
		}
		else
		{
			GD.Print($"Empty slot clicked: {equipmentSlot}");
			// TODO: Open inventory overlay
		}
	}

	private void OnInventoryItemClicked(int slotIndex)
	{
		GD.Print($"Inventory slot clicked: {slotIndex}");
		// TODO: Open vault/transfer UI
	}

	private void OnInventoryItemDoubleClicked(int slotIndex)
	{
		if (_currentCharacter == null) return;
		var items = _currentCharacter.PersonalInventory.Items;
		if (slotIndex >= items.Count) return;

		var item = items[slotIndex];
		GD.Print($"Double clicked: {item.Name}");

		// Try to equip if it has an equipment slot
		if (_currentCharacter.Equip(item))
		{
			_currentCharacter.PersonalInventory.RemoveItem(item);
			_equipmentDoll.Refresh();
			_inventoryPanel.Refresh();
			GD.Print($"Equipped {item.Name} from inventory");
		}
	}
	public void Open(Character character, CharacterSheetMode mode = CharacterSheetMode.Right)
	{
		PositionPanel(mode);
		Populate(character);
		Show();
	}

	private void PositionPanel(CharacterSheetMode mode)
	{
		var panel = GetNode<Panel>("Panel");

		switch (mode)
		{
			case CharacterSheetMode.Right:
				panel.AnchorLeft   = 0.5f;
				panel.AnchorTop    = 0.0f;
				panel.AnchorRight  = 1.0f;
				panel.AnchorBottom = 0.65f;
				break;

			case CharacterSheetMode.Center:
				panel.AnchorLeft   = 0.2f;
				panel.AnchorTop    = 0.05f;
				panel.AnchorRight  = 0.8f;
				panel.AnchorBottom = 0.65f;
				break;
		}

		panel.OffsetLeft   = 0;
		panel.OffsetTop    = 0;
		panel.OffsetRight  = 0;
		panel.OffsetBottom = 0;
	}

	private void Populate(Character character)
	{
		// Portrait
		if (!string.IsNullOrEmpty(character.Portrait)
			&& ResourceLoader.Exists(character.Portrait))
			_portrait.Texture = GD.Load<Texture2D>(character.Portrait);
		else
			_portrait.Texture = null;

		// Identity
		_nameLabel.Text       = character.Name;
		_raceClassLabel.Text  = $"{character.Race} {character.ClassType}";
		_alignmentLabel.Text  = $"Alignment: {character.Alignment}";
		_levelExpLabel.Text   = $"Level {character.Level}  |  EXP: {character.Experience}/{character.ExperienceToNextLevel}";
		_statusLabel.Text     = $"Status: {character.Status}";

		// Core stats
		_strLabel.Text  = $"STR: {character.Strength}";
		_conLabel.Text  = $"CON: {character.Constitution}";
		_dexLabel.Text  = $"DEX: {character.Dexterity}";
		_intLabel.Text  = $"INT: {character.Intelligence}";
		_wisLabel.Text  = $"WIS: {character.Wisdom}";
		_chaLabel.Text  = $"CHA: {character.Charisma}";

		// Derived stats
		_hpLabel.Text          = $"HP: {character.CurrentHP} / {character.MaxHP}";
		_manaLabel.Text        = $"Mana: {character.CurrentMana} / {character.MaxMana}";
		_encumbranceLabel.Text = $"Encumbrance: {character.CurrentEncumbrance} / {character.MaxEncumbrance}";

		// Backstory
		_backstoryLabel.Text = character.Backstory ?? "";
		
		// Load doll
		_equipmentDoll.LoadCharacter(character);
		_inventoryPanel.LoadCharacter(character);
	}

	private void _on_back_button_pressed()
	{
		Hide();
	}

	public void RefreshInventory()
	{
		_inventoryPanel?.Refresh();
	}
	
	public void RefreshDoll()
	{
		_equipmentDoll?.Refresh();
	}	
}
