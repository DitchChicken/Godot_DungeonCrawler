using Godot;
using System.Collections.Generic;

public static class StatusProcessor
{
	// Called at the START of a combatant's turn.
	// Returns a list of log messages describing what happened.
	// Sets skipTurn = true if the combatant can't act.
	public static List<string> ProcessTurnStart(
		Combatant combatant, out bool skipTurn)
	{
		var log = new List<string>();
		skipTurn = false;

		var effects = combatant.IsParty
			? combatant.Character.ActiveEffects
			: combatant.Monster.ActiveEffects;

		// Copy list since we may modify it
		foreach (var effect in new List<StatusEffect>(effects))
		{
			switch (effect.Type)
			{
				case StatusType.Poisoned:
					int poisonDmg = effect.Potency; // 1 damage per stack
					ApplyDamage(combatant, poisonDmg);
					log.Add($"{combatant.Name} takes {poisonDmg} poison damage.");
					break;

				case StatusType.Asleep:
					skipTurn = true;
					log.Add($"{combatant.Name} is asleep.");
					break;

				case StatusType.Paralyzed:
					skipTurn = true;
					log.Add($"{combatant.Name} is paralyzed.");
					break;
			}
		}

		return log;
	}

	// Called at the END of a combatant's turn — decrement durations, clear expired
	public static List<string> ProcessTurnEnd(Combatant combatant)
	{
		var log = new List<string>();

		var effects = combatant.IsParty
			? combatant.Character.ActiveEffects
			: combatant.Monster.ActiveEffects;

		// Tick status effect durations
		var toRemove = new List<StatusEffect>();
		foreach (var effect in effects)
		{
			if (effect.Duration > 0)
			{
				effect.Duration--;
				if (effect.Duration <= 0)
				{
					toRemove.Add(effect);
					log.Add($"{combatant.Name} is no longer {effect.Type}.");
				}
			}
		}
		foreach (var e in toRemove)
			effects.Remove(e);

		// Tick combat ability cooldowns (party only — monsters use attack weights, not cooldowns)
		if (combatant.IsParty)
		{
			var cds = combatant.Character.CombatCooldowns;
			foreach (var key in new List<string>(cds.Keys))
			{
				if (cds[key] > 0) cds[key]--;
			}
		}

		return log;
	}

	// Called when a combatant takes damage — wakes sleepers
	public static List<string> OnDamageTaken(Combatant combatant)
	{
		var log = new List<string>();

		var effects = combatant.IsParty
			? combatant.Character.ActiveEffects
			: combatant.Monster.ActiveEffects;

		var sleep = effects.Find(e => e.Type == StatusType.Asleep);
		if (sleep != null)
		{
			effects.Remove(sleep);
			log.Add($"{combatant.Name} wakes up!");
		}

		return log;
	}

	// Clear combat-only effects when combat ends
	public static void ClearCombatEffects(List<Character> party)
	{
		foreach (var c in party)
			c.ActiveEffects.RemoveAll(e => e.ClearsAtCombatEnd);
	}

	private static void ApplyDamage(Combatant combatant, int damage)
	{
		if (combatant.IsParty)
		{
			combatant.Character.CurrentHP =
				System.Math.Max(0, combatant.Character.CurrentHP - damage);
			if (combatant.Character.CurrentHP <= 0)
				combatant.Character.Status = Status.Dead;
		}
		else
		{
			combatant.Monster.TakeDamage(damage);
		}
	}
}
