using Godot;
using System;
using System.Collections.Generic;

public partial class VictoryScreen : Control
{
	private Label _titleLabel;
	private Label _xpLabel;
	private GridContainer _lootGrid;
	private Button _continueButton;
	private Control _rightPanel;

	private LootResult _lootResult;
	private GameState _gameState;
	private List<(Equipment item, int count)> _remainingLoot;
	private Random _rng = new Random();

	private const float LootSlotSize   = 128f;
	private const int   LootGridColumns = 8;

	public override void _Ready()
	{
		_gameState     = GetNode<GameState>("/root/GameState");
		_titleLabel    = GetNode<Label>("LeftPanel/VBoxContainer/TitleLabel");
		_xpLabel       = GetNode<Label>("LeftPanel/VBoxContainer/XPLabel");
		_lootGrid      = GetNode<GridContainer>("LeftPanel/VBoxContainer/LootGrid");
		_continueButton = GetNode<Button>("LeftPanel/VBoxContainer/ContinueButton");
		_rightPanel    = GetNode<Control>("RightPanel");

		_continueButton.Pressed += OnContinuePressed;
	}

	public void Initialize(LootResult lootResult, CombatState combatState)
	{
		_lootResult    = lootResult;
		_remainingLoot = new List<(Equipment, int)>(_lootResult.Items);

		// Award XP to living conscious survivors
		int xp = lootResult.ExperiencePerSurvivor;
		foreach (var character in _gameState.Party)
		{
			if (character.IsAlive && character.Status == Status.Ok)
				character.GainExperience(xp);
		}

		_xpLabel.Text = $"Each survivor gains {xp} XP";

		BuildLootGrid();

		// Open first living party member's character sheet on right
		var sheet = GetNode<CharacterSheet>("/root/CharacterSheet");
		foreach (var c in _gameState.Party)
		{
			if (c.IsAlive)
			{
				sheet.Open(c, CharacterSheetMode.Right);
				break;
			}
		}
	}

	private void BuildLootGrid()
	{
		foreach (Node child in _lootGrid.GetChildren())
			child.QueueFree();

		_lootGrid.Columns = LootGridColumns;

		if (_remainingLoot.Count == 0)
		{
			var emptyLabel = new Label();
			emptyLabel.Text = "No loot.";
			_lootGrid.AddChild(emptyLabel);
			return;
		}

		foreach (var (item, count) in _remainingLoot)
		{
			var slot = new InventorySlotButton();
			slot.Item            = item.CloneEquipment();
			slot.Item.StackCount = count;
			slot.SourceType      = InventoryDragData.SourceType.Vault;
			slot.CustomMinimumSize = new Vector2(LootSlotSize, LootSlotSize);
			slot.CustomMaximumSize = new Vector2(LootSlotSize, LootSlotSize);
			slot.ExpandIcon      = true;
			slot.IconAlignment   = HorizontalAlignment.Center;

			if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
				slot.Icon = GD.Load<Texture2D>(item.Icon);

			_lootGrid.AddChild(slot);
		}

		// Fill remaining empty slots
		int totalSlots = LootGridColumns * 2;
		for (int i = _remainingLoot.Count; i < totalSlots; i++)
		{
			var empty = new Button();
			empty.Disabled          = true;
			empty.CustomMinimumSize = new Vector2(LootSlotSize, LootSlotSize);
			empty.CustomMaximumSize = new Vector2(LootSlotSize, LootSlotSize);
			_lootGrid.AddChild(empty);
		}
	}

	private void OnContinuePressed()
	{
		// Store remaining loot in current room as a loot pile
		if (_remainingLoot.Count > 0)
		{
			GD.Print($"TODO: Store {_remainingLoot.Count} items as room loot pile");
			// We'll implement loot piles when we add room revisiting
		}

		// Close character sheet
		GetNode<CharacterSheet>("/root/CharacterSheet").Hide();

		// Return to dungeon
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Dungeons/Dungeon.tscn");
	}
}
