using Godot;
using System;

public partial class GluttonyZone : Area2D
{
	private ColorRect warningVisual;
	private Timer telegraphTimer;
	private Timer regenTimer;
	private CollisionShape2D killZone;

	public override void _Ready()
	{
		AddToGroup("GluttonyTrap");
		
		warningVisual = GetNode<ColorRect>("WarningVisual");
		telegraphTimer = GetNode<Timer>("TelegraphTimer");
		regenTimer = GetNode<Timer>("RegenTimer");
		killZone = GetNode<CollisionShape2D>("KillZone");

		warningVisual.Color = new Color(1.0f, 0.0f, 0.0f, 0.0f);
		killZone.SetDeferred("disabled", true);

		Tween flickerTween = GetTree().CreateTween();
		flickerTween.TweenProperty(warningVisual, "color", new Color(1.0f, 0.0f, 0.0f, 0.6f), 0.1f);
		flickerTween.TweenProperty(warningVisual, "color", new Color(1.0f, 0.0f, 0.0f, 0.1f), 0.1f);
		flickerTween.SetLoops(4);

		telegraphTimer.WaitTime = 0.8f;
		telegraphTimer.Start();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (killZone.Disabled) return;

		Godot.Collections.Array<Node2D> bodies = GetOverlappingBodies();
		foreach (Node2D body in bodies)
		{
			if (body.IsInGroup("Player"))
			{
				if (IsBodyFullyInside(body))
				{
					if (body.HasMethod("Die"))
					{
						body.Call("Die", "player has fallen into the abyss");
					}
				}
			}
			else if (body.IsInGroup("PrometheusFlame"))
			{
				if (body.HasMethod("Consume"))
				{
					body.Call("Consume");
				}
			}
		}
	}

	private bool IsBodyFullyInside(Node2D body)
	{
		if (killZone.Shape is RectangleShape2D holeShape)
		{
			CollisionShape2D playerCollider = body.GetNodeOrNull<CollisionShape2D>("PhysicsCollider");
			
			if (playerCollider != null && playerCollider.Shape is RectangleShape2D playerShape)
			{
				Rect2 holeRect = new Rect2(GlobalPosition - (holeShape.Size / 2.0f), holeShape.Size);
				Rect2 playerRect = new Rect2(body.GlobalPosition - (playerShape.Size / 2.0f), playerShape.Size);

				return holeRect.Encloses(playerRect);
			}
		}
		return false;
	}

	public void OnTelegraphTimerTimeout()
	{
		warningVisual.Color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
		killZone.SetDeferred("disabled", false);
		regenTimer.WaitTime = 2.0f;
		regenTimer.Start();
	}

	public void OnRegenTimerTimeout()
	{
		QueueFree();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Flame"))
		{
			if (body.HasMethod("Consume"))
			{
				body.Call("Consume");
			}
		}
	}
}
