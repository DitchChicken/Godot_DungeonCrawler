using System.Collections.Generic;
using System.Linq;

public enum EncounterAttachment
{
	Permanent,   // lives in a room, stays there (room-authored encounters)
	Temporary,   // in a room for now (wandering monsters resting/healing after a fight)
	Wandering    // roaming the dungeon, not in any room
}

public class EncounterInstance
{
	public string InstanceId { get; set; }        // unique per instance
	public string SourceEncounterId { get; set; } // template it came from
	public string RoomId { get; set; } = "";      // "" when Wandering

	public EncounterAttachment Attachment { get; set; } = EncounterAttachment.Permanent;

	// Live monsters — real HP, persists across flee/re-entry
	public List<List<Monster>> Formation { get; set; } = new List<List<Monster>>();

	// Rewards from the source template, granted on victory
	public EncounterRewards Rewards { get; set; }

	public IEnumerable<Monster> AllMonsters => Formation.SelectMany(r => r);

	public bool IsCleared => AllMonsters.All(m => !m.IsAlive);

	// Strip out dead monsters so re-fights only include survivors
	public void PruneDead()
	{
		foreach (var row in Formation)
			row.RemoveAll(m => !m.IsAlive);
		Formation.RemoveAll(row => row.Count == 0);
	}
}
