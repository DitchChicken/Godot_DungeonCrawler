using Godot;
using System.Collections.Generic;

public partial class SpriteHighlighter : Node
{
	public enum HighlightType
	{
		None,
		Active,    // green — current combatant's turn
		Hover,     // blue — hovering in turn tracker
		Target,    // red — valid attack target
		Ally,      // yellow — valid ally target (row switch etc)
	}

	// Maps sprite → its base shade (set once on load)
	private Dictionary<Control, float> _baseShades 
		= new Dictionary<Control, float>();

	// Maps sprite → current highest priority highlight
	private Dictionary<Control, HighlightType> _highlights 
		= new Dictionary<Control, HighlightType>();

	// Active tweens per sprite
	private Dictionary<Control, Tween> _tweens 
		= new Dictionary<Control, Tween>();

	private Dictionary<Control, List<HighlightType>> _highlightStacks
		= new Dictionary<Control, List<HighlightType>>();

	// Priority order — higher index = higher priority
	private static readonly HighlightType[] Priority = {
		HighlightType.None,
		HighlightType.Active,		
		HighlightType.Target,
		HighlightType.Ally,
		HighlightType.Hover
	};

	public void Register(Control sprite, float baseShade)
	{
		_baseShades[sprite]       = baseShade;
		_highlightStacks[sprite]  = new List<HighlightType>();
	}

	public void SetHighlight(Control sprite, HighlightType type)
	{
		if (!_baseShades.ContainsKey(sprite)) return;
		if (type == HighlightType.None) return;

		var stack = _highlightStacks[sprite];
		if (!stack.Contains(type))
			stack.Add(type);

		ApplyTopHighlight(sprite);
	}

	public void ClearHighlight(Control sprite, HighlightType type)
	{
		if (!_highlightStacks.ContainsKey(sprite)) return;
		_highlightStacks[sprite].Remove(type);
		ApplyTopHighlight(sprite);
	}

	public void ClearHighlight(HighlightType type)
	{
		foreach (var sprite in new List<Control>(_highlightStacks.Keys))
		{
			_highlightStacks[sprite].Remove(type);
			ApplyTopHighlight(sprite);
		}
	}

	public void ClearAll()
	{
		foreach (var sprite in new List<Control>(_highlightStacks.Keys))
		{
			_highlightStacks[sprite].Clear();
			ApplyTopHighlight(sprite);
		}
	}

	private void ApplyTopHighlight(Control sprite)
	{
		if (!_highlightStacks.ContainsKey(sprite)) return;

		var stack = _highlightStacks[sprite];

		// Find highest priority highlight in stack
		HighlightType top = HighlightType.None;
		int topPri = -1;
		foreach (var h in stack)
		{
			int pri = System.Array.IndexOf(Priority, h);
			if (pri > topPri)
			{
				top    = h;
				topPri = pri;
			}
		}

		// Kill existing tween
		if (_tweens.TryGetValue(sprite, out var existing))
		{
			existing?.Kill();
			_tweens.Remove(sprite);
		}

		float shade = _baseShades[sprite];

		switch (top)
		{
			case HighlightType.None:
				sprite.Modulate = new Color(shade, shade, shade, 1.0f);
				break;
			case HighlightType.Active:
				StartPulse(sprite, shade,
					new Color(0.2f, 0.9f, 0.2f, 1.0f),
					new Color(shade, shade, shade, 1.0f), 0.8f);
				break;
			case HighlightType.Hover:
				StartPulse(sprite, shade,
					new Color(0.2f, 0.2f, 0.9f, 1.0f),
					new Color(shade, shade, shade, 1.0f), 0.5f);
				break;
			case HighlightType.Target:
				sprite.Modulate = new Color(shade, shade * 0.4f, shade * 0.4f, 1.0f);
				break;
			case HighlightType.Ally:
				sprite.Modulate = new Color(shade, shade, shade * 0.4f, 1.0f);
				break;
		}
	}
	
	private void StartPulse(Control sprite, float shade,
		Color colorA, Color colorB, float duration)
	{
		var tween = sprite.CreateTween();
		tween.SetLoops();
		tween.TweenProperty(sprite, "modulate", colorA, duration);
		tween.TweenProperty(sprite, "modulate", colorB, duration);
		_tweens[sprite] = tween;
	}

	// Call when a sprite is removed (death etc)
	public void Unregister(Control sprite)
	{
		if (_tweens.TryGetValue(sprite, out var tween))
		{
			tween?.Kill();
			_tweens.Remove(sprite);
		}
		_baseShades.Remove(sprite);
		_highlightStacks.Remove(sprite);
	}
}
