using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
	// Party & Stable
	public List<Character> Party  = new List<Character>();
	public List<Character> Stable = new List<Character>();

	public Inventory PartyVault = new Inventory(1024, 0, 0);
	
	// Economy
	public int Gold = 1000;

	// Stash
	// public List<Item> Stash = new List<Item>();  // wire in when ready

	//Dungeon State
	public string CurrentDungeon = "";
	public RoomData CurrentRoom = null;
	public Dictionary<string, DungeonState> DungeonStates = new Dictionary<string, DungeonState>();

	// Current combat encounter
	public List<List<string>> CurrentEncounter = new List<List<string>>();

	public void SetEncounter(List<List<string>> formation)
	{
		CurrentEncounter = formation;
	}

	// Party management
	public bool AddToParty(Character character)
	{
		if (Party.Count >= 6) return false;
		if (Party.Contains(character)) return false;
		character.Location = Location.Party;
		Party.Add(character);
		return true;
	}

	public void RemoveFromParty(Character character)
	{
		character.Location = Location.Stable;
		Party.Remove(character);
		// character remains in Stable, no need to re-add
	}

	public void AddToStable(Character character)
	{
		character.Location = Location.Stable;
		if (Stable.Contains(character)) return;
		Stable.Add(character);
	}
	
	public override void _Ready()
	{
		var roster = CharacterLoader.LoadRoster();
		foreach (var character in roster)
			AddToStable(character);
			
		if (DebugFlags.AutoFormPartyOnEmbark)
			AutoFormParty();
	}
	
	public void ReturnToTown()
	{
		CurrentDungeon = "";
		CurrentRoom = null;
		DungeonStates.Clear();  // wipes ALL dungeon states, fresh run next entry
	}
	
	public DungeonState GetDungeonState(string dungeonId)
	{
		if (!DungeonStates.ContainsKey(dungeonId))
			DungeonStates[dungeonId] = new DungeonState();
		return DungeonStates[dungeonId];
	}	
	
	private void AutoFormParty()
	{
		if (Party.Count > 0) return; // don't overwrite existing party

		var rng       = new System.Random();
		var available = new System.Collections.Generic.List<Character>(Stable);
		int n         = available.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			(available[k], available[n]) = (available[n], available[k]);
		}

		int count = System.Math.Min(6, available.Count);
		for (int i = 0; i < count; i++)
			AddToParty(available[i]);

		GD.Print($"Debug: Auto-formed party with {Party.Count} members");
	}
}
