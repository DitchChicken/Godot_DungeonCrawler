using Godot;
using System.Collections.Generic;
using System.Linq;

public class SceneController
{
	private Scene _scene;
	private EncounterInstance _encounter;
	private GameState _gs;
	private ScenePanel _panel;

	// Set when an option requests combat; the panel reads it on close
	public bool CombatRequested { get; private set; } = false;

	public SceneController(Scene scene, EncounterInstance encounter, GameState gs, ScenePanel panel)
	{
		_scene     = scene;
		_encounter = encounter;
		_gs        = gs;
		_panel     = panel;
	}

	public void Start() => GoTo(_scene.StartNode);

	private void GoTo(string nodeId)
	{
		if (!_scene.Nodes.TryGetValue(nodeId, out var node))
		{
			GD.PrintErr($"Scene '{_scene.Id}': no node '{nodeId}'");
			_panel.CloseScene();
			return;
		}

		// Build the option list the panel will render, with availability + hints
		var display = new List<ScenePanel.OptionDisplay>();
		foreach (var opt in node.Options)
		{
			bool available = OptionAvailable(opt);
			string hint    = CheckHint(opt);
			display.Add(new ScenePanel.OptionDisplay
			{
				Label     = opt.Label + hint,
				Available = available,
				Option    = opt
			});
		}

		_panel.ShowNode(node, display);
	}

	// Called by the panel when the player clicks an option
	public void ChooseOption(SceneOption opt)
	{
		var outcomes = opt.Check == null
			? opt.Outcomes
			: ResolveCheck(opt);

		ApplyOutcomes(outcomes);
	}

	private void ApplyOutcomes(List<Outcome> outcomes)
	{
		foreach (var outcome in outcomes)
		{
			switch (outcome.Type)
			{
				case "GoToNode":
					GoTo(outcome.Value);
					return;

				case "EndScene":
					_panel.CloseScene();
					return;

				case "StartCombat":
					CombatRequested = true;
					_panel.CloseScene();
					return;

				case "SetDisposition":
					if (System.Enum.TryParse<Disposition>(outcome.Value, true, out var d))
						_encounter.Disposition = d;
					break;

				case "SetRequirement":
					if (System.Enum.TryParse<InteractionRequirement>(outcome.Value, true, out var r))
						_encounter.Requirement = r;
					break;

				default:
					// Delegate shared outcomes (SetFlag, GiveItem, AddLoot, ShowText…)
					var roomState = _gs.GetDungeonState(_gs.CurrentDungeon)
									   .GetRoomState(_gs.CurrentRoom.Id);
					InteractionResolver.ApplyOutcomeExternal(outcome, _gs, roomState, _panel);
					break;
			}
		}
	}

	private bool OptionAvailable(SceneOption opt)
	{
		var roomState = _gs.GetDungeonState(_gs.CurrentDungeon).GetRoomState(_gs.CurrentRoom.Id);
		foreach (var req in opt.Requires)
			if (!InteractionResolver.RequirementMet(req, _gs, roomState))
				return false;

		// A leveled check needs a qualified party member to even attempt
		if (opt.Check != null && opt.Check.Level > 0)
		{
			bool anyone = _gs.Party.Any(c =>
				c.CanAct() && c.GetDomainLevel(opt.Check.Domain) >= opt.Check.Level);
			if (!anyone) return false;
		}
		return true;
	}

	private string CheckHint(SceneOption opt)
	{
		if (opt.Check == null) return "";
		double chance = CheckResolver.BestChance(opt.Check, _gs);
		return $"  ({CheckOddsTable.DifficultyLabel(chance)})";
	}

	private List<Outcome> ResolveCheck(SceneOption opt)
	{
		var info  = CheckResolver.Resolve(opt.Check, _gs);
		var tiers = opt.CheckOutcomes ?? new TieredOutcomes();

		if (info.Result == CheckResult.NoQualifiedCharacter)
		{
			_panel.AppendText("No one is capable of that.");
			return tiers.Failure;
		}

		_panel.AppendText($"{info.Attempter.Name} tries... " +
						  (info.Result == CheckResult.Success || info.Result == CheckResult.CriticalSuccess
							  ? "and succeeds." : "and fails."));

		return info.Result switch
		{
			CheckResult.CriticalSuccess => tiers.CriticalSuccess.Count > 0 ? tiers.CriticalSuccess : tiers.Success,
			CheckResult.Success         => tiers.Success,
			_                           => tiers.Failure
		};
	}
}
