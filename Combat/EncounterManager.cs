using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class EncounterManager
{
	private Dictionary<string, EncounterInstance> _instances
		= new Dictionary<string, EncounterInstance>();
	public List<EncounterInstance> GetAllInstances() => _instances.Values.ToList();
	
	private int _nextId = 1;
	private Random _rng = new Random();

	// --- Creation ---
	// Instantiate a live encounter from a template
	public EncounterInstance CreateInstance(
		string dungeonId, string encounterId, string roomId,
		EncounterAttachment attachment)
	{
		var template = EncounterLoader.LoadEncounter(dungeonId, encounterId);
		if (template == null) return null;

		var instance = new EncounterInstance
		{
			InstanceId        = $"enc_{_nextId++}",
			SourceEncounterId = encounterId,
			RoomId            = roomId ?? "",
			Attachment        = attachment,
			Rewards           = template.Rewards
		};

		// Build live monsters from the template formation
		foreach (var row in template.Formation)
		{
			var monsterRow = new List<Monster>();
			foreach (var id in row)
			{
				var monster = MonsterLoader.LoadMonster(id);
				if (monster != null) monsterRow.Add(monster);
			}
			if (monsterRow.Count > 0)
				instance.Formation.Add(monsterRow);
		}

		if (instance.Formation.Count == 0) return null;

		_instances[instance.InstanceId] = instance;
		return instance;
	}

	public void PopulateDungeon(string dungeonId, List<string> roomIds)
	{
		GD.Print($"PopulateDungeon: {roomIds.Count} rooms");

		foreach (var roomId in roomIds)
		{
			var room = DungeonManager.LoadRoom(dungeonId, roomId);
			if (room?.Encounters == null)
			{
				GD.Print($"  {roomId}: no encounters list");
				continue;
			}

			GD.Print($"  {roomId}: {room.Encounters.Count} encounter entries");

			foreach (var entry in room.Encounters)
			{
				double roll = _rng.NextDouble();
				GD.Print($"    {entry.Id} chance {entry.Chance}, rolled {roll:F2}");
				if (roll > entry.Chance) continue;

				var inst = CreateInstance(dungeonId, entry.Id, roomId, EncounterAttachment.Permanent);
				GD.Print($"    created: {inst?.InstanceId ?? "FAILED"}");
				break;
			}
		}
	}

	// The live, uncleared encounter in a room (if any)
	public EncounterInstance GetRoomEncounter(string roomId)
	{
		return _instances.Values.FirstOrDefault(e =>
			e.RoomId == roomId
			&& e.Attachment != EncounterAttachment.Wandering
			&& !e.IsCleared);
	}

	public EncounterInstance GetInstance(string instanceId)
		=> _instances.TryGetValue(instanceId, out var e) ? e : null;

	public List<EncounterInstance> GetWandering()
		=> _instances.Values.Where(e =>
			e.Attachment == EncounterAttachment.Wandering && !e.IsCleared).ToList();

	// --- State transitions ---

	// Called after combat ends. Prunes dead; removes if cleared.
	public void ResolveAfterCombat(EncounterInstance instance, string currentRoomId)
	{
		if (instance == null) return;

		instance.PruneDead();

		if (instance.IsCleared || instance.Formation.Count == 0)
		{
			_instances.Remove(instance.InstanceId);
			return;
		}

		// Survivors remain. A wandering group that fought settles into the room
		// to lick its wounds; it resumes wandering once the time system exists.
		if (instance.Attachment == EncounterAttachment.Wandering)
		{
			instance.Attachment = EncounterAttachment.Temporary;
			instance.RoomId     = currentRoomId;
		}
	}

	// Attach a wandering group to a room (used when time system moves them)
	public void SetAttachment(string instanceId, EncounterAttachment attachment, string roomId)
	{
		var inst = GetInstance(instanceId);
		if (inst == null) return;
		inst.Attachment = attachment;
		inst.RoomId     = attachment == EncounterAttachment.Wandering ? "" : roomId;
	}

	public void Clear() => _instances.Clear();
}
