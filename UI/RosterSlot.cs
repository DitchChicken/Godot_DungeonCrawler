using Godot;

public partial class RosterSlot : PanelContainer
{
	private TextureRect _portrait;
	private Label _nameLabel;

	public Character Character { get; private set; }

	public override void _Ready()
	{
		_portrait  = GetNode<TextureRect>("VBoxContainer/Portrait");
		_nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
		Clear();
	}

	public void SetCharacter(Character character)
	{
		Character = character;

		if (character == null)
		{
			Clear();
			return;
		}

		if (!string.IsNullOrEmpty(character.Portrait)
			&& ResourceLoader.Exists(character.Portrait))
			_portrait.Texture = GD.Load<Texture2D>(character.Portrait);
		else
			_portrait.Texture = null;

		_nameLabel.Text = character.Name;		
	}

	public void SetInParty(bool inParty)
	{
		Modulate = inParty
			? new Color(0.6f, 1.0f, 0.6f)
			: new Color(1.0f, 1.0f, 1.0f);
	}

	public void Clear()
	{
		Character       = null;
		_portrait.Texture = null;
		_nameLabel.Text = "";
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse
			&& mouse.ButtonIndex == MouseButton.Left
			&& mouse.Pressed
			&& Character != null)
		{
			GetNode<CharacterSheet>("/root/CharacterSheet")
				.Open(Character, CharacterSheetMode.Right);
		}
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (Character == null) return new Variant();

		var preview = new TextureRect();
		preview.Texture = _portrait.Texture;
		preview.Size    = new Vector2(64, 64);
		SetDragPreview(preview);

		return Variant.From(Character);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		// Accept characters dropped back from HUD
		return data.As<Character>() != null;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var character = data.As<Character>();
		if (character == null) return;

		var gameState = GetNode<GameState>("/root/GameState");
		
		// Remove from party when dropped back to roster
		gameState.RemoveFromParty(character);
		
		// Refresh HUD and roster status
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
		RefreshRoster();
	}

	private void RefreshRoster()
	{
		// Find the roster and refresh party status indicators
		var roster = GetTree().Root.GetNodeOrNull<Roster>("Roster");
		roster?.RefreshPartyStatus();
	}	
	
}
