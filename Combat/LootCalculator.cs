using Godot;
using System;
using System.Collections.Generic;

public static class LootCalculator
{
	public static LootResult Calculate(
		CombatState combatState,
		EncounterData encounterData,
		Random rng)
	{
		var result = new LootResult();

		// --- Experience ---
		int totalXP = 0;
		foreach (var monster in combatState.AllMonsters)
			totalXP += monster.ExperienceReward;
		result.ExperiencePerSurvivor = totalXP;

		// --- Monster drops ---
		foreach (var monster in combatState.AllMonsters)
		{
			if (monster.Drops == null) continue;
			foreach (var drop in monster.Drops)
			{
				if (rng.NextDouble() > drop.Chance) continue;
				int count = rng.Next(drop.CountMin, drop.CountMax + 1);
				var item  = EquipmentLoader.LoadEquipment(drop.Id);
				if (item != null) result.AddItem(item, count);
			}
		}

		// --- Encounter reward pool ---
		if (encounterData?.Rewards != null)
		{
			var rewards = encounterData.Rewards;

			// Gold range
			if (rewards.GoldMax > 0)
			{
				int gold = rng.Next(rewards.GoldMin, rewards.GoldMax + 1);
				if (gold > 0)
				{
					var goldItem = EquipmentLoader.LoadEquipment("Gold");
					if (goldItem != null) result.AddItem(goldItem, gold);
				}
			}

			// Item rewards
			if (rewards.Items != null)
			{
				foreach (var reward in rewards.Items)
				{
					if (rng.NextDouble() > reward.Chance) continue;
					int count = rng.Next(reward.CountMin, reward.CountMax + 1);
					var item  = EquipmentLoader.LoadEquipment(reward.Id);
					if (item != null) result.AddItem(item, count);
				}
			}
		}

		return result;
	}
}
