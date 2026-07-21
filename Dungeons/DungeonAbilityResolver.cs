using Godot;

public static class DungeonAbilityResolver
{
	// Resolves an ability's effect on a party-member target, out of combat.
	public static void Resolve(Ability ability, Character caster, Character target)
	{
		switch (ability.EffectType)
		{
			case AbilityEffectType.Heal:
				int before = target.CurrentHP;
				target.CurrentHP = System.Math.Min(target.MaxHP, target.CurrentHP + ability.Power);
				int healed = target.CurrentHP - before;
				DungeonLog.Print($"{target.Name} was healed for {healed}.", DungeonLog.Healing);
				break;

			case AbilityEffectType.CureStatus:
				// Later: remove a status effect
				break;

			case AbilityEffectType.Buff:
				// Later: apply a buff
				break;

			// Damage abilities generally don't apply to party members out of combat
		}
	}
}
