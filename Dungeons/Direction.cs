using Godot;

public enum Direction { North, South, East, West, Up, Down }

public static class DirectionExtensions
{
	public static Direction Opposite(this Direction dir) => dir switch
	{
		Direction.North => Direction.South,
		Direction.South => Direction.North,
		Direction.East  => Direction.West,
		Direction.West  => Direction.East,
		Direction.Up    => Direction.Down,
		Direction.Down  => Direction.Up,
		_ => dir
	};

	// Unit offset in map space. X = east, Y = north (negative Y is north on screen),
	// Z = level (positive is up).
	public static Vector3I Offset(this Direction dir) => dir switch
	{
		Direction.North => new Vector3I(0, -1, 0),
		Direction.South => new Vector3I(0,  1, 0),
		Direction.East  => new Vector3I(1,  0, 0),
		Direction.West  => new Vector3I(-1, 0, 0),
		Direction.Up    => new Vector3I(0,  0, 1),
		Direction.Down  => new Vector3I(0,  0, -1),
		_ => Vector3I.Zero
	};

	// Rotate compass directions by quarter turns clockwise. Up/Down unaffected.
	// Used when placing authored chunks at an angle during procedural generation.
	public static Direction Rotate(this Direction dir, int quarterTurns)
	{
		if (dir == Direction.Up || dir == Direction.Down) return dir;

		var order = new[] { Direction.North, Direction.East, Direction.South, Direction.West };
		int index = System.Array.IndexOf(order, dir);
		if (index < 0) return dir;

		int rotated = ((index + quarterTurns) % 4 + 4) % 4;
		return order[rotated];
	}
}
