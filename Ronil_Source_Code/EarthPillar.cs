using Godot;
using System;

public partial class EarthPillar : Area2D
{
	private CollisionShape2D wallCollision;
	private CollisionShape2D damageCollision;
	private Node2D visualWrapper;
	private bool isLethal = false;

	public override void _Ready()
	{
		AddToGroup("BossAttack");

		wallCollision = GetNodeOrNull<CollisionShape2D>("WallPhysics/CollisionShape2D");
		damageCollision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		visualWrapper = GetNodeOrNull<Node2D>("VisualWrapper");

		if (visualWrapper != null)
		{
			visualWrapper.Scale = new Vector2(0.0f, 0.0f);
		}

		if (damageCollision != null) damageCollision.SetDeferred("disabled", true);
		if (wallCollision != null) wallCollision.SetDeferred("disabled", true);

		Tween tween = GetTree().CreateTween();
		
		if (visualWrapper != null)
		{
			tween.TweenProperty(visualWrapper, "scale", new Vector2(1.0f, 1.0f), 0.10f)
				 .SetTrans(Tween.TransitionType.Back)
				 .SetEase(Tween.EaseType.Out);
		}
			 
		tween.TweenCallback(Callable.From(() => ActivatePillar()));
		
		tween.TweenInterval(0.40f);
		
		tween.TweenCallback(Callable.From(() => DeactivatePillar()));
		
		if (visualWrapper != null)
		{
			tween.TweenProperty(visualWrapper, "scale", new Vector2(0.0f, 0.0f), 0.15f)
				 .SetTrans(Tween.TransitionType.Quad)
				 .SetEase(Tween.EaseType.In);
		}
			 
		tween.TweenCallback(Callable.From(() => QueueFree()));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isLethal)
		{
			Godot.Collections.Array<Area2D> areas = GetOverlappingAreas();
			foreach (Area2D area in areas)
			{
				if (area.Name == "PlayerHurtbox")
				{
					Node parent = area.GetParent();
					if (parent != null && parent.HasMethod("TakeDamage"))
					{
						parent.Call("TakeDamage", 100);
					}
				}
			}
		}
	}

	private void ActivatePillar()
	{
		isLethal = true;
		if (damageCollision != null) damageCollision.SetDeferred("disabled", false);
		if (wallCollision != null) wallCollision.SetDeferred("disabled", false);
	}

	private void DeactivatePillar()
	{
		isLethal = false;
		if (damageCollision != null) damageCollision.SetDeferred("disabled", true);
		if (wallCollision != null) wallCollision.SetDeferred("disabled", true);
	}
}
