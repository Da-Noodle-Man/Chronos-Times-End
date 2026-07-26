using Godot;
using System;

public partial class SimpleSword : Area2D
{
	[Export] public AnimationPlayer animPlayer;
	public float speedMultiplier = 1.0f;
	
	private CollisionShape2D hitbox;
	private ColorRect visual;
	private bool hasDealtDamage = false;

	public override void _Ready()
	{
		hitbox = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		visual = GetNodeOrNull<ColorRect>("ColorRect");
		
		if (hitbox != null) hitbox.SetDeferred("disabled", true);
		if (visual != null) visual.Visible = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleInputs();
		UpdateSpeedMultiplier();
		CheckForOverlappingHits();
	}

	private void UpdateSpeedMultiplier()
	{
		speedMultiplier = 1.0f;
	}

	private void HandleInputs()
	{
		if (Input.IsActionJustPressed("attack"))
		{
			PerformAttack();
		}
		else
		{
			if (animPlayer != null)
			{
				bool isDoingAction = animPlayer.AssignedAnimation == "attack" && animPlayer.IsPlaying();
				if (!isDoingAction && animPlayer.AssignedAnimation != "idle")
				{
					animPlayer.Play("idle");
					if (hitbox != null) hitbox.SetDeferred("disabled", true);
					if (visual != null) visual.Visible = false;
					hasDealtDamage = false;
				}
			}
		}
	}

	private void PerformAttack()
	{
		if (animPlayer != null)
		{
			bool isAttacking = animPlayer.AssignedAnimation == "attack" && animPlayer.IsPlaying();
			if (!isAttacking)
			{
				hasDealtDamage = false;
				if (hitbox != null) hitbox.SetDeferred("disabled", false);
				if (visual != null) visual.Visible = true;
				animPlayer.Play("attack");
			}
		}
	}

	private void CheckForOverlappingHits()
	{
		bool isAttacking = animPlayer != null && animPlayer.AssignedAnimation == "attack" && animPlayer.IsPlaying();
		
		// The Fix: We strictly verify that the Area2D's internal Monitoring engine is online
		if (isAttacking && !hasDealtDamage && Monitoring)
		{
			Godot.Collections.Array<Area2D> currentOverlaps = GetOverlappingAreas();
			
			foreach (Area2D area in currentOverlaps)
			{
				if (area.IsInGroup("Boss"))
				{
					Node parent = area.GetParent();
					if (parent != null && parent.HasMethod("TakeDamage"))
					{
						parent.Call("TakeDamage", 4);
						hasDealtDamage = true;
						break;
					}
				}
			}
		}
	}
}
