using Godot;
using System;
using System.Collections.Generic;

public class DialogueTree
{
	public string Id { get; set; }
	public string StartNode { get; set; }
	public Dictionary<string, DialogueNode> Nodes { get; set; } = new();
}

public class DialogueNode
{
	public string Id { get; set; }
	public string Image { get; set; } = "";       // speaker portrait/scene, optional
	public List<string> Text { get; set; } = new(); // joined, like room descriptions
	public List<DialogueOption> Options { get; set; } = new();
}

public class DialogueOption
{
	public string Label { get; set; }
	public List<Requirement> Requires { get; set; } = new();  // reused
	public Check Check { get; set; }                          // reused — DC/domain

	// Flat outcomes, or tiered when there's a check — reusing Outcome + TieredOutcomes
	public List<Outcome> Outcomes { get; set; } = new();
	public TieredOutcomes CheckOutcomes { get; set; }
}
