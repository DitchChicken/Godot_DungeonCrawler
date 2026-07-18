using Godot;

public static class DungeonItemUse
{
	// The item pending use, and where it came from (for consumption)
	private static Equipment _pendingItem;
	private static Character _sourceCharacter;   // whose inventory it's in
	private static InventoryDragData.SourceType _sourceType;
	private static int _sourceSlot;
	private static Ability _pendingAbility;

	// Reference to whatever drives target selection (set by the dungeon scene)
	public static DungeonTargetController TargetController;

	public static void BeginItemUse(
		Equipment item, Character sourceCharacter,
		InventoryDragData.SourceType sourceType, int slotIndex)
	{
		var ability = AbilityLoader.LoadAbility(item.UseAbility);
		if (ability == null || !ability.CanUseIn("Dungeon")) return;

		_pendingItem      = item;
		_sourceCharacter  = sourceCharacter;
		_sourceType       = sourceType;
		_sourceSlot       = slotIndex;
		_pendingAbility   = ability;

		// Ask the target controller to begin selection.
		// Valid targets determined by the ability's target type.
		TargetController?.BeginPartyTargetSelect(ability, item.Name, OnTargetChosen);
	}

	// Called when the player picks a valid party member
	private static void OnTargetChosen(Character target)
	{
		if (_pendingItem == null || _pendingAbility == null) return;

		// Resolve the ability on the target (out of combat — no combat state)
		DungeonAbilityResolver.Resolve(_pendingAbility, _sourceCharacter, target);

		// Consume one from the source inventory
		_sourceCharacter?.PersonalInventory.RemoveItem(_pendingItem, 1);

		// Refresh the character sheet so the item count / target HP updates
		var sheet = ((SceneTree)Engine.GetMainLoop()).Root
			.GetNodeOrNull<CharacterSheet>("/root/CharacterSheet");
		sheet?.RefreshInventory();

		var hud = ((SceneTree)Engine.GetMainLoop()).Root
			.GetNodeOrNull<PartyHUD>("/root/PartyHud");
		hud?.Refresh();

		Clear();
	}

	public static void Cancel() => Clear();

	private static void Clear()
	{
		_pendingItem     = null;
		_sourceCharacter = null;
		_pendingAbility  = null;
	}
}
