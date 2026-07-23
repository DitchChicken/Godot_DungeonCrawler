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
	private Label _backstoryLabel;
	private Label _encumbranceLabel;
	
	public enum SheetTab { Equipment, Spells, Techniques, Domains }

	private SheetTab _currentTab = SheetTab.Equipment;
	private VBoxContainer _tabColumn;
	private VBoxContainer _rightPanel;

	// Tab content roots
	private VBoxContainer _equipmentView;
	private VBoxContainer _spellsView;
	private VBoxContainer _techniquesView;
	private VBoxContainer _domainsView;

	private AbilityIconGrid _spellGrid;
	private AbilityIconGrid _techniqueGrid;
	private DomainPanel _domainPanel;
	private AbilityTooltip _tooltip;

	// Right panel
	private EquipmentDoll _equipmentDoll;
	private InventoryPanel _inventoryPanel;
	
	public Character CurrentCharacter => _currentCharacter;
	
	public override void _Ready()
	{
		// Wire up all nodes
		string left = "Panel/MainContainer/LeftPanel/";
		string stats = left + "StatsPanel/";

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
		_backstoryLabel    = GetNode<Label>(left + "BackstoryLabel");

		_rightPanel = GetNode<VBoxContainer>("Panel/MainContainer/RightPanel");
		foreach (Node child in _rightPanel.GetChildren())
			child.QueueFree();

		BuildTabColumn();
		BuildEquipmentView();
		BuildSpellsView();
		BuildTechniquesView();
		BuildDomainsView();

		// Tooltip sits on the CanvasLayer so it floats above the panel
		_tooltip = new AbilityTooltip();
		AddChild(_tooltip);
		_spellGrid.Setup(AbilityType.Spell, _tooltip);
		_techniqueGrid.Setup(AbilityType.Technique, _tooltip);

		SetTab(SheetTab.Equipment);
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
		//GD.Print($"Double clicked: {item.Name}");

		// Try to equip if it has an equipment slot
		if (_currentCharacter.Equip(item))
		{
			_currentCharacter.PersonalInventory.RemoveItem(item);
			_equipmentDoll.Refresh();
			_inventoryPanel.Refresh();
			//GD.Print($"Equipped {item.Name} from inventory");
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
		
		// Load menus
		_equipmentDoll.LoadCharacter(character);
		_inventoryPanel.LoadCharacter(character);
		_spellGrid.LoadCharacter(character);
		_techniqueGrid.LoadCharacter(character);
		_domainPanel.LoadCharacter(character);
	}

	private void _on_back_button_pressed()
	{
		Hide();
	}

	public void RefreshInventory()
	{		
		_inventoryPanel?.Refresh();
		RefreshEncumbrance();
	}
	
	public void RefreshDoll()
	{
		_equipmentDoll?.Refresh();
	}	
	
	public void RefreshEncumbrance()
	{
		if (_encumbranceLabel == null || CurrentCharacter == null) return;

		float current = CurrentCharacter.PersonalInventory.CurrentWeight;
		float max     = CurrentCharacter.MaxEncumbrance;

		_encumbranceLabel.Text = $"Encumbrance: {current:F1} / {max:F1} lbs";

		// Color red if over capacity
		if (current > max)
			_encumbranceLabel.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f));
		else
			_encumbranceLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.7f));
	}
	
	private void BuildTabColumn()
	{
		// Insert the tab column beside the portrait, in the left panel's parent HBox
		var mainContainer = GetNode<HBoxContainer>("Panel/MainContainer");

		_tabColumn = new VBoxContainer();
		_tabColumn.AddThemeConstantOverride("separation", 4);
		_tabColumn.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;

		AddTabButton("E", SheetTab.Equipment);
		AddTabButton("S", SheetTab.Spells);
		AddTabButton("T", SheetTab.Techniques);
		AddTabButton("D", SheetTab.Domains);

		// Place between LeftPanel and RightPanel
		mainContainer.AddChild(_tabColumn);
		mainContainer.MoveChild(_tabColumn, 1);
	}

	private void AddTabButton(string letter, SheetTab tab)
	{
		var btn = new Button();
		btn.Text = letter;
		btn.CustomMinimumSize = new Vector2(32, 32);
		btn.ToggleMode = true;
		var captured = tab;
		btn.Pressed += () => SetTab(captured);
		btn.SetMeta("tab", (int)tab);
		_tabColumn.AddChild(btn);
	}

	private void BuildEquipmentView()
	{
		_equipmentView = new VBoxContainer();
		_equipmentView.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_equipmentView.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_rightPanel.AddChild(_equipmentView);

		_equipmentDoll = new EquipmentDoll();
		_equipmentDoll.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_equipmentDoll.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_equipmentDoll.SlotClicked        += OnSlotClicked;
		_equipmentDoll.SlotDoubleClicked  += OnSlotDoubleClicked;
		_equipmentView.AddChild(_equipmentDoll);

		_equipmentView.AddChild(new HSeparator());

		_encumbranceLabel = new Label();
		_encumbranceLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_encumbranceLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.7f));
		_equipmentView.AddChild(_encumbranceLabel);

		var invLabel = new Label();
		invLabel.Text = "Inventory";
		invLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_equipmentView.AddChild(invLabel);

		_inventoryPanel = new InventoryPanel();
		_inventoryPanel.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_inventoryPanel.SizeFlagsVertical   = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_inventoryPanel.ItemClicked        += OnInventoryItemClicked;
		_inventoryPanel.ItemDoubleClicked  += OnInventoryItemDoubleClicked;
		_equipmentView.AddChild(_inventoryPanel);
	}

	private void BuildSpellsView()
	{
		_spellsView = new VBoxContainer();
		_spellsView.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_rightPanel.AddChild(_spellsView);

		var header = new Label();
		header.Text = "Spells";
		header.HorizontalAlignment = HorizontalAlignment.Center;
		_spellsView.AddChild(header);
		_spellsView.AddChild(new HSeparator());

		_spellGrid = new AbilityIconGrid();
		_spellsView.AddChild(_spellGrid);
	}

	private void BuildTechniquesView()
	{
		_techniquesView = new VBoxContainer();
		_techniquesView.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_rightPanel.AddChild(_techniquesView);

		var header = new Label();
		header.Text = "Techniques";
		header.HorizontalAlignment = HorizontalAlignment.Center;
		_techniquesView.AddChild(header);
		_techniquesView.AddChild(new HSeparator());

		_techniqueGrid = new AbilityIconGrid();
		_techniquesView.AddChild(_techniqueGrid);
	}

	private void BuildDomainsView()
	{
		_domainsView = new VBoxContainer();
		_domainsView.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
		_rightPanel.AddChild(_domainsView);

		var header = new Label();
		header.Text = "Domains";
		header.HorizontalAlignment = HorizontalAlignment.Center;
		_domainsView.AddChild(header);
		_domainsView.AddChild(new HSeparator());

		_domainPanel = new DomainPanel();
		_domainsView.AddChild(_domainPanel);
	}

	private void SetTab(SheetTab tab)
	{
		_currentTab = tab;
		_tooltip?.HideTooltip();

		_equipmentView.Visible  = tab == SheetTab.Equipment;
		_spellsView.Visible     = tab == SheetTab.Spells;
		_techniquesView.Visible = tab == SheetTab.Techniques;
		_domainsView.Visible    = tab == SheetTab.Domains;

		// Sync the toggle state on the buttons
		foreach (Node child in _tabColumn.GetChildren())
			if (child is Button b && b.HasMeta("tab"))
				b.ButtonPressed = (int)b.GetMeta("tab") == (int)tab;
	}	
}
