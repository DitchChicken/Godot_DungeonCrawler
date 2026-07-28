using Godot;
using System.Collections.Generic;
using System.Linq;

public static class InteractionResolver
{
	// Text produced by the last resolve, for the dungeon UI to display
	public static List<string> LastMessages { get; private set; } = new List<string>();
	
	public static bool IsAvailable(Interaction action, GameState gs, RoomState roomState)
	{
		// Hidden until explicitly revealed this run
		if (action.Hidden && !roomState.RevealedActions.Contains(action.Id))
			return false;
		
		if (action.OneShot && roomState.CompletedActions.Contains(action.Id))
			return false;

		var dungeonState = gs.GetDungeonState(gs.CurrentDungeon);

		foreach (var req in action.Requires)
			if (!RequirementMet(req, gs, roomState))
			return false;

		return true;
	}

	public static void Execute(Interaction action, GameState gs, RoomState roomState)
	{
		LastMessages = new List<string>();

		var outcomes = action.Check == null
			? action.Outcomes
			: ResolveCheck(action, gs);

		foreach (var outcome in outcomes)
			ApplyOutcome(outcome, gs, roomState);

		if (action.TimeCost > 0f)
			DungeonClock.Advance(gs, action.TimeCost, $"action: {action.Id}");

		if (action.OneShot)
			roomState.CompletedActions.Add(action.Id);
	}

	// Picks the best-qualified character and rolls. Stub tiers for now.
	private static List<Outcome> ResolveCheck(Interaction action, GameState gs)
	{
		var info = CheckResolver.Resolve(action.Check, gs);
		var tiers = action.CheckOutcomes ?? new TieredOutcomes();

		if (info.Result == CheckResult.NoQualifiedCharacter)
		{
			LastMessages.Add("No one in the party is skilled enough to attempt this.");
			return tiers.Failure;
		}

		LastMessages.Add($"{info.Attempter.Name} attempts the task... " +
						 $"(rolled {info.Total} vs DC {action.Check.Difficulty})");

		return info.Result switch
		{
			CheckResult.CriticalSuccess =>
				tiers.CriticalSuccess.Count > 0 ? tiers.CriticalSuccess : tiers.Success,
			CheckResult.Success         => tiers.Success,
			CheckResult.CriticalFailure =>
				tiers.CriticalFailure.Count > 0 ? tiers.CriticalFailure : tiers.Failure,
			_                           => tiers.Failure
		};
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

	private static void ApplyOutcome(Outcome outcome, GameState gs, RoomState roomState)
	{
		var dungeonState = gs.GetDungeonState(gs.CurrentDungeon);

		switch (outcome.Type)
		{
			case "ShowText":
				LastMessages.Add(outcome.Text);
				break;

			case "ToggleDoor":
				DoorOutcome(dungeonState, outcome, gs, d => d.Open = !d.Open);
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

			case "RevealAction":
				roomState.RevealedActions.Add(outcome.ActionId);
				break;
				
			case "UnlockDoor":   DoorOutcome(dungeonState, outcome, gs, d => d.Locked = false); break;
			case "BreakDoor":    DoorOutcome(dungeonState, outcome, gs, d => d.Exists = false); break;
			case "OpenDoor":     DoorOutcome(dungeonState, outcome, gs, d => d.Open   = true);  break;
			case "CloseDoor":    DoorOutcome(dungeonState, outcome, gs, d => d.Open   = false); break;

			case "RevealExit":
				{
					if (!System.Enum.TryParse<Direction>(outcome.Direction, true, out var revealDir)) break;

					string rid   = string.IsNullOrEmpty(outcome.Room) ? gs.CurrentRoom?.Id : outcome.Room;
					var mapRoom  = dungeonState.Map?.GetRoom(rid);
					var revealed = mapRoom?.GetExit(revealDir);
					if (revealed == null) break;

					revealed.Discovered = true;
					if (revealed.Visibility == ExitVisibility.HiddenToParty)
						revealed.Visibility = ExitVisibility.Visible;

					// Mirror discovery on the far side — a found passage is found from both rooms
					var far     = dungeonState.Map.GetRoom(revealed.TargetRoomId);
					var farExit = far?.GetExit(revealDir.Opposite());
					if (farExit != null && farExit.TargetRoomId == rid)
					{
						farExit.Discovered = true;
						if (farExit.Visibility == ExitVisibility.HiddenToParty)
							farExit.Visibility = ExitVisibility.Visible;
					}
					break;
				}
				
			case "AddLoot":
				{
					int amount = !string.IsNullOrEmpty(outcome.AmountDice)
						? Dice.Roll(outcome.AmountDice)
						: System.Math.Max(1, outcome.Amount);

					if (amount <= 0) break;

					var lootItem = EquipmentLoader.LoadEquipment(outcome.ItemId);
					if (lootItem == null)
					{
						GD.PrintErr($"AddLoot: unknown item '{outcome.ItemId}'");
						break;
					}

					roomState.LootPile.AddItem(lootItem, amount);
					LastMessages.Add($"{amount} {lootItem.Name} spills onto the floor.");
					break;
				}

			case "IdentifyItem":
				gs.IdentifyItemType(outcome.ItemId);
				LastMessages.Add($"Identified: {EquipmentLoader.LoadEquipment(outcome.ItemId)?.Name}.");
				break;

			default:
				GD.PrintErr($"Unknown outcome type: {outcome.Type}");
				break;
		}
	}

	public static void ApplyOutcomeExternal(Outcome outcome, GameState gs, RoomState rs, ScenePanel panel)
	{
		if (outcome.Type == "ShowText") { panel.AppendText(outcome.Text); return; }
		ApplyOutcome(outcome, gs, rs);   // existing private method
	}	
		
	public static bool RequirementMet(Requirement req, GameState gs, RoomState roomState)
	{
		var dungeonState = gs.GetDungeonState(gs.CurrentDungeon);

		switch (req.Type)
		{
			case "Flag":
				return dungeonState.Flags.Contains(req.Value);
			case "NotFlag":
				return !dungeonState.Flags.Contains(req.Value);
			case "ActionCompleted":
				return roomState.CompletedActions.Contains(req.Value);
			case "ClassInParty":
				return gs.Party.Any(c => c.IsAlive
					&& c.ClassType.ToString().Equals(req.Value, System.StringComparison.OrdinalIgnoreCase));
			case "Item":
				return gs.Party.Any(c => c.PersonalInventory.HasItem(req.Value))
					|| gs.PartyVault.HasItem(req.Value);
			default:
				return true;
		}
	}
	
	private static void DoorOutcome(DungeonState state, Outcome outcome, GameState gs,
								System.Action<Door> apply)
	{
		if (!System.Enum.TryParse<Direction>(outcome.Direction, true, out var dir)) return;
		string roomId = string.IsNullOrEmpty(outcome.Room) ? gs.CurrentRoom?.Id : outcome.Room;
		var door = state.Map?.GetRoom(roomId)?.GetExit(dir)?.Door;
		if (door == null) { GD.PrintErr($"Door outcome: no door {dir} in {roomId}"); return; }

		apply(door);
		if (!string.IsNullOrEmpty(door.UnlockFlag) && !door.Locked)
			state.Flags.Add(door.UnlockFlag);
	}
}
