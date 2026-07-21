using Godot;

public static class DungeonAbilityUse
{
	private static Character _caster;
	private static Ability _pendingAbility;

	public static AbilityMenu Menu;                        // set by dungeon scene
	public static DungeonTargetController TargetController; // set by dungeon scene

	public static void OpenMenu(Character caster, AbilityType type)
	{
		_caster = caster;
		Menu?.OpenForDungeon(caster, type, OnAbilityChosen);
	}

	private static void OnAbilityChosen(string abilityId)
	{
		//GD.Print($"OnAbilityChosen: {abilityId}");

		var ability = AbilityLoader.LoadAbility(abilityId);
		if (ability == null) { GD.Print("  ability null"); return; }
		if (_caster == null) { GD.Print("  caster null"); return; }

		//GD.Print($"  canUseInDungeon: {_caster.CanUseAbilityInDungeon(ability)}");
		if (!_caster.CanUseAbilityInDungeon(ability)) return;

		_pendingAbility = ability;

		if (ability.TargetType == AbilityTargetType.Self)
		{
			Resolve(_caster);
			return;
		}

		//GD.Print($"  TargetController null? {TargetController == null}");
		TargetController?.BeginPartyTargetSelect(ability, ability.Name, Resolve);
	}
	
	private static void Resolve(Character target)
	{
		if (_pendingAbility == null || _caster == null) return;

		// Spend costs
		_caster.CurrentMana -= _pendingAbility.ManaCost;
		if (_pendingAbility.HealthCost > 0)
			_caster.CurrentHP -= _pendingAbility.HealthCost;

		// Set exploration cooldown (combat cooldown ignored out of combat)
		if (_pendingAbility.ExplorationCooldown > 0)
			_caster.ExplorationCooldowns[_pendingAbility.Id] =
				DungeonClock.Current + _pendingAbility.ExplorationCooldown;

		DungeonAbilityResolver.Resolve(_pendingAbility, _caster, target);

		var hud = ((SceneTree)Engine.GetMainLoop()).Root
			.GetNodeOrNull<PartyHUD>("/root/PartyHud");
		hud?.Refresh();

		_pendingAbility = null;
	}

	public static void Cancel() => _pendingAbility = null;
}
