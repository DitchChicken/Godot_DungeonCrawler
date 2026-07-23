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
	private Button _spellSkillButton;  //Note:  We should rename this but I'm lazy
	private Button _itemButton;
	private Button _rowSwitchButton;
	private Button _fleeButton;
	private HBoxContainer _turnStrip;
	private PanelContainer _actionPanel;
	private Button _autoWinButton;
	private Button _autoLoseButton;

	//Ability Menu
	private AbilityMenu _abilityMenu;
	private Ability _pendingAbility;        // ability chosen, awaiting target
	private bool _selectingAllyTarget = false;
	private List<Character> _validAllyTargets = new List<Character>();
	
	//Item menu
	private ItemMenu _itemMenu;
	private Equipment _pendingItem;

	// Combat state
	private bool _selectingTarget = false;
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

	private List<Control> _enemySprites = new List<Control>();
	private List<Control> _partySprites = new List<Control>();
	private Dictionary<Monster, Control>   _monsterSpritemap = new Dictionary<Monster, Control>();
	private Dictionary<Character, Control> _partySpritemap   = new Dictionary<Character, Control>();
	private Dictionary<Control, StatusIconRow> _statusRows = new Dictionary<Control, StatusIconRow>();
	
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
		
		//Ability Menu
		_abilityMenu = new AbilityMenu();
		_abilityMenu.Visible = false;
		_abilityMenu.ZIndex  = 20;
		AddChild(_abilityMenu);
		_abilityMenu.AbilitySelected += OnAbilitySelected;
		_abilityMenu.Cancelled       += OnAbilityMenuCancelled;

		//Item Menu
		_itemMenu = new ItemMenu();
		_itemMenu.Visible = false;
		_itemMenu.ZIndex  = 20;
		AddChild(_itemMenu);
		_itemMenu.ItemSelected += OnItemSelected;
		_itemMenu.Cancelled    += OnItemMenuCancelled;
		
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
		
		//Apply status effects to an enemy for testing
		if (DebugFlags.ApplyTestStatuses)
		{
			var testMonster = _combatState.AllMonsters.FirstOrDefault();
			if (testMonster != null)
			{
				testMonster.AddStatusEffect(new StatusEffect(StatusType.Poisoned, 5, 3));
				testMonster.AddStatusEffect(new StatusEffect(StatusType.Asleep, 3));
				testMonster.AddStatusEffect(new StatusEffect(StatusType.Paralyzed, 3));
				GD.Print($"Applied test statuses to {testMonster.CombatLabel}");
			}

			var testHero = _gameState.Party.FirstOrDefault(c => c.IsAlive);
			if (testHero != null)
			{
				testHero.AddStatusEffect(new StatusEffect(StatusType.Poisoned, 5, 3));
				testHero.AddStatusEffect(new StatusEffect(StatusType.Asleep, 3));
				testHero.AddStatusEffect(new StatusEffect(StatusType.Paralyzed, 3));
				GD.Print($"Applied test statuses to {testHero.Name}");
			}

			RefreshStatusIcons();
			GetNode<PartyHUD>("/root/PartyHud").Refresh();
		}
		
		UpdateUI();
	}

	private void InitializeCombat()
	{
		var instance = _gameState.CurrentEncounterInstance;
		if (instance == null) { GD.PrintErr("No encounter instance!"); return; }

		// Use the LIVE formation — damage and deaths persist
		_combatState = new CombatState(_gameState.Party, instance.Formation);
		_combatState.RollInitiative();
		
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
		RefreshStatusIcons();

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

	private void RefreshCombatLog()
	{
		_logLabel.Text = string.Join("\n", _combatState.Log);
		// Defer scroll so the container recalculates content height first
		CallDeferred(nameof(ScrollLogToBottom));
	}

	private async void ScrollLogToBottom()
	{
		// Wait for the label to resize and the scrollbar to update
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var vbar = _scrollContainer.GetVScrollBar();
		_scrollContainer.ScrollVertical = (int)vbar.MaxValue;
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
			if (_combatState.CurrentPhase != CombatState.Phase.PlayerTurn) return;
			var current = _combatState.CurrentCombatant;
			if (current == null || !current.IsParty) return;

			_abilityMenu.Open(current.Character);
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
		RefreshStatusIcons();
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

		var instance = _gameState.CurrentEncounterInstance;
		var state    = _gameState.GetDungeonState(_gameState.CurrentDungeon);
		string roomId = _gameState.CurrentRoom?.Id ?? "";

		state.Encounters.ResolveAfterCombat(instance, roomId);
		_gameState.CurrentEncounterInstance = null;
		
		StatusProcessor.ClearCombatEffects(_gameState.Party);

		//Advance clock		
		if (!string.IsNullOrEmpty(_gameState.CurrentDungeon))
			DungeonClock.Advance(_gameState, _combatState.CurrentRound, "combat");

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
		// Unregister old party sprites from the highlighter before freeing them
		foreach (var sprite in _partySprites)
		{
			_highlighter.Unregister(sprite);
			_statusRows.Remove(sprite);
		}

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

			SetSpriteTexture(sprite, character.BattleSprite);

			float shade     = CalculateShade(positions[i].Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = positions[i];
			sprite.SetMeta("baseShade", shade);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);

			_partyContainer.AddChild(sprite);
			_partySprites.Add(sprite);
			_partySpritemap[character] = sprite;
			_highlighter.Register(sprite, shade);
			AttachStatusRow(sprite, character.SpriteTopOffset, character.SpriteRightOffset);

			var capturedCharacter = character;
			var capturedSprite    = sprite;
			sprite.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouse
					&& mouse.Pressed
					&& mouse.ButtonIndex == MouseButton.Left)
				{
					GD.Print($"Party sprite clicked: {capturedCharacter.Name}, " +
							 $"allyMode:{_selectingAllyTarget}, " +
							 $"inList:{_validAllyTargets.Contains(capturedCharacter)}");

					if (_selectingRowSwap && _validSwapTargets.Contains(capturedCharacter))
						OnSwapTargetClicked(capturedCharacter);
					else if (_selectingAllyTarget && _validAllyTargets.Contains(capturedCharacter))
						OnAllyTargetClicked(capturedCharacter);
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

		// Use the live instance formation (List<List<Monster>>), not the old ID list
		var formation = _combatState?.EnemyFormation;
		if (formation == null || formation.Count == 0) return;

		var containerSize = _enemyContainer.Size;
		float spriteSize  = containerSize.X * EnemySpriteSize;

		float topY    = containerSize.Y * EnemyStartYFrac;
		float bottomY = topY + 2 * containerSize.Y * EnemyStaggerYFrac;

		// Now carrying Monster references directly instead of string ids
		var allPositions = new List<(Vector2 pos, Monster monster)>();

		for (int row = 0; row < formation.Count; row++)
		{
			var rowMonsters = formation[row];
			int count       = rowMonsters.Count;
			int reversedRow = formation.Count - 1 - row;
			float rowX      = containerSize.X * (EnemyStartXFrac - reversedRow * EnemyRowOffsetFrac);

			for (int i = 0; i < count; i++)
			{
				float t = count == 1 ? 0.5f : (float)i / (count - 1);
				float y = topY + t * (bottomY - topY);
				allPositions.Add((new Vector2(rowX, y), rowMonsters[i]));
			}
		}

		allPositions.Sort((a, b) => a.pos.Y.CompareTo(b.pos.Y));

		float minY = float.MaxValue, maxY = float.MinValue;
		foreach (var (pos, _) in allPositions)
		{
			if (pos.Y < minY) minY = pos.Y;
			if (pos.Y > maxY) maxY = pos.Y;
		}

		foreach (var (pos, monster) in allPositions)
		{
			if (monster == null) continue;

			var sprite = CreateSprite(spriteSize);
			SetSpriteTexture(sprite, monster.Sprite);

			float shade     = CalculateShade(pos.Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = pos;
			sprite.Size     = new Vector2(spriteSize, spriteSize);
			sprite.SetMeta("baseShade", shade);

			var capturedMonster = monster;
			var capturedSprite  = sprite;

			sprite.GuiInput += (inputEvent) =>
			{
				if (inputEvent is not InputEventMouseButton mouse || !mouse.Pressed)
					return;

				if (mouse.ButtonIndex == MouseButton.Left
					&& _selectingTarget
					&& capturedMonster.IsAlive)
				{
					OnEnemyClicked(capturedMonster, capturedSprite);
				}
				else if (mouse.ButtonIndex == MouseButton.Right)
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

			_enemyContainer.AddChild(sprite);
			_enemySprites.Add(sprite);
			_enemyOrder.Add(monster);
			_highlighter.Register(sprite, shade);
			_monsterSpritemap[monster] = sprite;
			AttachStatusRow(sprite, monster.SpriteTopOffset, monster.SpriteRightOffset);
		}
	}

	private void OnEnemyClicked(Monster monster, Control sprite)
	{
		if (!_selectingTarget) return;
		ExitTargetSelectMode();

		// If we're using an item/ability on an enemy, route there
		if (_pendingItem != null || _pendingAbility != null)
		{
			var target = new Combatant { Monster = monster, IsParty = false };
			if (_pendingItem != null)
				ResolveItemOnTarget(target);
			else
				ResolveAbilityOnTarget(target);
			return;
		}

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
		RefreshStatusIcons();
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

	private TextureButton CreateSprite(float size)
	{
		var btn = new TextureButton();
		btn.IgnoreTextureSize = true;
		btn.StretchMode       = TextureButton.StretchModeEnum.KeepAspectCentered;
		btn.MouseFilter       = Control.MouseFilterEnum.Stop;
		btn.CustomMinimumSize = new Vector2(size, size);
		return btn;
	}
	
	// Sets the displayed texture AND builds a click mask from its alpha,
	// so only opaque pixels are clickable.
	private void SetSpriteTexture(TextureButton btn, string path)
	{
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;

		var texture = GD.Load<Texture2D>(path);
		btn.TextureNormal = texture;

		var image  = texture.GetImage();
		var bitmap = new Bitmap();
		bitmap.CreateFromImageAlpha(image, 0.1f); // alpha < 0.1 = not clickable
		btn.TextureClickMask = bitmap;
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
				else if (_selectingAllyTarget)
				{
					ExitAllyTargetMode();
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
			else if (_selectingAllyTarget)
			{
				ExitAllyTargetMode();
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

		Control sprite = GetCombatantSprite(current);
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

	private Control GetCombatantSprite(Combatant combatant)
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
			character.Status    = Status.Dead;  // was Status.Asleep
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

		// TODO: flee penalty is a flat 10 ticks for now — revisit once the clock
		// drives wandering monsters, so a botched escape costs meaningful time.
		DungeonClock.Advance(_gameState, _combatState.CurrentRound + 10, "flee");
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
	
	private const float SourceImageSize = 1024f;
	
	private void AttachStatusRow(Control sprite, float topOffsetPx, float rightOffsetPx)
	{
		var row = new StatusIconRow();
		row.MouseFilter  = Control.MouseFilterEnum.Ignore;
		row.ZIndex       = 5;

		// Use the sprite's ACTUAL on-screen size, not the passed parameter
		float scale = sprite.Size.X / 1024f;

		//GD.Print($"Scale: {scale}");
		//GD.Print($"rightOffsetPx: {rightOffsetPx}");
		//GD.Print($"topOffsetPx: {topOffsetPx}");
		float xScreen = rightOffsetPx * scale;
		float yScreen = topOffsetPx   * scale;
		//GD.Print($"xScreen: {xScreen}");
		//GD.Print($"yScreen: {yScreen}");
				
		// Straight proportional offset from the image's top-left corner
		row.Position = new Vector2(
			sprite.Position.X + xScreen,
			sprite.Position.Y + yScreen);
		//GD.Print($"sprite.Position.X : {sprite.Position.X}, sprite.Position.Y: {sprite.Position.Y}");
		//GD.Print($"row.Position.X: {row.Position.X}, row.Position.Y: {row.Position.Y}");

		sprite.GetParent().AddChild(row);
		_statusRows[sprite] = row;
	}
	
	private void RefreshStatusIcons()
	{
		// Party
		foreach (var character in _gameState.Party)
		{
			if (_partySpritemap.TryGetValue(character, out var sprite)
				&& _statusRows.TryGetValue(sprite, out var row))
			{
				row.Refresh(character.ActiveEffects);
			}
		}

		// Enemies
		foreach (var monster in _combatState.AllMonsters)
		{
			if (_monsterSpritemap.TryGetValue(monster, out var sprite)
				&& _statusRows.TryGetValue(sprite, out var row))
			{
				row.Refresh(monster.ActiveEffects);
			}
		}
	}
	
	private void OnAbilityMenuCancelled()
	{
		_abilityMenu.Visible = false;
	}

	private void OnAbilitySelected(string abilityId)
	{
		_abilityMenu.Visible = false;
		_abilityMenu.MouseFilter = Control.MouseFilterEnum.Ignore;  // ensure it can't catch input

		GD.Print($"Ability selected: {abilityId}");

		var ability = AbilityLoader.LoadAbility(abilityId);
		if (ability == null) { GD.Print("  ability null!"); return; }

		_pendingAbility = ability;
		GD.Print($"  targetType: {ability.TargetType}");

		switch (ability.TargetType)
		{
			case AbilityTargetType.SingleAlly:
				EnterAllyTargetMode(ability);
				break;
			case AbilityTargetType.Self:
				ResolveAbilityOnTarget(_combatState.CurrentCombatant);
				break;
			default:
				GD.Print($"  target type {ability.TargetType} not handled");
				break;
		}
	}
	
	private void EnterAllyTargetMode(Ability ability)
	{
		_validAllyTargets = new List<Character>();
		foreach (var c in _gameState.Party)
			if (c.IsAlive) _validAllyTargets.Add(c);

		GD.Print($"EnterAllyTargetMode: {_validAllyTargets.Count} valid targets");

		if (_validAllyTargets.Count == 0)
		{
			AddLog("No valid targets.");
			_pendingAbility = null;
			return;
		}

		_selectingAllyTarget = true;

		foreach (var c in _validAllyTargets)
		{
			if (_partySpritemap.TryGetValue(c, out var sprite))
				_highlighter.SetHighlight(sprite, SpriteHighlighter.HighlightType.Ally);
		}
		GD.Print($"_selectingAllyTarget = {_selectingAllyTarget}");
	}

	private void ExitAllyTargetMode()
	{
		_selectingAllyTarget = false;
		_validAllyTargets.Clear();
		_highlighter.ClearHighlight(SpriteHighlighter.HighlightType.Ally);
		_pendingAbility = null;
	}
	
	private void OnAllyTargetClicked(Character target)
	{
		if (!_selectingAllyTarget) return;
		var targetCombatant = new Combatant { Character = target, IsParty = true };
		ExitAllyTargetMode();

		if (_pendingItem != null)
			ResolveItemOnTarget(targetCombatant);
		else
			ResolveAbilityOnTarget(targetCombatant, _pendingAbility);
	}
	
	private void ResolveAbilityOnTarget(Combatant target, Ability ability = null)
	{
		ability ??= _pendingAbility;
		if (ability == null) return;

		var caster = _combatState.CurrentCombatant;
		if (caster == null) return;

		int amount = _combatState.ResolveAbility(caster, ability, target);

		// Floating number — green heal on the target
		if (ability.EffectType == AbilityEffectType.Heal
			&& target.IsParty
			&& _partySpritemap.TryGetValue(target.Character, out var sprite))
		{
			SpawnDamageNumber(sprite.GlobalPosition + sprite.Size / 2, amount, true);
		}

		_pendingAbility = null;

		RefreshCombatLog();
		RefreshStatusIcons();
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
	
	private void ResolveItemOnTarget(Combatant target)
	{
		var caster  = _combatState.CurrentCombatant;
		var ability = _pendingAbility;
		var item    = _pendingItem;
		if (caster == null || ability == null || item == null) return;

		// Resolve the ability's effect (no mana cost for items)
		int amount = _combatState.ResolveAbility(caster, ability, target, freeCost: true);

		// Floating number
		if (ability.EffectType == AbilityEffectType.Heal
			&& target.IsParty
			&& _partySpritemap.TryGetValue(target.Character, out var sprite))
		{
			SpawnDamageNumber(sprite.GlobalPosition + sprite.Size / 2, amount, true);
		}

		// Consume one from the stack
		caster.Character.PersonalInventory.RemoveItem(item, 1);
		AddLog($"{caster.Name} uses {item.Name}.");

		_pendingItem    = null;
		_pendingAbility = null;

		RefreshCombatLog();
		RefreshStatusIcons();
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

	private void OnItemPressed()
	{
		if (_combatState.CurrentPhase != CombatState.Phase.PlayerTurn) return;
		var current = _combatState.CurrentCombatant;
		if (current == null || !current.IsParty) return;
		_itemMenu.Open(current.Character);
	}

	private void OnItemMenuCancelled()
	{
		_itemMenu.Visible = false;
	}

	private void OnItemSelected(string itemId)
	{
		_itemMenu.Visible = false;

		var current = _combatState.CurrentCombatant;
		if (current == null || !current.IsParty) return;

		// Find the item in the user's inventory
		Equipment item = null;
		foreach (var i in current.Character.PersonalInventory.Items)
			if (i.Id == itemId) { item = i; break; }
		if (item == null) return;

		var ability = AbilityLoader.LoadAbility(item.UseAbility);
		if (ability == null) return;

		_pendingItem    = item;
		_pendingAbility = ability;

		// Determine targeting. Potions collapse ally-targeting to self.
		bool isPotion = item.ConsumableType == "Potion";

		switch (ability.TargetType)
		{
			case AbilityTargetType.SingleAlly:
			case AbilityTargetType.AllAllies:
			case AbilityTargetType.Self:
				if (isPotion)
				{
					// Potion: only the acting PC (drink it)
					ResolveItemOnTarget(current);
				}
				else
				{
					// Scroll/Liturgy: normal ally targeting
					EnterAllyTargetMode(ability);
				}
				break;

			case AbilityTargetType.SingleEnemy:
				// Thrown potion or offensive scroll — enemy targeting
				EnterTargetSelectMode(); // existing enemy select; see note below
				break;

			default:
				GD.Print($"Item target type {ability.TargetType} not yet handled");
				break;
		}
	}
}
