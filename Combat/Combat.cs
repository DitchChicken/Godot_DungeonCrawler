using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Combat : Control
{
	private Control _partyContainer;
	private Control _enemyContainer;
	private GameState _gameState;
	private CombatState _combatState;
	private TurnTracker _turnTracker;
	private SpriteHighlighter _highlighter = new SpriteHighlighter();
	
	// UI nodes
	private Label _combatantLabel;
	private Label _logLabel;
	private ScrollContainer _scrollContainer;
	private Button _attackButton;
	private Button _spellSkillButton;
	private Button _itemButton;
	private Button _rowSwitchButton;
	private Button _fleeButton;
	private HBoxContainer _turnStrip;
	private PanelContainer _actionPanel;
	private Button _autoWinButton;
	private Button _autoLoseButton;

	// Combat state
	private bool _selectingTarget = false;
	private List<TextureRect> _enemySprites = new List<TextureRect>();
	private List<TextureRect> _partySprites = new List<TextureRect>();
	private List<Monster> _enemyOrder = new List<Monster>(); // matches _enemySprites

	// Formation constants
	private const float PartyStartXFrac    = 0.1f;
	private const float PartyStartYFrac    = -0.20f;
	private const float PartyStaggerXFrac  = 0.080f;
	private const float PartyStaggerYFrac  = 0.275f;
	private const float PartyRowOffsetFrac = 0.35f;
	private const float PartySpriteSize    = 0.495f;
	
	private const float EnemyStartXFrac    =  0.750f;
	private const float EnemyStartYFrac    = -0.20f;
	private const float EnemyStaggerXFrac  = 0.08f;
	private const float EnemyStaggerYFrac  = 0.25f;
	private const float EnemyRowOffsetFrac = 0.25f;
	private const float EnemySpriteSize    = 0.35f;
	private const float ShadeMin           = 0.67f;
	private const float ShadeMax           = 1.00f;

	private Dictionary<Character, TextureRect> _partySpritemap = new Dictionary<Character, TextureRect>();
	private Dictionary<TextureRect, Image> _spriteImages = new Dictionary<TextureRect, Image>();
	private Dictionary<Monster, TextureRect> _monsterSpritemap = new Dictionary<Monster, TextureRect>();
	
	private bool _selectingRowSwap = false;
	private List<Character> _validSwapTargets = new List<Character>();

	public override void _Ready()
	{
		_partyContainer  = GetNode<Control>("PartyContainer");
		_enemyContainer  = GetNode<Control>("EnemyContainer");
		_gameState       = GetNode<GameState>("/root/GameState");
		_combatantLabel  = GetNode<Label>("ActionPanel/VBoxContainer/CombatantLabel");
		_logLabel        = GetNode<Label>("CombatLog/MarginContainer/ScrollContainer/LogLabel");
		_scrollContainer = GetNode<ScrollContainer>("CombatLog/MarginContainer/ScrollContainer");
		_attackButton    = GetNode<Button>("ActionPanel/VBoxContainer/AttackButton");
		_spellSkillButton = GetNode<Button>("ActionPanel/VBoxContainer/SpellSkillButton");
		_itemButton      = GetNode<Button>("ActionPanel/VBoxContainer/ItemButton");
		_rowSwitchButton = GetNode<Button>("ActionPanel/VBoxContainer/RowSwitchButton");
		_fleeButton      = GetNode<Button>("ActionPanel/VBoxContainer/FleeButton");
		_turnTracker = GetNode<TurnTracker>("TurnTracker");
		_turnTracker.SlotHovered   += OnTrackerSlotHovered;
		_turnTracker.SlotUnhovered += OnTrackerSlotUnhovered;
		_actionPanel = GetNode<PanelContainer>("ActionPanel");
		
		AddChild(_highlighter);
		
		// Connect buttons
		_attackButton.Pressed    += OnAttackPressed;
		_spellSkillButton.Pressed += OnSpellSkillPressed;
		_itemButton.Pressed      += OnItemPressed;
		_rowSwitchButton.Pressed += OnRowSwitchPressed;
		_fleeButton.Pressed      += OnFleePressed;
		
		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
			AutoFormParty();

		BuildDebugButtons();
		CallDeferred(nameof(LoadCombatants));
	}

	private void LoadCombatants()
	{
		LoadParty();		
		InitializeCombat();
		LoadEnemies();
		UpdateUI();
	}

	private void InitializeCombat()
	{
		var formation = new List<List<Monster>>();
		foreach (var row in _gameState.CurrentEncounter)
		{
			var monsterRow = new List<Monster>();
			foreach (var id in row)
			{
				var monster = MonsterLoader.LoadMonster(id);
				if (monster != null)
					monsterRow.Add(monster);
			}
			formation.Add(monsterRow);
		}

		_combatState = new CombatState(_gameState.Party, formation);
		_combatState.RollInitiative();
		
		_turnTracker.Initialize(_combatState, this);
		_turnTracker.SlotHovered   += OnTrackerSlotHovered;
		_turnTracker.SlotUnhovered += OnTrackerSlotUnhovered;

		RefreshCombatLog();
		RefreshPartySprites();
		
		/*
		GD.Print("AllMonsters:");
		foreach (var m in _combatState.AllMonsters)
			GD.Print($"  {m.CombatLabel} hash:{m.GetHashCode()}");
		
		GD.Print("TurnOrder monsters:");
		foreach (var c in _combatState.TurnOrder.Where(c => !c.IsParty))
			GD.Print($"  {c.Monster.CombatLabel} hash:{c.Monster.GetHashCode()}");

		GD.Print("EnemyOrder (after LoadEnemies):"); 
		// Add this after LoadEnemies() call 
		foreach (var m in _enemyOrder)
			GD.Print($"  {m.CombatLabel} hash:{m.GetHashCode()}");
		*/
	}

	private void UpdateUI()
	{
		if (_combatState == null) return;

		var current = _combatState.CurrentCombatant;
		if (current == null) return;

		_combatantLabel.Text = $"{current.Name}'s Turn";

		bool isPlayerTurn = _combatState.CurrentPhase == CombatState.Phase.PlayerTurn;
		SetActionButtonsEnabled(isPlayerTurn);

		HighlightActiveCombatant();
		HighlightActiveHudSlot();
		_turnTracker.Refresh();

		if (!isPlayerTurn)
			ProcessEnemyTurn();
	}
	
	private void SetActionButtonsEnabled(bool enabled)
	{
		_attackButton.Disabled    = !enabled;
		_spellSkillButton.Disabled = !enabled;
		_itemButton.Disabled      = !enabled;
		_rowSwitchButton.Disabled = !enabled;
		_fleeButton.Disabled      = !enabled;
	}

	// Refresh combat log display
	private void RefreshCombatLog()
	{
		_logLabel.Text = string.Join("\n", _combatState.Log);
		// Scroll to bottom
		CallDeferred(nameof(ScrollLogToBottom));
	}

	private void ScrollLogToBottom()
	{
		_scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
	}

	private void AddLog(string message)
	{
		_combatState.AddLog(message);
		RefreshCombatLog();
	}

	// --- Button Handlers ---

	private void OnAttackPressed()
	{
		if (_combatState.CurrentPhase != CombatState.Phase.PlayerTurn) return;
		EnterTargetSelectMode();
	}

	private void OnSpellSkillPressed()
	{
		GD.Print("Spell/Skill menu TODO");
	}

	private void OnItemPressed()
	{
		GD.Print("Item menu TODO");
	}

	private void OnRowSwitchPressed()
	{
		if (_combatState.CurrentPhase != CombatState.Phase.PlayerTurn) return;
		var current = _combatState.CurrentCombatant;
		if (current == null || !current.IsParty) return;

		_validSwapTargets = _combatState.GetValidSwapTargets(current.Character);
		if (_validSwapTargets.Count == 0)
		{
			AddLog("No valid swap targets.");
			return;
		}

		_selectingRowSwap = true;
		HighlightSwapTargets();
	}

	private void HighlightSwapTargets()
	{
		foreach (var character in _validSwapTargets)
		{
			if (_partySpritemap.TryGetValue(character, out var sprite))
				_highlighter.SetHighlight(sprite, SpriteHighlighter.HighlightType.Ally);
		}
	}

	private void ClearSwapHighlights()
	{
		_highlighter.ClearHighlight(SpriteHighlighter.HighlightType.Ally);
	}

	private void ExitRowSwitchMode()
	{
		_selectingRowSwap = false;
		_validSwapTargets.Clear();
		ClearSwapHighlights();
	}

	// --- Target Selection ---
	private void EnterTargetSelectMode()
	{
		_selectingTarget = true;
		HighlightValidTargets();
	}

	private void ExitTargetSelectMode()
	{
		_selectingTarget = false;
		ClearTargetHighlights();
	}

	private void HighlightValidTargets()
	{
		for (int i = 0; i < _enemySprites.Count; i++)
		{
			if (i < _enemyOrder.Count && _enemyOrder[i].IsAlive)
				_highlighter.SetHighlight(_enemySprites[i], 
					SpriteHighlighter.HighlightType.Target);
		}
	}

	private void ClearTargetHighlights()
	{
		_highlighter.ClearHighlight(SpriteHighlighter.HighlightType.Target);
	}

	// --- Enemy Turn ---

	private async void ProcessEnemyTurn()
	{		
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

		var current = _combatState.CurrentCombatant;
		if (current == null || current.IsParty) return;

		// Pick random living party member
		var targets = new List<Character>();
		foreach (var c in _gameState.Party)
			if (c.IsAlive) targets.Add(c);

		if (targets.Count == 0)
		{
			_combatState.CheckVictoryDefeat();
			UpdateUI();
			return;
		}

		var rng    = new Random();
		var target = targets[rng.Next(targets.Count)];
		var defender = new Combatant
		{
			Character = target,
			IsParty   = true
		};

		int damage = _combatState.ResolveAttack(current, defender);
		
		if (_combatState.IsFrontRowWiped())
		{
			_combatState.AutoPromoteBackRow();
			_partySpritemap.Clear();
			LoadParty();
			HighlightActiveCombatant();
			GetNode<PartyHUD>("/root/PartyHud").Refresh();
		}

		// Fade any unconscious party sprites
		foreach (var c in _gameState.Party)
		{
			if (!c.IsAlive && _partySpritemap.TryGetValue(c, out var pSprite))
			{
				_highlighter.Unregister(pSprite);
				pSprite.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.4f);
			}
		}
		
		// Spawn damage number on the hit party member
		if (_partySpritemap.TryGetValue(target, out var hitSprite))
		{
			SpawnDamageNumber(
				hitSprite.GlobalPosition + hitSprite.Size / 2,
				damage, false);
		}

		RefreshCombatLog();
		RefreshPartySprites();
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
		
		_combatState.CheckVictoryDefeat();
		if (_combatState.CurrentPhase == CombatState.Phase.Victory ||
			_combatState.CurrentPhase == CombatState.Phase.Defeat)
		{
			HandleCombatEnd();
			return;
		}

		_combatState.NextTurn();
		UpdateUI();
	}

	private void RefreshPartySprites()
	{
		var party = _gameState.Party;
		for (int i = 0; i < party.Count; i++)
		{
			if (i >= _partySprites.Count) break;
			if (!party[i].IsAlive)
			{
				_highlighter.Unregister(_partySprites[i]);
				_partySprites[i].Modulate = new Color(0.3f, 0.3f, 0.3f, 0.4f);
			}
		}
	}
		
	private void HandleCombatEnd()
	{
		SetActionButtonsEnabled(false);
		GetNode<PartyHUD>("/root/PartyHud").ClearHighlights();

		// Hide combat UI elements
		_turnTracker.Visible  = false;
		_actionPanel.Visible  = false;

		if (_combatState.CurrentPhase == CombatState.Phase.Victory)
		{
			var loot = LootCalculator.Calculate(
				_combatState,
				_gameState.CurrentEncounterData,
				new Random());

			// Show victory screen as overlay
			var victoryScene = GD.Load<PackedScene>("res://Combat/VictoryScreen.tscn");
			var victory      = victoryScene.Instantiate<VictoryScreen>();
			AddChild(victory);
			victory.Initialize(loot, _combatState);
		}
		else
		{
			// Show defeat screen as overlay
			var defeatScene = GD.Load<PackedScene>("res://Combat/DefeatScreen.tscn");
			var defeat      = defeatScene.Instantiate<DefeatScreen>();
			AddChild(defeat);
			defeat.Initialize();
		}
	}
	
	private void LoadParty()
	{
		foreach (Node child in _partyContainer.GetChildren())
			child.QueueFree();
		_partySprites.Clear();

		var party        = _gameState.Party;
		var containerSize = _partyContainer.Size;
		float spriteSize  = containerSize.X * PartySpriteSize;		
		
		var positions = new List<Vector2>();
		for (int i = 0; i < party.Count; i++)
		{
			bool isFrontRow  = i < 3;
			int rowIndex     = isFrontRow ? i : i - 3;
			float rowX       = isFrontRow
				? containerSize.X * (PartyStartXFrac + PartyRowOffsetFrac)
				: containerSize.X * PartyStartXFrac;
			float x = rowX + rowIndex * containerSize.X * PartyStaggerXFrac;
			float y = containerSize.Y * PartyStartYFrac
					+ rowIndex * containerSize.Y * PartyStaggerYFrac;
			positions.Add(new Vector2(x, y));
		}

		float minY = float.MaxValue, maxY = float.MinValue;
		foreach (var pos in positions)
		{
			if (pos.Y < minY) minY = pos.Y;
			if (pos.Y > maxY) maxY = pos.Y;
		}

		for (int i = 0; i < party.Count; i++)
		{
			var character = party[i];
			var sprite    = CreateSprite(spriteSize);

			if (!string.IsNullOrEmpty(character.BattleSprite)
				&& ResourceLoader.Exists(character.BattleSprite))
				sprite.Texture = GD.Load<Texture2D>(character.BattleSprite);

			float shade     = CalculateShade(positions[i].Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = positions[i];
			sprite.SetMeta("baseShade", shade);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);

			_partyContainer.AddChild(sprite);
			_partySprites.Add(sprite);
			_partySpritemap[character] = sprite;
			_highlighter.Register(sprite, shade);
			
			var capturedCharacter = character;
			var capturedSprite    = sprite;
			sprite.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouse
					&& mouse.Pressed
					&& mouse.ButtonIndex == MouseButton.Left
					&& _selectingRowSwap
					&& _validSwapTargets.Contains(capturedCharacter))
				{
					OnSwapTargetClicked(capturedCharacter);
				}
			};
			sprite.MouseFilter = Control.MouseFilterEnum.Stop;
		}
	}

	private void LoadEnemies()
	{
		foreach (Node child in _enemyContainer.GetChildren())
			child.QueueFree();
		_enemySprites.Clear();
		_enemyOrder.Clear();

		var formation = _gameState.CurrentEncounter;
		if (formation == null || formation.Count == 0) return;

		var containerSize = _enemyContainer.Size;
		float spriteSize  = containerSize.X * EnemySpriteSize;

		// Calculate the vertical range from the party constants
		// so enemy spacing matches party spacing for a row of 3
		float topY    = containerSize.Y * EnemyStartYFrac;
		float bottomY = topY + 2 * containerSize.Y * EnemyStaggerYFrac; // 3 monsters = 2 gaps

		var allPositions = new List<(Vector2 pos, string monsterId)>();

		for (int row = 0; row < formation.Count; row++)
		{
			var rowMonsters = formation[row];
			int count       = rowMonsters.Count;

			// Front row (last in formation) should be leftmost in enemy container
			// Back row (first in formation) should be rightmost
			int reversedRow = formation.Count - 1 - row;
			
			// Keep all rows within container — max offset should not exceed ~0.6
			// With 3 rows at 0.2 spacing: 0.0, 0.2, 0.4 from right edge
			float rowX = containerSize.X * (EnemyStartXFrac - reversedRow * EnemyRowOffsetFrac);

			//GD.Print($"Row {row} (reversed:{reversedRow}): {count} monsters, rowX:{rowX:F1}");

			float rowTopY    = containerSize.Y * EnemyStartYFrac;
			float rowBottomY = topY + 2 * containerSize.Y * EnemyStaggerYFrac;

			for (int i = 0; i < count; i++)
			{
				float t = count == 1 ? 0.5f : (float)i / (count - 1);
				float y = rowTopY + t * (rowBottomY - rowTopY);
				float x = rowX; // all monsters in row share same X (no stagger for now)

				allPositions.Add((new Vector2(x, y), rowMonsters[i]));
			}
		}

		// Sort by Y so front sprites draw on top
		allPositions.Sort((a, b) => a.pos.Y.CompareTo(b.pos.Y));

		float minY = float.MaxValue, maxY = float.MinValue;
		foreach (var (pos, _) in allPositions)
		{
			if (pos.Y < minY) minY = pos.Y;
			if (pos.Y > maxY) maxY = pos.Y;
		}

		foreach (var (pos, monsterId) in allPositions)
		{
			var monster = _combatState.AllMonsters
				.FirstOrDefault(m =>
					string.Equals(m.Id, monsterId, StringComparison.OrdinalIgnoreCase)
					&& !_enemyOrder.Contains(m));
			if (monster == null) continue;

			//GD.Print($"Placing {monster.CombatLabel} at ({pos.X:F1}, {pos.Y:F1}) " +
					 //$"container size: ({containerSize.X:F1}, {containerSize.Y:F1})");
			
			var sprite  = CreateSprite(spriteSize);
			if (!string.IsNullOrEmpty(monster.Sprite)
				&& ResourceLoader.Exists(monster.Sprite))
				sprite.Texture = GD.Load<Texture2D>(monster.Sprite);

			if (sprite.Texture != null)
				_spriteImages[sprite] = sprite.Texture.GetImage();
	
			float shade     = CalculateShade(pos.Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = pos;
			sprite.Size     = new Vector2(spriteSize, spriteSize);			
			sprite.SetMeta("baseShade", shade);

			var capturedMonster = monster;
			var capturedSprite  = sprite;
			sprite.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouse
					&& mouse.ButtonIndex == MouseButton.Left
					&& mouse.Pressed
					&& _selectingTarget
					&& capturedMonster.IsAlive)
				{
					OnEnemyClicked(capturedMonster, capturedSprite);
				}
				if (inputEvent is InputEventMouseButton rightMouse
					&& rightMouse.ButtonIndex == MouseButton.Right
					&& rightMouse.Pressed)
				{
					if (_selectingTarget)
						ExitTargetSelectMode();
					else if (_combatState.CurrentPhase == CombatState.Phase.PlayerTurn
							 && capturedMonster.IsAlive)
					{
						EnterTargetSelectMode();
						OnEnemyClicked(capturedMonster, capturedSprite);
					}
				}
			};

			sprite.MouseFilter = Control.MouseFilterEnum.Stop;
			_enemyContainer.AddChild(sprite);
			_enemySprites.Add(sprite);
			_enemyOrder.Add(monster);
			_highlighter.Register(sprite, shade);
			_monsterSpritemap[monster] = sprite;
		}
	}

	private void OnEnemyClicked(Monster monster, TextureRect sprite)
	{	
		if (!_selectingTarget) return;

		ExitTargetSelectMode();

		var current = _combatState.CurrentCombatant;
		if (current == null || !current.IsParty) return;

		var defender = new Combatant { Monster = monster, IsParty = false };
		int damage   = _combatState.ResolveAttack(current, defender);

		if (_combatState.IsFrontRowWiped())
		{
			_combatState.AutoPromoteBackRow();
			_partySpritemap.Clear();
			LoadParty();
			HighlightActiveCombatant();
			GetNode<PartyHUD>("/root/PartyHud").Refresh();
		}

		SpawnDamageNumber(sprite.GlobalPosition + sprite.Size / 2, damage, false);
		RefreshCombatLog();
		RefreshPartySprites();
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
		
		if (!monster.IsAlive)
		{
			_highlighter.Unregister(sprite);  // stop highlighter managing it
			sprite.Modulate     = new Color(0.3f, 0.3f, 0.3f, 0.5f);
			sprite.MouseFilter  = Control.MouseFilterEnum.Ignore;
		}

		_combatState.CheckVictoryDefeat();
		if (_combatState.CurrentPhase == CombatState.Phase.Victory ||
			_combatState.CurrentPhase == CombatState.Phase.Defeat)
		{
			HandleCombatEnd();
			return;
		}

		_combatState.NextTurn();
		UpdateUI();
	}

	// --- Floating Damage Numbers ---

	private void SpawnDamageNumber(Vector2 position, int amount, bool isHealing)
	{
		var label = new Label();
		label.Text = isHealing ? $"+{amount}" : $"{amount}";  // no minus sign

		// Double the font size
		label.AddThemeFontSizeOverride("font_size", 32);

		label.AddThemeColorOverride("font_color",
			isHealing ? new Color(0, 1, 0) : new Color(1, 0, 0));
		label.Position = position;
		label.ZIndex   = 10;
		AddChild(label);

		Vector2 drift = isHealing
			? new Vector2(-15, -30)   // half speed, floats left
			: new Vector2(15, -30);   // half speed, floats right

		var tween = CreateTween();
		tween.TweenProperty(label, "position",
			position + drift, 3.0f);  // 4x duration
		tween.Parallel().TweenProperty(label, "modulate:a",
			0.0f, 4.0f);              // fade over same 4x duration
		tween.TweenCallback(Callable.From(() => label.QueueFree()));
	}

	// --- Helpers ---

	private TextureRect CreateSprite(float size)
	{
		var sprite = new TextureRect();
		sprite.CustomMinimumSize   = new Vector2(size, size);
		sprite.CustomMaximumSize   = new Vector2(size, size);
		sprite.StretchMode         = TextureRect.StretchModeEnum.KeepAspect;
		sprite.ExpandMode          = TextureRect.ExpandModeEnum.IgnoreSize;
		sprite.SizeFlagsVertical   = Control.SizeFlags.ShrinkBegin;
		sprite.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		sprite.MouseFilter         = Control.MouseFilterEnum.Stop;
		return sprite;
	}
	
	private float CalculateShade(float y, float minY, float maxY)
	{
		if (maxY <= minY) return ShadeMax;
		float t = (y - minY) / (maxY - minY);
		return ShadeMin + t * (ShadeMax - ShadeMin);
	}

	private void AutoFormParty()
	{
		var rng       = new Random();
		var available = new List<Character>(_gameState.Stable);
		int n         = available.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			(available[k], available[n]) = (available[n], available[k]);
		}
		int count = Math.Min(6, available.Count);
		for (int i = 0; i < count; i++)
			_gameState.AddToParty(available[i]);
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse && mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.Right)
			{
				if (_selectingTarget)
				{
					ExitTargetSelectMode();
					GetViewport().SetInputAsHandled();
				}
				else if (_selectingRowSwap)
				{
					ExitRowSwitchMode();
					GetViewport().SetInputAsHandled();
				}
			}
		}

		if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			if (_selectingTarget)
			{
				ExitTargetSelectMode();
				GetViewport().SetInputAsHandled();
			}
			else if (_selectingRowSwap)
			{
				ExitRowSwitchMode();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	private void HighlightActiveCombatant()
	{
		// Clear previous active highlight
		_highlighter.ClearHighlight(SpriteHighlighter.HighlightType.Active);

		var current = _combatState?.CurrentCombatant;
		if (current == null) return;

		TextureRect sprite = GetCombatantSprite(current);
		if (sprite != null)
			_highlighter.SetHighlight(sprite, SpriteHighlighter.HighlightType.Active);
	}

	private void HighlightActiveHudSlot()
	{
		var partyHud = GetNode<PartyHUD>("/root/PartyHud");
		var current  = _combatState?.CurrentCombatant;

		if (current != null && current.IsParty)
			partyHud.HighlightSlot(current.Character);
		else
			partyHud.ClearHighlights();
	}
	
	private bool IsClickOnVisiblePixel(TextureRect sprite, Vector2 localClickPos)
	{
		if (!_spriteImages.TryGetValue(sprite, out var image)) return true;

		var textureSize = new Vector2(image.GetWidth(), image.GetHeight());
		var spriteSize  = sprite.Size;

		float scaleX = textureSize.X / spriteSize.X;
		float scaleY = textureSize.Y / spriteSize.Y;
		float scale  = Mathf.Max(scaleX, scaleY);

		int tx = Mathf.Clamp((int)(localClickPos.X * scale), 0, image.GetWidth() - 1);
		int ty = Mathf.Clamp((int)(localClickPos.Y * scale), 0, image.GetHeight() - 1);

		return image.GetPixel(tx, ty).A > 0.1f;
	}	

	private Dictionary<TextureRect, Tween> _spriteGlowTweens = new Dictionary<TextureRect, Tween>();

	private void StartSpriteGlow(TextureRect sprite, Color? glowColor = null)
	{
		// Kill existing glow on this sprite only
		if (_spriteGlowTweens.TryGetValue(sprite, out var existing))
		{
			existing?.Kill();
			_spriteGlowTweens.Remove(sprite);
		}

		float baseShade = (float)sprite.GetMeta("baseShade", ShadeMax);
		Color color = glowColor ?? new Color(baseShade * 0.6f, baseShade, baseShade * 0.6f);

		var tween = CreateTween();
		tween.SetLoops();
		tween.TweenProperty(sprite, "modulate", color, 0.5f);
		tween.TweenProperty(sprite, "modulate",
			new Color(baseShade, baseShade, baseShade, 1.0f), 0.5f);
		_spriteGlowTweens[sprite] = tween;

		// Reapply active highlight AFTER starting glow so it's not overwritten
		HighlightActiveCombatant();
	}
	
	private void StopAllSpriteGlows()
	{
		foreach (var kvp in _spriteGlowTweens)
		{
			kvp.Value?.Kill();
			if (kvp.Key != null && kvp.Key.HasMeta("baseShade"))
			{
				float baseShade = (float)kvp.Key.GetMeta("baseShade");
				kvp.Key.Modulate = new Color(baseShade, baseShade, baseShade, 1.0f);
			}
		}
		_spriteGlowTweens.Clear();
		HighlightActiveCombatant();
		RefreshPartySprites();
	}
		
	private void OnTrackerSlotHovered(int turnOrderIndex)
	{
		if (turnOrderIndex < 0 || turnOrderIndex >= _combatState.TurnOrder.Count)
			return;

		var combatant = _combatState.TurnOrder[turnOrderIndex];
		var sprite = GetCombatantSprite(combatant);
		if (sprite != null)
			_highlighter.SetHighlight(sprite, SpriteHighlighter.HighlightType.Hover);
	}

	private void OnTrackerSlotUnhovered()
	{
		_highlighter.ClearHighlight(SpriteHighlighter.HighlightType.Hover);
	}

	private TextureRect GetCombatantSprite(Combatant combatant)
	{
		if (combatant.IsParty)
		{
			if (_partySpritemap.TryGetValue(combatant.Character, out var sprite))
				return sprite;
		}
		else
		{
			if (_monsterSpritemap.TryGetValue(combatant.Monster, out var sprite))
				return sprite;
		}
		return null;
	}
	
	private void OnSwapTargetClicked(Character target)
	{
		if (!_selectingRowSwap) return;

		var current = _combatState.CurrentCombatant;
		if (current == null || !current.IsParty) return;

		ExitRowSwitchMode();

		// Perform the swap
		_combatState.SwapRows(current.Character, target);

		// Rebuild party sprites to reflect new positions
		_partySpritemap.Clear();
		LoadParty();
		HighlightActiveCombatant();

		RefreshCombatLog();
		GetNode<PartyHUD>("/root/PartyHud").Refresh();

		// Advance turn
		_combatState.NextTurn();
		UpdateUI();
	}
	
	private void BuildDebugButtons()
	{
		if (!DebugFlags.ShowCombatDebugButtons) return;

		var vbox = GetNode<VBoxContainer>("ActionPanel/VBoxContainer");

		var separator = new HSeparator();
		vbox.AddChild(separator);

		_autoWinButton = new Button();
		_autoWinButton.Text     = "[DEBUG] Auto Win";
		_autoWinButton.Pressed += OnAutoWinPressed;
		vbox.AddChild(_autoWinButton);

		_autoLoseButton = new Button();
		_autoLoseButton.Text     = "[DEBUG] Auto Lose";
		_autoLoseButton.Pressed += OnAutoLosePressed;
		vbox.AddChild(_autoLoseButton);
	}
	
	private void OnAutoWinPressed()
	{
		// Kill all monsters
		foreach (var monster in _combatState.AllMonsters)
		{
			monster.CurrentHP = 0;
			int index = _enemyOrder.IndexOf(monster);
			if (index >= 0 && index < _enemySprites.Count)
			{
				_highlighter.Unregister(_enemySprites[index]);
				_enemySprites[index].Modulate    = new Color(0.3f, 0.3f, 0.3f, 0.4f);
				_enemySprites[index].MouseFilter = Control.MouseFilterEnum.Ignore;
			}
		}

		_combatState.AddLog("--- [DEBUG] Auto Win ---");
		_combatState.CheckVictoryDefeat();
		RefreshCombatLog();
		HandleCombatEnd();
	}

	private void OnAutoLosePressed()
	{
		// Knock out all party members
		foreach (var character in _gameState.Party)
		{
			character.CurrentHP = 0;
			character.Status    = Status.Asleep;
			if (_partySpritemap.TryGetValue(character, out var sprite))
			{
				_highlighter.Unregister(sprite);
				sprite.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.4f);
			}
		}

		_combatState.AddLog("--- [DEBUG] Auto Lose ---");
		_combatState.CheckVictoryDefeat();
		RefreshCombatLog();
		GetNode<PartyHUD>("/root/PartyHud").Refresh();
		HandleCombatEnd();
	}
	
	private void OnFleePressed()
	{
		//TODO: Custom flee combat confirmation panel
		if (_combatState.CurrentPhase != CombatState.Phase.PlayerTurn) return;
		ShowFleeConfirmation();
	}

	private void ShowFleeConfirmation()
	{
		var dialog = new ConfirmationDialog();
		dialog.Title       = "Flee?";
		dialog.DialogText  = "Are you sure you want to flee?\n" +
							 "The enemy will get a chance to strike as you run.";
		dialog.OkButtonText     = "Flee";
		dialog.CancelButtonText = "Cancel";

		AddChild(dialog);

		dialog.Confirmed += () =>
		{
			dialog.QueueFree();
			ExecuteFlee();  // the async sequence
		};
		dialog.Canceled += () =>
		{
			dialog.QueueFree();
		};

		// Center and show
		dialog.PopupCentered();
	}

	private async void ExecuteFlee()
	{
		SetActionButtonsEnabled(false);
		var rolls = _combatState.RollFleeInitiative();
		RefreshCombatLog();

		// Track which characters have already fled (faded)
		var fled = new HashSet<Character>();

		// Helper — fade any living character whose flee init beats all REMAINING monsters
		async System.Threading.Tasks.Task FadeEscapees(List<Monster> remainingMonsters)
		{
			var newlyFled = new List<Character>();
			foreach (var c in _gameState.Party)
			{
				if (!c.IsAlive || fled.Contains(c)) continue;

				int cInit = rolls.PartyInit[c];
				bool caught = false;
				foreach (var m in remainingMonsters)
				{
					if (!m.IsAlive) continue;
					if (rolls.EnemyInit[m] >= cInit) { caught = true; break; }
				}

				if (!caught)
				{
					newlyFled.Add(c);
					fled.Add(c);
				}
			}

			// Fade them over 1.5s (in parallel)
			foreach (var c in newlyFled)
			{
				if (_partySpritemap.TryGetValue(c, out var sprite))
				{
					_highlighter.Unregister(sprite);
					var tween = CreateTween();
					tween.TweenProperty(sprite, "modulate:a", 0.0f, 1.5f);
					_combatState.AddLog($"{c.Name} escapes!");
				}
			}

			if (newlyFled.Count > 0)
			{
				RefreshCombatLog();
				await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
			}
		}

		// Build the list of monsters in flee-initiative order (highest first)
		var orderedMonsters = new List<Monster>();
		foreach (var m in _combatState.AllMonsters)
			if (m.IsAlive) orderedMonsters.Add(m);
		orderedMonsters.Sort((a, b) => rolls.EnemyInit[b].CompareTo(rolls.EnemyInit[a]));

		// STEP 1 — fade anyone faster than every monster, before any attacks
		await FadeEscapees(orderedMonsters);

		// Process each monster in initiative order
		for (int i = 0; i < orderedMonsters.Count; i++)
		{
			var monster = orderedMonsters[i];
			if (!monster.IsAlive) continue;

			// This monster attacks a random NON-FLED hero with init <= its own
			int mInit = rolls.EnemyInit[monster];
			var validTargets = new List<Character>();
			foreach (var c in _gameState.Party)
			{
				if (!c.IsAlive || fled.Contains(c)) continue;
				if (rolls.PartyInit[c] <= mInit)
					validTargets.Add(c);
			}

			if (validTargets.Count > 0)
			{
				var target   = validTargets[new Random().Next(validTargets.Count)];
				var attacker = new Combatant { Monster = monster, IsParty = false };
				var defender = new Combatant { Character = target, IsParty = true };

				int damage = _combatState.ResolveAttack(attacker, defender);
				RefreshCombatLog();
				GetNode<PartyHUD>("/root/PartyHud").Refresh();

				if (_partySpritemap.TryGetValue(target, out var tSprite))
					SpawnDamageNumber(tSprite.GlobalPosition + tSprite.Size / 2, damage, false);

				// Fade if knocked out
				if (!target.IsAlive && _partySpritemap.TryGetValue(target, out var koSprite))
				{
					_highlighter.Unregister(koSprite);
					koSprite.Modulate = new Color(0.3f, 0.3f, 0.3f, 0.4f);
					fled.Add(target); // remove from further targeting
				}

				// brief beat so the player can read each hit
				await ToSignal(GetTree().CreateTimer(0.8f), "timeout");
			}

			// After this monster acts, anyone now faster than all REMAINING monsters escapes
			var remaining = orderedMonsters.GetRange(i + 1, orderedMonsters.Count - (i + 1));
			await FadeEscapees(remaining);

			// If the party got wiped, jump to defeat
			if (_combatState.IsDefeat)
			{
				_combatState.CheckVictoryDefeat();
				HandleCombatEnd();
				return;
			}
		}

		// All hits resolved — wait for the floating numbers to breathe
		await ToSignal(GetTree().CreateTimer(2.0f), "timeout");

		ShowFleeScreen();
	}

	private void ShowFleeScreen()
	{
		_turnTracker.Visible = false;
		_actionPanel.Visible = false;

		var fleeScene = GD.Load<PackedScene>("res://Combat/FleeScreen.tscn");
		var flee      = fleeScene.Instantiate();
		AddChild(flee);
		(flee as FleeScreen)?.Initialize();
	}
	
	private void FleeToExploredRoom()
	{
		SetActionButtonsEnabled(false);
		_turnTracker.Visible = false;
		_actionPanel.Visible = false;

		var dungeon = _gameState.CurrentDungeon;
		var state   = _gameState.GetDungeonState(dungeon);

		var explored = new List<string>(state.ExploredRooms);

		// Prefer a room other than the current one
		explored.Remove(_gameState.CurrentRoom?.Id);

		string targetRoom = null;
		if (explored.Count > 0)
			targetRoom = explored[new Random().Next(explored.Count)];
		else
			targetRoom = _gameState.CurrentRoom?.Id; // nowhere else to go

		if (targetRoom != null)
			_gameState.CurrentRoom = DungeonManager.LoadRoom(dungeon, targetRoom);

		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Dungeons/Dungeon.tscn");
	}
}
