using Godot;

public static class SearchController
{
	public static SearchLevel Next(SearchLevel current) => current switch
	{
		SearchLevel.Unsearched => SearchLevel.Quick,
		SearchLevel.Quick      => SearchLevel.Thorough,
		_                      => SearchLevel.Thorough
	};

	public static bool CanSearch(RoomState rs) => rs.Searched != SearchLevel.Thorough;

	public static float TimeFor(RoomData room, SearchLevel next)
	{
		var s = room.Search;
		return next == SearchLevel.Quick
			? (s?.QuickTime    ?? 2.0f)
			: (s?.ThoroughTime ?? 8.0f);
	}

	public static string LabelFor(RoomData room, RoomState rs)
	{
		var next = Next(rs.Searched);
		string verb = next == SearchLevel.Quick ? "Quick Search" : "Thorough Search";
		return $"{verb} ({TimeFor(room, next):0.#} ticks)";
	}

	public static void Execute(RoomData room, RoomState rs, GameState gs)
	{
		var next = Next(rs.Searched);
		var interaction = next == SearchLevel.Quick ? room.Search?.Quick : room.Search?.Thorough;

		DungeonClock.Advance(gs, TimeFor(room, next), $"search: {next}");
		rs.Searched = next;

		if (interaction == null)
		{
			DungeonLog.Print(next == SearchLevel.Quick
				? "A quick look around turns up nothing."
				: "You search thoroughly, and find nothing of interest.",
				DungeonLog.Flavor);
			return;
		}

		// Time is charged above, so don't let the interaction charge again
		float saved = interaction.TimeCost;
		interaction.TimeCost = 0f;
		InteractionResolver.Execute(interaction, gs, rs);
		interaction.TimeCost = saved;

		foreach (var msg in InteractionResolver.LastMessages)
			DungeonLog.Print(msg, DungeonLog.Flavor);
	}
}
