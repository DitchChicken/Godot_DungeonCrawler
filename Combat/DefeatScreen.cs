using Godot;

public partial class DefeatScreen : Control
{
	private Label _titleLabel;
	private Label _consequenceLabel;
	private Button _returnButton;
	private GameState _gameState;

	public override void _Ready()
	{
		_gameState         = GetNode<GameState>("/root/GameState");
		_titleLabel        = GetNode<Label>("Panel/VBoxContainer/TitleLabel");
		_consequenceLabel  = GetNode<Label>("Panel/VBoxContainer/ConsequenceLabel");
		_returnButton      = GetNode<Button>("Panel/VBoxContainer/ReturnButton");
		_returnButton.Pressed += OnReturnPressed;
	}

	public void Initialize()
	{
		ApplyDefeatConsequences();
		_consequenceLabel.Text = GetConsequenceText();
	}

	private void ApplyDefeatConsequences()
	{
		switch (_gameState.CurrentDifficulty)
		{
			case GameState.Difficulty.Easy:
				// Full heal, no penalties
				foreach (var c in _gameState.Party)
				{
					c.CurrentHP   = c.MaxHP;
					c.CurrentMana = c.MaxMana;
					c.Status      = Status.Ok;
				}
				break;

			case GameState.Difficulty.Normal:
				// Lose gold and items, keep equipment, status = Dead
				foreach (var c in _gameState.Party)
				{
					// Remove non-equipment items from inventory
					var toRemove = new System.Collections.Generic.List<Equipment>();
					foreach (var item in c.PersonalInventory.Items)
						if (item.Slot != EquipmentSlot.Treasure)
							toRemove.Add(item);
					foreach (var item in toRemove)
						c.PersonalInventory.RemoveItem(item, item.StackCount);

					// Clear all treasure
					c.PersonalInventory.RemoveAllTreasure();
					c.Status = Status.Dead;
				}
				break;

			case GameState.Difficulty.Hard:
				// Same as normal but status = Lost
				foreach (var c in _gameState.Party)
				{
					c.PersonalInventory.RemoveAllTreasure();
					c.Status = Status.Lost;
				}
				break;
		}

		_gameState.ReturnToTown();
	}

	private string GetConsequenceText()
	{
		return _gameState.CurrentDifficulty switch
		{
			GameState.Difficulty.Easy =>
				"The party retreats to safety.\nAll wounds are healed.",
			GameState.Difficulty.Normal =>
				"The party falls in battle.\nAll gold and carried items are lost.\n" +
				"Visit the priest to revive your fallen comrades.",
			GameState.Difficulty.Hard =>
				"The party is lost to the dungeon.\n" +
				"All gold and carried items are lost.\n" +
				"Your adventurers are gone forever.",
			_ => ""
		};
	}

	private void OnReturnPressed()
	{
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Town/Town.tscn");
	}
}
