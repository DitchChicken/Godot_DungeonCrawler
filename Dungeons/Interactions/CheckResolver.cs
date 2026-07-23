using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum CheckResult { CriticalSuccess, Success, Failure, CriticalFailure, NoQualifiedCharacter }

public class CheckOutcomeInfo
{
	public CheckResult Result;
	public Character Attempter;
	public int Roll;              // summed dice after rerolls
	public int Total;            // roll + stat mod + equipment mod
	public int StatModifier;
}

public static class CheckResolver
{
	private static Random _rng = new Random();

	public static CheckOutcomeInfo Resolve(Check check, GameState gs)
	{
		var attempter = PickAttempter(check, gs);
		if (attempter == null)
			return new CheckOutcomeInfo { Result = CheckResult.NoQualifiedCharacter };

		int domainLevel = attempter.GetDomainLevel(check.Domain);
		int roll       = RollWithRerolls(domainLevel);

		int statValue  = GetStat(attempter, check.Stat);
		int statMod    = Character.StatModifier(statValue);
		int equipMod   = GetEquipmentModifier(attempter, check); // TODO: real equipment mods
		int total      = roll + statMod + equipMod;

		CheckResult result;
		if (total >= check.Difficulty)
		{
			result = (check.CriticalThreshold >= 0 && total >= check.Difficulty + check.CriticalThreshold)
				? CheckResult.CriticalSuccess
				: CheckResult.Success;
		}
		else
		{
			result = CheckResult.Failure;
		}

		return new CheckOutcomeInfo
		{
			Result       = result,
			Attempter    = attempter,
			Roll         = roll,
			Total        = total,
			StatModifier = statMod
		};
	}

	// Qualified = viable AND (level 0 check OR has the domain at required level)
	private static Character PickAttempter(Check check, GameState gs)
	{
		var qualified = gs.Party.Where(c =>
			c.CanAct() &&
			(check.Level == 0 || c.GetDomainLevel(check.Domain) >= check.Level));

		// Heuristic score: DomainLevel * 2 + StatModifier
		return qualified
			.OrderByDescending(c =>
				c.GetDomainLevel(check.Domain) * 2
				+ Character.StatModifier(GetStat(c, check.Stat)))
			.FirstOrDefault();
	}

	// Roll 3d6, then reroll the lowest die up to domainLevel times, keeping the better result each time
	private static int RollWithRerolls(int domainLevel)
	{
		var dice = new List<int> { D6(), D6(), D6() };

		for (int i = 0; i < domainLevel; i++)
		{
			int lowIndex = 0;
			for (int d = 1; d < dice.Count; d++)
				if (dice[d] < dice[lowIndex]) lowIndex = d;

			int reroll = D6();
			if (reroll > dice[lowIndex])   // keep the better
				dice[lowIndex] = reroll;
		}

		return dice.Sum();
	}

	private static int D6() => _rng.Next(1, 7);

	private static int GetEquipmentModifier(Character c, Check check)
	{
		// TODO: sum equipment bonuses relevant to this check's domain/stat
		return 0;
	}

	private static int GetStat(Character c, string stat) => stat.ToLower() switch
	{
		"strength"     => c.TotalStrength(),
		"intelligence" => c.TotalIntelligence(),
		"wisdom"       => c.TotalWisdom(),
		"dexterity"    => c.TotalDexterity(),
		"constitution" => c.TotalConstitution(),
		"charisma"     => c.TotalCharisma(),
		_ => 10
	};
}
