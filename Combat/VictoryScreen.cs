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

	private Random _rng = new Random();
	private const float LootSlotSize    = 128f;
	private const int   LootGridColumns = 8;

	private Inventory _lootInventory;

	public override void _Ready()
	{
		_gameState      = GetNode<GameState>("/root/GameState");
		_titleLabel     = GetNode<Label>("LeftPanel/VBoxContainer/TitleLabel");
		_xpLabel        = GetNode<Label>("LeftPanel/VBoxContainer/XPLabel");
		_lootGrid       = GetNode<GridContainer>("LeftPanel/VBoxContainer/LootGrid");
		_continueButton = GetNode<Button>("LeftPanel/VBoxContainer/ContinueButton");
		_rightPanel     = GetNode<Control>("RightPanel");

		_continueButton.Pressed += OnContinuePressed;
	}

	public void Initialize(LootResult lootResult, CombatState combatState)
	{
		_lootResult = lootResult;

		// Build loot inventory (unlimited, like the vault)
		_lootInventory = new Inventory(64, 0, 0);
		foreach (var (item, count) in _lootResult.Items)
			_lootInventory.AddItem(item.CloneEquipment(), count);

		// Register as the active loot target for drag/drop
		LootContext.Set(_lootInventory, BuildLootGrid);

		// Award XP to living conscious survivors
		int xp = lootResult.ExperiencePerSurvivor;
		foreach (var character in _gameState.Party)
		{
			if (character.IsAlive && character.Status == Status.Ok)
				character.GainExperience(xp);
		}
		_xpLabel.Text = $"Each survivor gains {xp} XP";

		BuildLootGrid();

		// Open first living party member's character sheet on the right
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
		int totalSlots = LootGridColumns * 2;

		for (int i = 0; i < totalSlots; i++)
		{
			var slot = new InventorySlotButton();
			slot.SlotIndex         = i;
			slot.SourceType        = InventoryDragData.SourceType.Loot;
			slot.CustomMinimumSize = new Vector2(LootSlotSize, LootSlotSize);
			slot.CustomMaximumSize = new Vector2(LootSlotSize, LootSlotSize);
			slot.ExpandIcon        = true;
			slot.IconAlignment     = HorizontalAlignment.Center;

			if (i < _lootInventory.Items.Count)
			{
				var item = _lootInventory.Items[i];
				slot.Item = item;
				if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
					slot.Icon = GD.Load<Texture2D>(item.Icon);
			}

			_lootGrid.AddChild(slot);
		}
	}

	private void OnContinuePressed()
	{
		// Anything left behind drops to the room floor
		StoreRemainingLootInRoom();

		GetNode<CharacterSheet>("/root/CharacterSheet").Hide();

		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Dungeons/Dungeon.tscn");
	}

	private void StoreRemainingLootInRoom()
	{
		if (_lootInventory == null || _lootInventory.UsedSlots == 0) return;

		string dungeonId = _gameState.CurrentDungeon;
		string roomId    = _gameState.CurrentRoom?.Id;
		if (string.IsNullOrEmpty(dungeonId) || string.IsNullOrEmpty(roomId)) return;

		var roomState = _gameState.GetDungeonState(dungeonId).GetRoomState(roomId);

		// Copy counts first — RemoveItem zeroes StackCount
		var leftovers = new List<(Equipment item, int count)>();
		foreach (var item in _lootInventory.Items)
			leftovers.Add((item, item.StackCount));

		foreach (var (item, count) in leftovers)
			roomState.LootPile.AddItem(item.CloneEquipment(), count);

		GD.Print($"Left {leftovers.Count} stack(s) of loot in {roomId}");
	}

	public override void _ExitTree()
	{
		LootContext.Clear();
	}
}
