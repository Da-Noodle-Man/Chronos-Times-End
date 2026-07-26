using Godot;
using System;

public partial class OrbOfTime : Area2D
{
	private Line2D targetLaser;
	private Timer adjustmentTimer;
	private Timer lockOnTimer;
	private ColorRect visualCore;
	private CollisionShape2D hitbox;
	private PointLight2D glowLight;

	private Node2D targetPlayer;
	private Vector2 fireDirection;
	private float travelSpeed = 0.0f;
	private float currentTurnSpeed = 4.5f;
	private Vector2 smoothedVelocity = Vector2.Zero;
	
	private enum OrbState { Spawning, Tracking, Locked, Firing }
	private OrbState currentState = OrbState.Spawning;

	public override void _Ready()
	{
		AddToGroup("BossAttack");
		
		targetLaser = GetNode<Line2D>("TargetLaser");
		adjustmentTimer = GetNode<Timer>("AdjustmentTimer");
		lockOnTimer = GetNode<Timer>("LockOnTimer");
		visualCore = GetNode<ColorRect>("VisualCore");
		hitbox = GetNode<CollisionShape2D>("CollisionShape2D");
		glowLight = GetNode<PointLight2D>("PointLight2D");
		
		lockOnTimer.WaitTime = 0.3f;
		
		targetLaser.Visible = false;
		hitbox.SetDeferred("disabled", true);
	}

	public void Initialize(Vector2 teleportPosition, float speed, float trackTime, Node2D player)
	{
		travelSpeed = speed;
		targetPlayer = player;
		adjustmentTimer.WaitTime = trackTime;
		
		if (speed >= 2300.0f) 
		{
			visualCore.Color = new Color(1.0f, 0.0f, 1.0f); 
		}
		else if (speed >= 1200.0f) 
		{
			visualCore.Color = new Color(1.0f, 0.6f, 0.0f); 
		}
		else 
		{
			visualCore.Color = new Color(0.0f, 1.0f, 1.0f); 
		}

		glowLight.Color = visualCore.Color;

		Tween teleportTween = GetTree().CreateTween();
		teleportTween.TweenInterval(0.8f);
		teleportTween.TweenCallback(Callable.From(() => StartTracking(teleportPosition)));
	}

	private void StartTracking(Vector2 newPos)
	{
		GlobalPosition = newPos;
		currentState = OrbState.Tracking;
		hitbox.SetDeferred("disabled", false);
		targetLaser.Visible = true;
		
		Vector2 initialDirection = (targetPlayer.GlobalPosition - GlobalPosition).Normalized();
		float offset = GD.Randf() > 0.5f ? 45.0f : -45.0f;
		Rotation = initialDirection.Angle() + Mathf.DegToRad(offset);
		smoothedVelocity = Vector2.Zero;
		
		adjustmentTimer.Start();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (targetPlayer == null) return;

		if (currentState == OrbState.Tracking)
		{
			Vector2 rawVelocity = Vector2.Zero;
			Variant velVariant = targetPlayer.Get("velocity");
			if (velVariant.VariantType == Variant.Type.Vector2)
			{
				rawVelocity = velVariant.AsVector2();
			}

			smoothedVelocity = smoothedVelocity.Lerp(rawVelocity, 6.0f * (float)delta);
			Vector2 predictionVelocity = smoothedVelocity.LimitLength(500.0f);

			float distanceToPlayer = GlobalPosition.DistanceTo(targetPlayer.GlobalPosition);
			float timeToImpact = Mathf.Min(distanceToPlayer / travelSpeed, 0.5f);
			
			Vector2 predictedPos = targetPlayer.GlobalPosition + (predictionVelocity * timeToImpact * 0.35f);
			Vector2 directionToTarget = (predictedPos - GlobalPosition).Normalized();
			float targetAngle = directionToTarget.Angle();

			float targetTurnSpeed = 6.0f;
			
			float angleDiff = Mathf.Abs(Mathf.AngleDifference(Rotation, targetAngle));
			if (angleDiff > 0.4f)
			{
				targetTurnSpeed *= 2.0f;
			}

			currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, targetTurnSpeed, 8.0f * (float)delta);
			Rotation = Mathf.LerpAngle(Rotation, targetAngle, currentTurnSpeed * (float)delta);
			
			Vector2 forwardDirection = Vector2.Right.Rotated(Rotation);
			
			var spaceState = GetWorld2D().DirectSpaceState;
			var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + forwardDirection * 5000.0f);
			
			Godot.Collections.Array<Rid> exclusions = new Godot.Collections.Array<Rid> { GetRid() };
			if (targetPlayer is CollisionObject2D playerCol)
			{
				exclusions.Add(playerCol.GetRid());
			}
			query.Exclude = exclusions;
			
			var result = spaceState.IntersectRay(query);
			
			if (result.Count > 0)
			{
				Vector2 hitPosition = (Vector2)result["position"];
				float distanceToWall = GlobalPosition.DistanceTo(hitPosition);
				targetLaser.SetPointPosition(1, new Vector2(distanceToWall, 0));
			}
			else
			{
				targetLaser.SetPointPosition(1, new Vector2(5000.0f, 0));
			}
		}
		else if (currentState == OrbState.Firing)
		{
			GlobalPosition += fireDirection * travelSpeed * (float)delta;
		}
	}

	private void ForceLockOn()
	{
		if (currentState == OrbState.Locked) return;
		
		currentState = OrbState.Locked;
		adjustmentTimer.Stop();
		fireDirection = Vector2.Right.Rotated(Rotation);
		
		Tween flickerTween = GetTree().CreateTween();
		flickerTween.TweenProperty(targetLaser, "default_color", new Color(1.0f, 0.0f, 0.0f, 0.0f), 0.05f);
		flickerTween.TweenProperty(targetLaser, "default_color", new Color(1.0f, 0.0f, 0.0f, 1.0f), 0.05f);
		flickerTween.SetLoops(3);
		
		lockOnTimer.Start();
	}

	public void OnAdjustmentTimerTimeout()
	{
		if (currentState == OrbState.Tracking)
		{
			ForceLockOn();
		}
	}

	public void OnLockOnTimerTimeout()
	{
		currentState = OrbState.Firing;
		targetLaser.Visible = false;
		
		SceneTreeTimer lifetimeTimer = GetTree().CreateTimer(5.0f);
		lifetimeTimer.Timeout += QueueFree;
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			if (body.HasMethod("Die"))
			{
				body.Call("Die", "Player was struck by an Orb of Time.");
			}
			QueueFree();
		}
		else if (body.IsInGroup("Environment") || body.IsInGroup("PrometheusFlame"))
		{
			QueueFree();
		}
	}

	public void OnAreaEntered(Area2D area)
	{
		if (area.IsInGroup("GluttonyTrap"))
		{
			QueueFree();
		}
	}
}
