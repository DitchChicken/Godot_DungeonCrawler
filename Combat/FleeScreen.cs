using Godot;

public partial class FleeScreen : Control
{
	private Label _messageLabel;
	private Button _continueButton;
	private GameState _gameState;

	public override void _Ready()
	{
		_gameState      = GetNode<GameState>("/root/GameState");
		_messageLabel   = GetNode<Label>("Panel/VBoxContainer/MessageLabel");
		_continueButton = GetNode<Button>("Panel/VBoxContainer/ContinueButton");
		_continueButton.Pressed += OnContinuePressed;
	}

	public void Initialize()
	{
		_messageLabel.Text =
			"The party fled successfully.\n" +
			"They make their way back to a known room.";
	}

	private void OnContinuePressed()
	{
		var dungeon = _gameState.CurrentDungeon;
		var state   = _gameState.GetDungeonState(dungeon);

		var explored = new System.Collections.Generic.List<string>(state.ExploredRooms);
		explored.Remove(_gameState.CurrentRoom?.Id);

		string targetRoom = explored.Count > 0
			? explored[new System.Random().Next(explored.Count)]
			: _gameState.CurrentRoom?.Id;

		if (targetRoom != null)
			_gameState.CurrentRoom = DungeonManager.LoadRoom(dungeon, targetRoom);

		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Dungeons/Dungeon.tscn");
	}
}
