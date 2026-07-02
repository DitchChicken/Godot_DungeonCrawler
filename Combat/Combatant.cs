public class Combatant
{
	public Character Character { get; set; }
	public Monster Monster { get; set; }
	public int Initiative { get; set; }
	public bool IsParty { get; set; }

	public bool IsAlive => IsParty
		? Character.IsAlive
		: Monster.IsAlive;

	public string Name => IsParty
		? Character.Name
		: Monster.CombatLabel;
		
	public void AddEffect(StatusEffect effect)
	{
		if (IsParty) Character.AddStatusEffect(effect);
		else Monster.AddStatusEffect(effect);
	}
}
