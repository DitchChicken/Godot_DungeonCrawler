using Godot;
using System.Linq;

public static class DebugCommands
{
	public static void RegisterAll(GameState gameState)
	{
		var c = DebugConsole.Instance;
		if (c == null) return;

		// --- Party ---

		c.Register("party", args =>
		{
			var lines = gameState.Party.Select(p =>
				$"  {p.Name,-16} {p.ClassType,-8} HP {p.CurrentHP}/{p.MaxHP}  " +
				$"MP {p.CurrentMana}/{p.MaxMana}  {p.Status}");
			return "Party:\n" + string.Join("\n", lines);
		}, "List party members and their vitals");

		c.Register("heal", args =>
		{
			foreach (var p in gameState.Party)
			{
				p.CurrentHP   = p.MaxHP;
				p.CurrentMana = p.MaxMana;
				p.Status      = Status.Ok;
				p.ActiveEffects.Clear();
			}
			RefreshHud();
			return "Party fully healed.";
		}, "Fully heal and restore the party");

		c.Register("damage", args =>
		{
			if (args.Length < 1) return "usage: damage <amount> [name]";
			int amount = int.Parse(args[0]);

			var targets = args.Length > 1
				? gameState.Party.Where(p => p.Name.ToLower().StartsWith(args[1].ToLower()))
				: gameState.Party;

			foreach (var p in targets)
				p.CurrentHP = System.Math.Max(0, p.CurrentHP - amount);

			RefreshHud();
			return $"Applied {amount} damage.";
		}, "damage <amount> [name] — damage party or one member");

		// --- Gold & items ---

		c.Register("gold", args =>
		{
			if (args.Length == 0) return $"Gold: {gameState.Gold}";
			gameState.Gold += int.Parse(args[0]);
			return $"Gold: {gameState.Gold}";
		}, "gold [amount] — show gold, or add the given amount");

		c.Register("give", args =>
		{
			if (args.Length < 1) return "usage: give <itemId> [count]";
			int count = args.Length > 1 ? int.Parse(args[1]) : 1;

			var item = EquipmentLoader.LoadEquipment(args[0]);
			if (item == null) return $"No such item: {args[0]}";

			gameState.PartyVault.AddItem(item, count);
			return $"Added {count}x {item.Name} to the vault.";
		}, "give <itemId> [count] — add an item to the party vault");

		// --- Status effects ---

		c.Register("status", args =>
		{
			if (args.Length < 2)
				return "usage: status <name> <effect> [duration] [potency]";

			var target = gameState.Party.FirstOrDefault(p =>
				p.Name.ToLower().StartsWith(args[0].ToLower()));
			if (target == null) return $"No party member: {args[0]}";

			if (!System.Enum.TryParse<StatusType>(args[1], true, out var type))
				return $"No such status: {args[1]}";

			int duration = args.Length > 2 ? int.Parse(args[2]) : 3;
			int potency  = args.Length > 3 ? int.Parse(args[3]) : 1;

			target.AddStatusEffect(new StatusEffect(type, duration, potency));
			RefreshHud();
			return $"{target.Name} is now {type} ({duration} rounds, x{potency}).";
		}, "status <name> <effect> [duration] [potency] — apply a status effect");

		// --- Dungeon / encounters ---

		c.Register("map", args =>
		{
			var state = gameState.GetDungeonState(gameState.CurrentDungeon);
			if (state?.Map == null) return "No map.";
			var lines = state.Map.Rooms.Values.Select(r =>
				$"  {r.RoomId,-16} {r.Coordinates}  exits: " +
				string.Join(", ", r.Exits.Select(e => $"{e.Direction}→{e.TargetRoomId}[{e.State}]")));
			return $"Map ({state.Map.Rooms.Count} rooms):\n" + string.Join("\n", lines);
		}, "Dump the dungeon map graph");

		c.Register("encounters", args =>
		{
			if (string.IsNullOrEmpty(gameState.CurrentDungeon))
				return "Not in a dungeon.";

			var state = gameState.GetDungeonState(gameState.CurrentDungeon);
			var all   = state.Encounters.GetAllInstances();

			if (all.Count == 0) return "No live encounters.";

			var lines = all.Select(e =>
			{
				string monsters = string.Join(", ",
					e.AllMonsters.Select(m => $"{m.CombatLabel} {m.CurrentHP}/{m.MaxHP}"));
				return $"  {e.InstanceId} [{e.Attachment}] room:{(string.IsNullOrEmpty(e.RoomId) ? "-" : e.RoomId)}\n" +
					   $"    {monsters}";
			});
			return "Live encounters:\n" + string.Join("\n", lines);
		}, "List all live encounter instances and monster HP");

		c.Register("spawn", args =>
		{
			if (args.Length < 1) return "usage: spawn <encounterId>";
			if (string.IsNullOrEmpty(gameState.CurrentDungeon)) return "Not in a dungeon.";

			var state  = gameState.GetDungeonState(gameState.CurrentDungeon);
			var roomId = gameState.CurrentRoom?.Id ?? "";

			var inst = state.Encounters.CreateInstance(
				gameState.CurrentDungeon, args[0], roomId, EncounterAttachment.Permanent);

			return inst == null
				? $"Failed to spawn {args[0]}"
				: $"Spawned {args[0]} as {inst.InstanceId} in {roomId}";
		}, "spawn <encounterId> — create an encounter in the current room");

		c.Register("xp", args =>
		{
			if (args.Length < 1) return "usage: xp <amount>";
			int amount = int.Parse(args[0]);
			foreach (var p in gameState.Party)
				p.GainExperience(amount);
			return $"Granted {amount} XP to the party.";
		}, "xp <amount> — grant experience to the party");
		
		c.Register("search", args =>
		{
			var state = gameState.GetDungeonState(gameState.CurrentDungeon);
			var rs    = state?.GetRoomState(gameState.CurrentRoom?.Id);
			return rs == null ? "Not in a room." : $"Search level: {rs.Searched}";
		}, "Show the current room's search state");
		
		c.Register("domain", args =>
		{
			if (args.Length < 3) return "usage: domain <name> <domain> <level>";
			var ch = gameState.Party.FirstOrDefault(p =>
				p.Name.ToLower().StartsWith(args[0].ToLower()));
			if (ch == null) return $"No party member: {args[0]}";
			ch.Domains[args[1]] = int.Parse(args[2]);
			return $"{ch.Name} now has {args[1]} at level {args[2]}.";
		}, "domain <name> <domain> <level> — set a character's domain level");

	}

	private static void RefreshHud()
	{
		var hud = ((SceneTree)Engine.GetMainLoop()).Root
			.GetNodeOrNull<PartyHUD>("/root/PartyHud");
		hud?.Refresh();
	}
}
