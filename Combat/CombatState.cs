using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CombatState
{
	public enum Phase { Initiative, PlayerTurn, EnemyTurn, Victory, Defeat }
	
	// Combatants
	public List<Character> Party { get; private set; }
	public List<List<Monster>> EnemyFormation { get; private set; } // rows of monsters
	public List<Monster> AllMonsters => EnemyFormation.SelectMany(r => r).ToList();

	// Turn tracking
	public int CurrentRound { get; private set; } = 1;	
	public List<Combatant> TurnOrder { get; private set; } = new List<Combatant>();
	public int CurrentTurnIndex { get; private set; } = 0;
	public Combatant CurrentCombatant => TurnOrder.Count > 0 
		? TurnOrder[CurrentTurnIndex] : null;

	public Phase CurrentPhase { get; private set; } = Phase.Initiative;

	// Combat log
	public List<string> Log { get; private set; } = new List<string>();

	// Monster letter assignment (Skeleton A, Skeleton B etc)
	private Dictionary<string, int> _monsterLetterCount = new Dictionary<string, int>();

	private Random _rng = new Random();

	public CombatState(List<Character> party, List<List<Monster>> formation)
	{
		Party          = party;
		EnemyFormation = formation;
		AssignMonsterLetters();
	}

	// Assign A/B/C letters to monsters of the same type
	private void AssignMonsterLetters()
	{
		foreach (var monster in AllMonsters)
		{
			if (!_monsterLetterCount.ContainsKey(monster.Name))
				_monsterLetterCount[monster.Name] = 0;

			int index = _monsterLetterCount[monster.Name]++;
			monster.CombatLabel = $"{monster.Name} {(char)('A' + index)}";
		}
	}

	public void RollInitiative()
	{
		TurnOrder.Clear();

		foreach (var character in Party)
		{
			character.CombatCooldowns.Clear();			
			if (!character.IsAlive) continue;
			var combatant = new Combatant { Character = character, IsParty = true };
			combatant.Initiative = RollInitiativeFor(combatant);
			TurnOrder.Add(combatant);

			int equipMod = 0;
			foreach (var item in character.EquippedItems.Values)
				equipMod += item.InitiativeModifier;
			string logMsg = $"{character.Name} rolls initiative: DEX({character.Dexterity})";
			if (equipMod != 0) logMsg += $" + equip({equipMod})";
			logMsg += $" = {combatant.Initiative}";
			AddLog(logMsg);
		}

		foreach (var monster in AllMonsters)
		{
			if (!monster.IsAlive) continue;
			var combatant = new Combatant { Monster = monster, IsParty = false };
			combatant.Initiative = RollInitiativeFor(combatant);
			TurnOrder.Add(combatant);
			AddLog($"{monster.CombatLabel} rolls initiative: {combatant.Initiative}");
		}

		TurnOrder = TurnOrder
			.OrderByDescending(c => c.Initiative)
			.ThenByDescending(c => c.IsParty ? 1 : 0)
			.ToList();

		AddLog("--- Combat Begins ---");
		CurrentPhase = TurnOrder[0].IsParty ? Phase.PlayerTurn : Phase.EnemyTurn;
	}

	public void NextTurn()
	{
		// End-of-turn processing for the combatant who just acted
		if (CurrentCombatant != null)
		{
			var endLog = StatusProcessor.ProcessTurnEnd(CurrentCombatant);
			foreach (var msg in endLog) AddLog(msg);
		}

		int previousIndex = CurrentTurnIndex;
		do
		{
			CurrentTurnIndex = (CurrentTurnIndex + 1) % TurnOrder.Count;
		}
		while (!TurnOrder[CurrentTurnIndex].IsAlive);

		if (CurrentTurnIndex <= previousIndex)
			CurrentRound++;

		// Start-of-turn processing for the new combatant
		var startLog = StatusProcessor.ProcessTurnStart(CurrentCombatant, out bool skipTurn);
		foreach (var msg in startLog) AddLog(msg);

		CurrentPhase = CurrentCombatant.IsParty ? Phase.PlayerTurn : Phase.EnemyTurn;

		// If the combatant can't act, skip straight to the next turn
		if (skipTurn)
		{
			CheckVictoryDefeat();
			if (CurrentPhase != Phase.Victory && CurrentPhase != Phase.Defeat)
			{
				AddLog($"--- {CurrentCombatant.Name}'s turn is skipped ---");
				NextTurn(); // recurse to next combatant
				return;
			}
		}
	}

	public int ResolveAttack(Combatant attacker, Combatant defender)
	{
		float rawDamage   = 0;
		Attack monsterAttack = null;

		if (attacker.IsParty)
		{
			var weapon = attacker.Character.GetEquipped(EquipmentSlot.WeaponMain);
			if (weapon != null)
			{
				rawDamage  = _rng.Next(weapon.BaseDamageMin + weapon.MagicBonus,
									   weapon.BaseDamageMax + weapon.MagicBonus + 1);
				rawDamage += attacker.Character.Strength / 2f;
			}
			else
				rawDamage = _rng.Next(1, 3);
		}
		else
		{
			monsterAttack = AttackLoader.SelectWeightedAttack(attacker.Monster.Attacks, _rng);
			if (monsterAttack != null)
				rawDamage = _rng.Next(monsterAttack.BaseDamageMin, monsterAttack.BaseDamageMax + 1);
			else
				rawDamage = _rng.Next(1, 4);
			if (monsterAttack != null
				&& !string.IsNullOrEmpty(monsterAttack.StatusEffect)
				&& _rng.NextDouble() < monsterAttack.StatusChance)
			{
				if (Enum.TryParse<StatusType>(monsterAttack.StatusEffect, out var statusType))
				{
					int duration = 3; // default; could come from the attack JSON later
					defender.AddEffect(new StatusEffect(statusType, duration));
					AddLog($"{defender.Name} is {statusType}!");
				}
			}
		}

		int defenderAC = defender.IsParty
			? defender.Character.TotalArmorClass()
			: defender.Monster.ArmorClass;

		rawDamage = System.Math.Max(0, rawDamage - defenderAC);
		int damage = ProbabilisticRound(rawDamage);

		// Log damage FIRST
		AddLog($"{attacker.Name} hits {defender.Name} for {damage} damage.");

		// Apply damage
		if (defender.IsParty)
		{
			defender.Character.CurrentHP =
				System.Math.Max(0, defender.Character.CurrentHP - damage);

			// Death/unconscious AFTER damage log
			if (defender.Character.CurrentHP <= 0)
			{
				defender.Character.Status = Status.Dead;
				AddLog($"{defender.Character.Name} is knocked unconscious!");
			}

			// Status effect
			if (monsterAttack != null
				&& !string.IsNullOrEmpty(monsterAttack.StatusEffect)
				&& _rng.NextDouble() < monsterAttack.StatusChance)
			{
				if (Enum.TryParse<Status>(monsterAttack.StatusEffect, out var status))
				{
					defender.Character.Status = status;
					AddLog($"{defender.Character.Name} is {status}!");
				}
			}
		}
		else
		{
			defender.Monster.TakeDamage(damage);
			// Death AFTER damage log
			if (!defender.Monster.IsAlive)
				AddLog($"{defender.Monster.CombatLabel} is defeated!");
		}

		var wakeLog = StatusProcessor.OnDamageTaken(defender);
		foreach (var msg in wakeLog) AddLog(msg);
		
		return damage;
	}

	// Probabilistic rounding — 14.273 = 14 with 27.3% chance of 15
	public int ProbabilisticRound(float value)
	{
		int floor    = (int)value;
		float chance = value - floor;
		return _rng.NextDouble() < chance ? floor + 1 : floor;
	}

	public void AddLog(string message) => Log.Add(message);

	// Victory/Defeat checks
	public bool IsVictory => AllMonsters.All(m => !m.IsAlive);
	public bool IsDefeat  => Party.All(c => !c.IsAlive);

	public void CheckVictoryDefeat()
	{
		if (IsVictory)
		{
			CurrentPhase = Phase.Victory;
			AddLog("--- Victory! ---");
		}
		else if (IsDefeat)
		{
			CurrentPhase = Phase.Defeat;
			AddLog("--- Defeat! ---");
		}
	}
	
	public int GetPartyRow(Character character)
	{
		int index = Party.IndexOf(character);
		if (index < 0) return -1;
		return index < 3 ? 0 : 1; // 0 = front, 1 = back
	}

	public List<Character> GetPartyRow(int row)
	{
		var result = new List<Character>();
		int start  = row == 0 ? 0 : 3;
		int end    = row == 0 ? 3 : 6;
		for (int i = start; i < end && i < Party.Count; i++)
			result.Add(Party[i]);
		return result;
	}

	public List<Character> GetValidSwapTargets(Character character)
	{
		int currentRow = GetPartyRow(character);
		int oppositeRow = currentRow == 0 ? 1 : 0;

		var targets = new List<Character>();
		var opposite = GetPartyRow(oppositeRow);

		// Check if this character is the last one standing
		int livingCount = 0;
		foreach (var c in Party)
			if (c.IsAlive) livingCount++;

		if (livingCount <= 1)
		{
			// Last man standing — allow moving to any empty slot in opposite row
			foreach (var c in opposite)
				targets.Add(c); // includes dead
			return targets;
		}

		// Normal case — only living members in opposite row
		foreach (var c in opposite)
			if (c.IsAlive) targets.Add(c);

		return targets;
	}

	public void SwapRows(Character a, Character b)
	{
		int indexA = Party.IndexOf(a);
		int indexB = Party.IndexOf(b);
		if (indexA < 0 || indexB < 0) return;

		Party[indexA] = b;
		Party[indexB] = a;

		AddLog($"{a.Name} and {b.Name} switch rows.");
	}
	
	public bool IsFrontRowWiped()
	{
		for (int i = 0; i < 3 && i < Party.Count; i++)
			if (Party[i].IsAlive) return false;
		return true;
	}
	
	public void AutoPromoteBackRow()
	{
		if (!IsFrontRowWiped()) return;

		// Find first living back row member and swap to front
		for (int back = 3; back < 6 && back < Party.Count; back++)
		{
			if (!Party[back].IsAlive) continue;

			// Find first empty/dead front slot
			for (int front = 0; front < 3; front++)
			{
				if (Party[front].IsAlive) continue;
				SwapRows(Party[back], Party[front]);
				AddLog($"{Party[front].Name} moves to the front row!");
				break;
			}
			break;
		}
	}
	
	// Roll initiative for a single combatant — reusable for combat start and fleeing
	public int RollInitiativeFor(Combatant combatant)
	{
		int roll = _rng.Next(1, 11); // 1d10

		if (combatant.IsParty)
		{
			int equipMod = 0;
			foreach (var item in combatant.Character.EquippedItems.Values)
				equipMod += item.InitiativeModifier;
			return combatant.Character.Dexterity + roll + equipMod;
		}
		else
		{
			return roll + combatant.Monster.Initiative;
		}
	}
	
	// Result of a flee attempt
	public class FleeResult
	{
		public bool Success { get; set; }
		public List<(Combatant attacker, Combatant defender, int damage)> Hits { get; set; }
			= new List<(Combatant, Combatant, int)>();
	}

	public class FleeRolls
	{
		public Dictionary<Character, int> PartyInit = new Dictionary<Character, int>();
		public Dictionary<Monster, int>   EnemyInit = new Dictionary<Monster, int>();
	}

	public FleeRolls RollFleeInitiative()
	{
		var rolls = new FleeRolls();
		AddLog("The party attempts to flee!");

		foreach (var c in Party)
		{
			if (!c.IsAlive) continue;
			var combatant = new Combatant { Character = c, IsParty = true };
			rolls.PartyInit[c] = RollInitiativeFor(combatant);
		}

		foreach (var m in AllMonsters)
		{
			if (!m.IsAlive) continue;
			var combatant = new Combatant { Monster = m, IsParty = false };
			rolls.EnemyInit[m] = RollInitiativeFor(combatant);
		}

		// Hardmode TODO: Add flee state and keep low init characters in combat?
		// General TODO: Combat is marked completed at trigger, do we want to track the
		//    remaining monsters and make it a new (likely) combat upon reentry to room?
		return rolls;
	}
	
	// Resolves an ability from caster onto a single target.
	// Returns a log-friendly result amount (heal or damage).
	public int ResolveAbility(Combatant caster, Ability ability, Combatant target, bool freeCost = false)
	{
		if (caster.IsParty && !freeCost)
		{
			caster.Character.CurrentMana -= ability.ManaCost;
			if (ability.HealthCost > 0)
				caster.Character.CurrentHP -= ability.HealthCost;
			if (ability.CombatCooldown > 0)
				caster.Character.CombatCooldowns[ability.Id] = ability.CombatCooldown;
		}

		int amount = 0;

		switch (ability.EffectType)
		{
			case AbilityEffectType.Heal:
				amount = ability.Power;
				if (target.IsParty)
				{
					int before = target.Character.CurrentHP;
					target.Character.CurrentHP = System.Math.Min(
						target.Character.MaxHP,
						target.Character.CurrentHP + amount);
					amount = target.Character.CurrentHP - before; // actual healed (no overheal)
				}
				AddLog($"{caster.Name} heals {target.Name} for {amount} HP.");
				break;

			case AbilityEffectType.Damage:
				// (built out when we add attack spells)
				break;
		}

		return amount;
	}
}
