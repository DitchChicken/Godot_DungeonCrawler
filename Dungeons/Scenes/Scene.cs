using System.Collections.Generic;

public class Scene
{
	public string Id { get; set; }
	public string StartNode { get; set; }
	public Dictionary<string, SceneNode> Nodes { get; set; } = new();
}

public class SceneNode
{
	public string Id { get; set; }
	public string Image { get; set; } = "";
	public List<string> Text { get; set; } = new();
	public List<SceneOption> Options { get; set; } = new();

	public string GetText() => string.Join(" ", Text);
}

public class SceneOption
{
	public string Label { get; set; }
	public List<Requirement> Requires { get; set; } = new();  // reused
	public Check Check { get; set; }                          // reused

	// Flat outcomes, or tiered when a check is present — reused
	public List<Outcome> Outcomes { get; set; } = new();
	public TieredOutcomes CheckOutcomes { get; set; }
}
