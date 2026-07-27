using System.Collections.Generic;
using System.Linq;

public enum EncounterAttachment
{
	Permanent,   // lives in a room, stays there (room-authored encounters)
	Temporary,   // in a room for now (wandering monsters resting/healing after a fight)
	Wandering    // roaming the dungeon, not in any room
}

public enum Disposition { Hostile, Neutral }
public enum InteractionRequirement { Mandatory, Optional }

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

	public Disposition Disposition { get; set; } = Disposition.Hostile;
	public InteractionRequirement Requirement { get; set; } = InteractionRequirement.Mandatory;
	public string DialogueId { get; set; } = "";
	public string SceneId { get; set; } = "";
	
	// Overlay image drawn over the room while this encounter is present
	public string OverlayImage { get; set; } = "";
	public float OverlayX { get; set; } = 0.5f;   // fractional placement
	public float OverlayY { get; set; } = 0.5f;
	public float OverlayScale { get; set; } = 1.0f;

	// Does this encounter block room progress (search/loot) right now?
	public bool BlocksRoom => Requirement == InteractionRequirement.Mandatory;

	// Strip out dead monsters so re-fights only include survivors
	public void PruneDead()
	{
		foreach (var row in Formation)
			row.RemoveAll(m => !m.IsAlive);
		Formation.RemoveAll(row => row.Count == 0);
	}
}
