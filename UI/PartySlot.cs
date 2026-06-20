using Godot;

public partial class PartySlot : PanelContainer
{
	public TextureRect Portrait;
	private Label _nameLabel;
	private Label _hpLabel;
	private Label _manaLabel;

	public Character Character { get; private set; }

	[Export] public bool IsHudSlot = false;

	public override void _Ready()
	{
		Portrait   = GetNode<TextureRect>("HBoxContainer/Portrait");
		_nameLabel = GetNode<Label>("HBoxContainer/VBoxContainer/NameLabel");
		_hpLabel   = GetNode<Label>("HBoxContainer/VBoxContainer/HpLabel");
		_manaLabel = GetNode<Label>("HBoxContainer/VBoxContainer/MpLabel");

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

		// Portrait
		if (!string.IsNullOrEmpty(character.Portrait)
			&& ResourceLoader.Exists(character.Portrait))
			Portrait.Texture = GD.Load<Texture2D>(character.Portrait);
		else
			Portrait.Texture = null;

		// Labels
		_nameLabel.Text = character.Name;
		_hpLabel.Text   = $"HP: {character.CurrentHP}/{character.MaxHP}";

		// Only show mana for casters
		if (character.MaxMana > 0)
		{
			_manaLabel.Text    = $"MP: {character.CurrentMana}/{character.MaxMana}";
			_manaLabel.Visible = true;
		}
		else
		{
			_manaLabel.Visible = false;
		}
	}

	public void Clear()
	{
		Character          = null;
		Portrait.Texture   = null;
		_nameLabel.Text    = "";
		_hpLabel.Text      = "";
		_manaLabel.Visible = false;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (Character == null) return new Variant();

		var preview = new TextureRect();
		preview.Texture = Portrait.Texture;
		preview.Size    = new Vector2(64, 64);
		SetDragPreview(preview);

		return Variant.From(Character);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!IsHudSlot) return false;
		return data.As<Character>() != null;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var incomingCharacter = data.As<Character>();
		if (incomingCharacter == null) return;

		var gameState = GetNode<GameState>("/root/GameState");
		var partyHud  = GetNode<PartyHUD>("/root/PartyHud");

		if (IsHudSlot)
		{
			var allSlots = partyHud.GetAllSlots();
			PartySlot sourceSlot = null;

			foreach (var slot in allSlots)
			{
				if (slot.Character == incomingCharacter)
				{
					sourceSlot = slot;
					break;
				}
			}

			var existingCharacter = Character;

			// Remove existing character from party if slot was occupied
			if (existingCharacter != null)
				gameState.RemoveFromParty(existingCharacter);

			// Add incoming character to party
			gameState.AddToParty(incomingCharacter);

			// Update slot visuals
			SetCharacter(incomingCharacter);
			if (sourceSlot != null)
				sourceSlot.SetCharacter(existingCharacter);

			// Rebuild party order from HUD slots
			gameState.Party.Clear();
			foreach (var slot in allSlots)
			{
				if (slot.Character != null)
					gameState.Party.Add(slot.Character);
			}

			// Refresh roster if it's currently open
			var roster = GetTree().Root.GetNodeOrNull<Roster>("Roster");
			roster?.RefreshPartyStatus();
		}
		else
		{
			gameState.RemoveFromParty(incomingCharacter);
			partyHud.Refresh();
		}
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
	
	public void SetInParty(bool inParty)
	{
		// Simple approach - tint the panel
		Modulate = inParty 	        
			? new Color(1.0f, 1.0f, 1.0f)  // normal
			: new Color(0.85f, 1.0f, 0.85f); // green tint = on bench
	}
}
