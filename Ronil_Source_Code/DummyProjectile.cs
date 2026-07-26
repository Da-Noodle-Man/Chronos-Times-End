using Godot;
using System;

public partial class DummyProjectile : Area2D
{
	private float speed = 400.0f;

	public override void _PhysicsProcess(double delta)
	{
		Position += new Vector2(-speed * (float)delta, 0);
	}
}
