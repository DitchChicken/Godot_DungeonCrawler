using Godot;

public partial class TurnTrackerSlot : PanelContainer
{
	private TextureRect _portrait;
	public Combatant Combatant { get; private set; }
	public int TurnIndex { get; private set; } = -1;
	private bool _isActive;
	private Tween _pulseTween;

	[Signal] public delegate void HoveredEventHandler();
	[Signal] public delegate void UnhoveredEventHandler();

	public void InitializeEmpty(float size)
	{
		CustomMinimumSize = new Vector2(size, size);
		CustomMaximumSize = new Vector2(size, size);
		MouseFilter       = MouseFilterEnum.Stop;

		_portrait = new TextureRect();
		_portrait.LayoutMode   = 1;
		_portrait.AnchorRight  = 1.0f;
		_portrait.AnchorBottom = 1.0f;
		_portrait.StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered;
		_portrait.ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize;
		_portrait.MouseFilter  = MouseFilterEnum.Ignore;
		AddChild(_portrait);

		MouseEntered += () => EmitSignal(SignalName.Hovered);
		MouseExited  += () => EmitSignal(SignalName.Unhovered);

		SetEmpty();
	}

	public void SetCombatant(Combatant combatant, int turnIndex, bool isActive)
	{
		Combatant = combatant;
		TurnIndex = turnIndex;
		_isActive = isActive;

		LoadPortrait();
		ApplyActiveStyle();
		Visible = true;
	}

	public void SetEmpty()
	{
		Combatant = null;
		TurnIndex = -1;
		StopPulse();
		if (_portrait != null) _portrait.Texture = null;
		Modulate = new Color(1, 1, 1, 0.3f); // faded empty slot
	}

	private void LoadPortrait()
	{
		Modulate = new Color(1, 1, 1, 1);

		if (Combatant.IsParty)
		{
			var portrait = Combatant.Character.Portrait;
			if (!string.IsNullOrEmpty(portrait) && ResourceLoader.Exists(portrait))
				_portrait.Texture = GD.Load<Texture2D>(portrait);
		}
		else
		{
			var portrait = Combatant.Monster.Portrait;
			var sprite   = Combatant.Monster.Sprite;

			if (!string.IsNullOrEmpty(portrait) && ResourceLoader.Exists(portrait))
				_portrait.Texture = GD.Load<Texture2D>(portrait);
			else if (!string.IsNullOrEmpty(sprite) && ResourceLoader.Exists(sprite))
				_portrait.Texture = GD.Load<Texture2D>(sprite);
		}
	}

	private void ApplyActiveStyle()
	{
		if (_isActive) StartPulse();
		else StopPulse();
	}

	private void StartPulse()
	{
		StopPulse();
		_pulseTween = CreateTween();
		_pulseTween.SetLoops();
		_pulseTween.TweenProperty(this, "modulate",
			new Color(0.6f, 1.0f, 0.6f, 1.0f), 0.8f);
		_pulseTween.TweenProperty(this, "modulate",
			new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.8f);
	}

	private void StopPulse()
	{
		_pulseTween?.Kill();
		_pulseTween = null;
	}
}
