using Godot;
using System.Collections.Generic;


public partial class Combat : Control
{
	private Control _partyContainer;
	private Control _enemyContainer;
	private GameState  _gameState;

	// Single source of truth for sizing and layout
	private const float SpriteSize    = 512f;
	private const float StaggerX      =  70f;   // horizontal offset per slot
	private const float StaggerY      = 235f;   // vertical offset per slot
	private const float RowOffset     = 320f;   // back row to front row distance
	private const float StartX        = 150f;   // left edge start
	private const float StartY        = 20f;    // top edge start

	// Depth shading - front row is brightest
	private const float ShadeRow1     = 0.70f;  // back, top slot - 30% darker
	private const float ShadeRow2     = 0.85f;  // back, middle slot - 15% darker
	private const float ShadeRow3     = 1.00f;  // front, brightest

	// Enemy formation - right side of screen
	private const float EnemySpriteSize  = 512;
	private const float EnemyStartX      = 0f;     // left edge of enemy area
	private const float EnemyStartY      = 20f;    // top of formation
	private const float EnemyRowOffset   = 320f;   // vertical distance between rows
	private const float EnemyMaxWidth    = 550f;   // total width available for a row
	private const float EnemyStaggerX    = 30f;    // depth stagger per row
	private const float EnemyStaggerY    = 30f;    // depth stagger per row

	// Depth shading same as party
	private const float EnemyShadeRow1   = 0.70f;  // back row
	private const float EnemyShadeRow2   = 0.77f;  // middle row
	private const float EnemyShadeRow3   = 0.85f;  // middle row
	private const float EnemyShadeRow4   = 0.92f;  // middle row
	private const float EnemyShadeRow5   = 1.00f;  // front row

	public override void _Ready()
	{
		_partyContainer = GetNode<Control>("PartyContainer");
		_enemyContainer = GetNode<Control>("EnemyContainer");
		
		_gameState       = GetNode<GameState>("/root/GameState");
		
		LoadParty();
		LoadEnemies();
	}

	private void LoadParty()
	{
		var gameState = GetNode<GameState>("/root/GameState");
		var party = gameState.Party;

		for (int i = 0; i < party.Count; i++)
		{
			var character = party[i];
			var sprite = new TextureRect();

			sprite.CustomMinimumSize   = new Vector2(SpriteSize, SpriteSize);
			sprite.CustomMaximumSize   = new Vector2(SpriteSize, SpriteSize);
			sprite.StretchMode         = TextureRect.StretchModeEnum.KeepAspect;
			sprite.ExpandMode          = TextureRect.ExpandModeEnum.IgnoreSize;
			sprite.SizeFlagsVertical   = Control.SizeFlags.ShrinkBegin;
			sprite.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

			if (!string.IsNullOrEmpty(character.BattleSprite)
				&& ResourceLoader.Exists(character.BattleSprite))
				sprite.Texture = GD.Load<Texture2D>(character.BattleSprite);

			// Calculate staggered position
			// Front row = 0,1,2  Back row = 3,4,5
			bool isFrontRow = i < 3;
			int rowIndex    = isFrontRow ? i : i - 3;
			float rowX      = isFrontRow ? StartX + RowOffset : StartX;
			float x         = rowX      + rowIndex * StaggerX;
			float y         = StartY    + rowIndex * StaggerY;

			// Shade by vertical position - top of column is darkest
			float[] shades = { ShadeRow1, ShadeRow2, ShadeRow3 };
			sprite.Modulate = new Color(shades[rowIndex], shades[rowIndex], shades[rowIndex], 1.0f);

			sprite.Position = new Vector2(x, y);
			_partyContainer.AddChild(sprite);
		}
	}

	private void LoadEnemies()
	{
		var formation = _gameState.CurrentEncounter;
		
		if (formation == null || formation.Count == 0)
		{
			GD.PrintErr("No encounter data found in GameState");
			return;
		}

		GD.Print($"LoadEnemies called - {formation.Count} rows");

		for (int row = 0; row < formation.Count; row++)
		{
			var rowMonsterIds = formation[row];
			int count         = rowMonsterIds.Count;
			GD.Print($"Row {row}: {count} monsters");

			float shade = row switch
			{
				0 => EnemyShadeRow1,
				1 => EnemyShadeRow2,
				2 => EnemyShadeRow3,
				_ => 1.0f
			};

			float rowX    = EnemyStartX + EnemyRowOffset * row;
			float rowY    = EnemyStartY;
			float spacing = EnemyMaxWidth / (count + 1);

			for (int i = 0; i < count; i++)
			{
				// Load monster data
				var monster = MonsterLoader.LoadMonster(rowMonsterIds[i]);
				if (monster == null) continue;

				var sprite = new TextureRect();
				sprite.CustomMinimumSize   = new Vector2(EnemySpriteSize, EnemySpriteSize);
				sprite.CustomMaximumSize   = new Vector2(EnemySpriteSize, EnemySpriteSize);
				sprite.StretchMode         = TextureRect.StretchModeEnum.KeepAspect;
				sprite.ExpandMode          = TextureRect.ExpandModeEnum.IgnoreSize;
				sprite.SizeFlagsVertical   = Control.SizeFlags.ShrinkBegin;
				sprite.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
				sprite.Modulate            = new Color(shade, shade, shade, 1.0f);

				// Load sprite from monster data
				if (!string.IsNullOrEmpty(monster.Sprite)
					&& ResourceLoader.Exists(monster.Sprite))
					sprite.Texture = GD.Load<Texture2D>(monster.Sprite);
				else
					GD.PrintErr($"No sprite found for {monster.Name}: {monster.Sprite}");

				float x         = rowX + i * EnemyStaggerX;
				float y         = rowY + spacing * (i + 1) + i * EnemyStaggerY;
				sprite.Position = new Vector2(x, y);

				_enemyContainer.AddChild(sprite);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!DebugFlags.AutoFormPartyOnEmbark) return;
		
		// Clear existing sprites
		foreach (Node child in _partyContainer.GetChildren())
			child.QueueFree();
		
		LoadParty();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key 
			&& key.Pressed 
			&& key.Keycode == Key.F1)
		{
			foreach (Node child in _partyContainer.GetChildren())
				child.QueueFree();
			LoadParty();
			GD.Print("Party redrawn");
		}
	}
}
