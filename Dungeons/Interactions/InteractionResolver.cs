using Godot;
using System.Collections.Generic;
using System.Linq;

public static class InteractionResolver
{
	// Text produced by the last resolve, for the dungeon UI to display
	public static List<string> LastMessages { get; private set; } = new List<string>();

	public static bool IsAvailable(Interaction action, GameState gs, RoomState roomState)
	{
		if (action.OneShot && roomState.CompletedActions.Contains(action.Id))
			return false;

		var dungeonState = gs.GetDungeonState(gs.CurrentDungeon);

		foreach (var req in action.Requires)
		{
			switch (req.Type)
			{
				case "Flag":
					if (!dungeonState.Flags.Contains(req.Value)) return false;
					break;
				case "NotFlag":
					if (dungeonState.Flags.Contains(req.Value)) return false;
					break;
				case "ActionCompleted":
					if (!roomState.CompletedActions.Contains(req.Value)) return false;
					break;
				case "ClassInParty":
					if (!gs.Party.Any(c => c.IsAlive
						&& c.ClassType.ToString().Equals(req.Value, System.StringComparison.OrdinalIgnoreCase)))
						return false;
					break;
				case "Item":
					if (!gs.Party.Any(c => c.PersonalInventory.HasItem(req.Value))
						&& !gs.PartyVault.HasItem(req.Value))
						return false;
					break;
			}
		}
		return true;
	}

	public static void Execute(Interaction action, GameState gs, RoomState roomState)
	{
		LastMessages = new List<string>();

		var outcomes = action.Check == null
			? action.Outcomes
			: ResolveCheck(action, gs);

		foreach (var outcome in outcomes)
			ApplyOutcome(outcome, gs);

		if (action.OneShot)
			roomState.CompletedActions.Add(action.Id);
	}

	// Picks the best-qualified character and rolls. Stub tiers for now.
	private static List<Outcome> ResolveCheck(Interaction action, GameState gs)
	{
		var best = PickBestCharacter(action.Check.Stat, gs);
		if (best == null) return action.CheckOutcomes?.Failure ?? new List<Outcome>();

		int statValue = GetStat(best, action.Check.Stat);
		int roll      = new System.Random().Next(1, 21) + statValue;
		int diff      = action.Check.Difficulty;

		LastMessages.Add($"{best.Name} attempts the task...");

		var tiers = action.CheckOutcomes ?? new TieredOutcomes();
		if (roll >= diff + 10) return tiers.CriticalSuccess.Count > 0 ? tiers.CriticalSuccess : tiers.Success;
		if (roll >= diff)      return tiers.Success;
		if (roll <= diff - 10) return tiers.CriticalFailure.Count > 0 ? tiers.CriticalFailure : tiers.Failure;
		return tiers.Failure;
	}

	private static Character PickBestCharacter(string stat, GameState gs)
		=> gs.Party.Where(c => c.IsAlive)
				   .OrderByDescending(c => GetStat(c, stat))
				   .FirstOrDefault();

	private static int GetStat(Character c, string stat) => stat.ToLower() switch
	{
		"strength"     => c.TotalStrength(),
		"intelligence" => c.TotalIntelligence(),
		"wisdom"       => c.TotalWisdom(),
		"dexterity"    => c.TotalDexterity(),
		"constitution" => c.TotalConstitution(),
		"charisma"     => c.TotalCharisma(),
		_ => 0
	};

	private static void ApplyOutcome(Outcome outcome, GameState gs)
	{
		var dungeonState = gs.GetDungeonState(gs.CurrentDungeon);

		switch (outcome.Type)
		{
			case "ShowText":
				LastMessages.Add(outcome.Text);
				break;

			case "SetExitState":
				SetExitState(dungeonState, outcome, gs);
				break;

			case "ToggleExitState":
				ToggleExitState(dungeonState, outcome, gs);
				break;

			case "SetFlag":
				dungeonState.Flags.Add(outcome.Flag);
				break;

			case "ClearFlag":
				dungeonState.Flags.Remove(outcome.Flag);
				break;

			case "GiveItem":
				var item = EquipmentLoader.LoadEquipment(outcome.ItemId);
				if (item != null)
				{
					gs.PartyVault.AddItem(item, System.Math.Max(1, outcome.Amount));
					LastMessages.Add($"Gained {item.Name}.");
				}
				break;

			case "SpawnEncounter":
				dungeonState.Encounters.CreateInstance(
					gs.CurrentDungeon, outcome.EncounterId,
					gs.CurrentRoom?.Id ?? "", EncounterAttachment.Permanent);
				break;

			case "HealParty":
				foreach (var c in gs.Party.Where(p => p.IsAlive))
					c.CurrentHP = System.Math.Min(c.MaxHP, c.CurrentHP + outcome.Amount);
				break;

			case "DamageParty":
				foreach (var c in gs.Party.Where(p => p.IsAlive))
					c.CurrentHP = System.Math.Max(0, c.CurrentHP - outcome.Amount);
				break;

			default:
				GD.PrintErr($"Unknown outcome type: {outcome.Type}");
				break;
		}
	}

	// Sets an exit's state AND its reciprocal in the target room.
	private static void SetExitState(DungeonState state, Outcome outcome, GameState gs)
	{
		if (!System.Enum.TryParse<Direction>(outcome.Direction, true, out var dir)) return;
		if (!System.Enum.TryParse<ExitState>(outcome.State, true, out var newState)) return;

		// Default to the current room when none specified
		string roomId = string.IsNullOrEmpty(outcome.Room) ? gs.CurrentRoom?.Id : outcome.Room;
		ApplyExitState(state, roomId, dir, newState);
	}

	// Flips Open <-> Blocked (for levers)
	private static void ToggleExitState(DungeonState state, Outcome outcome, GameState gs)
	{
		if (!System.Enum.TryParse<Direction>(outcome.Direction, true, out var dir)) return;

		string roomId = string.IsNullOrEmpty(outcome.Room) ? gs.CurrentRoom?.Id : outcome.Room;
		var room = state.Map?.GetRoom(roomId);
		var exit = room?.GetExit(dir);
		if (exit == null) return;

		var newState = exit.State == ExitState.Open ? ExitState.Blocked : ExitState.Open;
		ApplyExitState(state, roomId, dir, newState);
	}

	private static void ApplyExitState(DungeonState state, string roomId, Direction dir, ExitState newState)
	{
		var room = state.Map?.GetRoom(roomId);
		var exit = room?.GetExit(dir);
		if (exit == null)
		{
			GD.PrintErr($"SetExitState: no {dir} exit in room '{roomId}'");
			return;
		}

		exit.State = newState;

		// Mirror on the far side so the door works from both rooms
		var target   = state.Map.GetRoom(exit.TargetRoomId);
		var backExit = target?.GetExit(dir.Opposite());
		if (backExit != null && backExit.TargetRoomId == roomId)
			backExit.State = newState;
	}
}
