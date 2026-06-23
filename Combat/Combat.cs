using Godot;
using System;
using System.Collections.Generic;

public partial class Combat : Control
{
	private Control _partyContainer;
	private Control _enemyContainer;
	private GameState _gameState;

	// Party formation constants (as fraction of container)
	private const float PartyStartXFrac   = 0.1f;   // 10% from left
	private const float PartyStartYFrac   = -0.10f;   // 10% from top
	private const float PartyStaggerXFrac = 0.080f;  // 8% per slot
	private const float PartyStaggerYFrac  = 0.272f;  // 15% per slot
	private const float PartyRowOffsetFrac = 0.35f; // 25% between rows
	private const float PartySpriteSize   = 0.495f;  // 35% of container width

	private const float EnemyStartXFrac    = 0.15f;  // start near right edge
	private const float EnemyStartYFrac    = -0.10f; // match party top
	private const float EnemyStaggerXFrac  = 0.04f;  // match party
	private const float EnemyStaggerYFrac  = 0.275f; // match party
	private const float EnemyRowOffsetFrac = 0.15f;  // match party
	private const float EnemySpriteSize    = 0.495f;  // slightly smaller than party

	// Shading
	private const float ShadeMin = 0.725f;  // darkest (back)
	private const float ShadeMax = 1.00f;  // brightest (front)

	public override void _Ready()
	{
		_partyContainer = GetNode<Control>("PartyContainer");
		_enemyContainer = GetNode<Control>("EnemyContainer");
		_gameState      = GetNode<GameState>("/root/GameState");

		if (DebugFlags.AutoFormPartyOnEmbark && _gameState.Party.Count == 0)
			AutoFormParty();

		CallDeferred(nameof(LoadCombatants));
	}

	private void LoadCombatants()
	{
		LoadParty();
		LoadEnemies();
	}

	private float CalculateShade(float y, float minY, float maxY)
	{
		if (maxY <= minY) return ShadeMax;
		// Higher Y = closer to front = brighter
		float t = (y - minY) / (maxY - minY);
		return ShadeMin + t * (ShadeMax - ShadeMin);
	}

	private void LoadParty()
	{
		foreach (Node child in _partyContainer.GetChildren())
			child.QueueFree();

		var party        = _gameState.Party;
		var containerSize = _partyContainer.Size;
		float spriteSize  = containerSize.X * PartySpriteSize;

		// Calculate all positions first so we can derive min/max Y for shading
		var positions = new List<Vector2>();

		for (int i = 0; i < party.Count; i++)
		{
			bool isFrontRow = i < 3;
			int rowIndex    = isFrontRow ? i : i - 3;

			float rowX = isFrontRow
				? containerSize.X * (PartyStartXFrac + PartyRowOffsetFrac)
				: containerSize.X * PartyStartXFrac;

			float x = rowX + rowIndex * containerSize.X * PartyStaggerXFrac;
			float y = containerSize.Y * PartyStartYFrac
					+ rowIndex * containerSize.Y * PartyStaggerYFrac;

			positions.Add(new Vector2(x, y));
		}

		// Get Y range for shading
		float minY = float.MaxValue;
		float maxY = float.MinValue;
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

			float shade    = CalculateShade(positions[i].Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = positions[i];

			_partyContainer.AddChild(sprite);
		}
	}

	private void LoadEnemies()
	{
		foreach (Node child in _enemyContainer.GetChildren())
			child.QueueFree();

		var formation = _gameState.CurrentEncounter;
		if (formation == null || formation.Count == 0)
		{
			GD.PrintErr("No encounter data");
			return;
		}

		var containerSize = _enemyContainer.Size;
		float spriteSize  = containerSize.X * EnemySpriteSize;

		// Calculate all positions first
		var allPositions = new List<(Vector2 pos, string monsterId)>();

		for (int row = 0; row < formation.Count; row++)
		{
			var rowMonsters = formation[row];
			int count       = rowMonsters.Count;

			// Mirror party logic — back row starts further right, front row to the left
			// Party goes left=back, right=front
			// Enemies go right=back, left=front (mirrored)
			int reversedRow = formation.Count - 1 - row;

			float rowX = containerSize.X * (EnemyStartXFrac + reversedRow * EnemyRowOffsetFrac);
			float rowStartY = containerSize.Y * EnemyStartYFrac;

			for (int i = 0; i < count; i++)
			{
				float x = rowX - i * containerSize.X * EnemyStaggerXFrac;
				float y = rowStartY + i * containerSize.Y * EnemyStaggerYFrac;

				allPositions.Add((new Vector2(x, y), rowMonsters[i]));
			}
		}

		// Get Y range for shading
		float minY = float.MaxValue;
		float maxY = float.MinValue;
		foreach (var (pos, _) in allPositions)
		{
			if (pos.Y < minY) minY = pos.Y;
			if (pos.Y > maxY) maxY = pos.Y;
		}

		allPositions.Sort((a, b) => a.pos.Y.CompareTo(b.pos.Y));

		foreach (var (pos, monsterId) in allPositions)
		{
			var monster = MonsterLoader.LoadMonster(monsterId);
			if (monster == null) continue;

			var sprite = CreateSprite(spriteSize);

			if (!string.IsNullOrEmpty(monster.Sprite)
				&& ResourceLoader.Exists(monster.Sprite))
				sprite.Texture = GD.Load<Texture2D>(monster.Sprite);

			float shade     = CalculateShade(pos.Y, minY, maxY);
			sprite.Modulate = new Color(shade, shade, shade, 1.0f);
			sprite.Position = pos;

			_enemyContainer.AddChild(sprite);
		}
	}

	private TextureRect CreateSprite(float size)
	{
		var sprite = new TextureRect();
		sprite.CustomMinimumSize   = new Vector2(size, size);
		sprite.CustomMaximumSize   = new Vector2(size, size);
		sprite.StretchMode         = TextureRect.StretchModeEnum.KeepAspect;
		sprite.ExpandMode          = TextureRect.ExpandModeEnum.IgnoreSize;
		sprite.SizeFlagsVertical   = Control.SizeFlags.ShrinkBegin;
		sprite.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		return sprite;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key
			&& key.Pressed
			&& key.Keycode == Key.F1
			&& DebugFlags.AutoFormPartyOnEmbark)
		{
			foreach (Node child in _partyContainer.GetChildren())
				child.QueueFree();
			foreach (Node child in _enemyContainer.GetChildren())
				child.QueueFree();
			LoadParty();
			LoadEnemies();
		}
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
}
