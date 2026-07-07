using Godot;

public partial class PartySlot : PanelContainer
{
	public TextureRect Portrait;
	private Label _nameLabel;
	private Label _hpLabel;
	private Label _manaLabel;
	private StatusIconRow _statusRow;
	
	public Character Character { get; private set; }

	[Export] public bool IsHudSlot = false;

	public override void _Ready()
	{
		Portrait   = GetNode<TextureRect>("HBoxContainer/Portrait");
		_nameLabel = GetNode<Label>("HBoxContainer/VBoxContainer/NameLabel");
		_hpLabel   = GetNode<Label>("HBoxContainer/VBoxContainer/HpLabel");
		_manaLabel = GetNode<Label>("HBoxContainer/VBoxContainer/MpLabel");
		_statusRow = new StatusIconRow();
		_statusRow.Alignment = BoxContainer.AlignmentMode.Center;
		_statusRow.MouseFilter = Control.MouseFilterEnum.Ignore;
		var vbox = GetNode<VBoxContainer>("HBoxContainer/VBoxContainer");
		vbox.AddChild(_statusRow);
		
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
		var obj = data.Obj;

		// Item transfer (from inventory/vault/equipment)
		if (obj is InventoryDragData) return true;

		// Party member swap (HUD only)
		if (IsHudSlot && obj is Character) return true;

		return false;
	}



	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var gameState = GetNode<GameState>("/root/GameState");
		var obj = data.Obj;

		// Handle item transfer to this character
		if (obj is InventoryDragData itemData && Character != null)
		{
			var target = TransferTarget.ToInventory(Character);
			bool moved = InventoryTransfer.Transfer(itemData, target, gameState);

			if (moved)
				GD.Print($"Transferred {itemData.Item.Name} to {Character.Name}");
			else
				GD.Print($"{Character.Name} can't accept {itemData.Item.Name}");

			var sheet = GetTree().Root.GetNodeOrNull<CharacterSheet>("/root/CharacterSheet");
			sheet?.RefreshDoll();
			sheet?.RefreshInventory();
			var vault = GetTree().Root.GetNodeOrNull<Vault>("Vault");
			vault?.RefreshAll();
			return;
		}
		
		// Party member swap
		if (obj is not Character incomingCharacter) return;
	
		incomingCharacter = data.As<Character>();
		if (incomingCharacter == null) return;

		var partyHud  = GetNode<PartyHUD>("/root/PartyHud");
		var allSlots  = partyHud.GetAllSlots();

		// Find source slot if dragging from another HUD slot
		PartySlot sourceSlot = null;
		foreach (var slot in allSlots)
		{
			if (slot.Character == incomingCharacter)
			{
				sourceSlot = slot;
				break;
			}
		}

		// Swap if this slot is occupied
		var existingCharacter = Character;

		// Place incoming character in this slot
		SetCharacter(incomingCharacter);

		// If came from another HUD slot, put existing there
		// If came from roster, just clear source slot
		if (sourceSlot != null)
			sourceSlot.SetCharacter(existingCharacter);

		// Rebuild party list from slot order
		gameState.Party.Clear();
		foreach (var slot in allSlots)
		{
			if (slot.Character != null)
				gameState.Party.Add(slot.Character);
		}

		// Update location flags
		foreach (var character in gameState.Party)
			character.Location = Location.Party;

		if (existingCharacter != null && !gameState.Party.Contains(existingCharacter))
			existingCharacter.Location = Location.Stable;

		// Refresh roster status if open
		var roster = GetTree().Root.GetNodeOrNull<Roster>("Roster");
		roster?.RefreshPartyStatus();
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
	
	public void RefreshStatus()
	{
		if (Character != null)
			_statusRow?.Refresh(Character.ActiveEffects);
		else
			_statusRow?.Refresh(null);
	}
}
