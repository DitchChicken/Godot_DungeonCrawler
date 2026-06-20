using Godot;

public partial class Party : CharacterBody2D
{
	[Export] public int TileSize = 64;

	private bool _isMoving = false;

	public override void _Ready()
	{
		// Snap to nearest tile center on startup
		Position = new Vector2(
			Mathf.Floor(Position.X / TileSize) * TileSize + TileSize / 2,
			Mathf.Floor(Position.Y / TileSize) * TileSize + TileSize / 2
		);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (_isMoving) return;

		Vector2 direction = Vector2.Zero;

		if (Input.IsActionJustPressed("ui_right")) direction = Vector2.Right;
		else if (Input.IsActionJustPressed("ui_left"))  direction = Vector2.Left;
		else if (Input.IsActionJustPressed("ui_down"))  direction = Vector2.Down;
		else if (Input.IsActionJustPressed("ui_up"))    direction = Vector2.Up;

		if (direction == Vector2.Zero) return;

		Vector2 targetPosition = Position + direction * TileSize;

		// Check for collision before moving
		KinematicCollision2D collision = MoveAndCollide(direction * TileSize, testOnly: true);
		if (collision != null) return; // Wall in the way, don't move

		Position = targetPosition;
	}
}
