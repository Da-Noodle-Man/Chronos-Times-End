using Godot;
using System;
using System.Collections.Generic;

public partial class ChronosAttacks : Node2D
{
	public bool IsAttacking = false;
	private ChronosBoss boss;
	
	private PackedScene gluttonyScene;
	private PackedScene enhancedGluttonyScene;
	private PackedScene orbScene;
	private PackedScene earthPillarScene;
	
	private Area2D sweepHitbox;
	private Area2D slamHitbox;
	private Node2D bossVisuals;
	private Area2D contactHitbox;
	
	public int ActiveOrbsCount = 0;

	public override void _Ready()
	{
		boss = GetParent<ChronosBoss>();
		
		gluttonyScene = ResourceLoader.Load<PackedScene>("res://GluttonyZone.tscn");
		enhancedGluttonyScene = ResourceLoader.Load<PackedScene>("res://EnhancedGluttonyZone.tscn");
		orbScene = ResourceLoader.Load<PackedScene>("res://OrbOfTime.tscn");
		earthPillarScene = ResourceLoader.Load<PackedScene>("res://EarthPillar.tscn");
		
		sweepHitbox = boss.GetNodeOrNull<Area2D>("SweepHitbox");
		slamHitbox = boss.GetNodeOrNull<Area2D>("SlamHitbox");
		bossVisuals = boss.GetNodeOrNull<Node2D>("BossVisuals");
		contactHitbox = boss.GetNodeOrNull<Area2D>("ContactHitbox");
		
		if (sweepHitbox != null)
		{
			sweepHitbox.SetDeferred("monitoring", false);
			sweepHitbox.SetDeferred("monitorable", false);
			sweepHitbox.Scale = new Vector2(0.0f, 1.0f);
			sweepHitbox.AddToGroup("BossAttack");
			
			CollisionShape2D sweepShape = sweepHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (sweepShape != null) sweepShape.SetDeferred("disabled", true);
			
			ColorRect clockHand = sweepHitbox.GetNodeOrNull<ColorRect>("ClockHandVisual");
			if (clockHand != null) clockHand.Visible = false;
		}

		if (slamHitbox != null)
		{
			slamHitbox.SetDeferred("monitoring", false);
			slamHitbox.SetDeferred("monitorable", false);
			slamHitbox.AddToGroup("BossAttack");
			
			CollisionShape2D slamShape = slamHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (slamShape != null) slamShape.SetDeferred("disabled", true);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (contactHitbox != null && contactHitbox.Monitoring)
		{
			Godot.Collections.Array<Node2D> bodies = contactHitbox.GetOverlappingBodies();
			foreach (Node2D body in bodies)
			{
				if (body.IsInGroup("Player") && body.HasMethod("Die"))
				{
					body.Call("Die", "Crushed by the Titan of Time.");
				}
			}
		}
	}

	private void SpawnGluttonyZoneAt(Vector2 targetPosition)
	{
		if (gluttonyScene == null) return;
		float safeDistance = 250.0f;
		float distanceToPlayer = boss.GlobalPosition.DistanceTo(targetPosition);
		if (distanceToPlayer < safeDistance)
		{
			Vector2 pushDirection = (targetPosition - boss.GlobalPosition).Normalized();
			if (pushDirection == Vector2.Zero) pushDirection = Vector2.Right;
			targetPosition = boss.GlobalPosition + (pushDirection * safeDistance);
		}
		Node2D gluttonyInstance = gluttonyScene.Instantiate<Node2D>();
		boss.GetParent().AddChild(gluttonyInstance);
		gluttonyInstance.GlobalPosition = targetPosition;
	}

	private void SpawnEnhancedGluttonyZoneAt(Vector2 targetPosition)
	{
		if (enhancedGluttonyScene == null) return;
		float safeDistance = 350.0f;
		float distanceToPlayer = boss.GlobalPosition.DistanceTo(targetPosition);
		if (distanceToPlayer < safeDistance)
		{
			Vector2 pushDirection = (targetPosition - boss.GlobalPosition).Normalized();
			if (pushDirection == Vector2.Zero) pushDirection = Vector2.Right;
			targetPosition = boss.GlobalPosition + (pushDirection * safeDistance);
		}
		Node2D gluttonyInstance = enhancedGluttonyScene.Instantiate<Node2D>();
		boss.GetParent().AddChild(gluttonyInstance);
		gluttonyInstance.GlobalPosition = targetPosition;
	}

	public void ExecuteGluttony()
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player != null)
		{
			SpawnGluttonyZoneAt(player.GlobalPosition);
		}
	}

	public async void ExecuteEnhancedGluttony()
	{
		IsAttacking = true;
		for (int i = 0; i < 3; i++)
		{
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
			Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
			if (player != null)
			{
				SpawnEnhancedGluttonyZoneAt(player.GlobalPosition);
			}
			await ToSignal(GetTree().CreateTimer(0.85f), SceneTreeTimer.SignalName.Timeout);
		}
		IsAttacking = false;
		if (IsInsideTree()) boss.Brain.ResumeCooldown(1.2f);
	}

	public async void ExecuteOrbsOfTime(int orbCount)
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player != null && orbScene != null)
		{
			IsAttacking = true;
			float[] speeds = { 600.0f, 1200.0f, 2300.0f };
			float[] trackTimes = { 1.0f, 2.0f, 3.0f };
			
			for (int j = 0; j < trackTimes.Length; j++)
			{
				int randIdx = GD.RandRange(0, trackTimes.Length - 1);
				float temp = trackTimes[j];
				trackTimes[j] = trackTimes[randIdx];
				trackTimes[randIdx] = temp;
			}
			
			ActiveOrbsCount = 0;
			List<Vector2> usedPositions = new List<Vector2>();

			for (int i = 0; i < orbCount; i++)
			{
				OrbOfTime orbInstance = orbScene.Instantiate<OrbOfTime>();
				boss.GetParent().AddChild(orbInstance);
				
				float spawnAngle = i * (Mathf.Tau / orbCount);
				Vector2 spawnPos = boss.GlobalPosition + new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle)) * 150.0f;
				orbInstance.GlobalPosition = spawnPos;
				
				Vector2 teleportPos = Vector2.Zero;
				int maxAttempts = 20;

				for (int attempt = 0; attempt < maxAttempts; attempt++)
				{
					float randomAngle = (float)GD.RandRange(0, Mathf.Tau);
					float randomDistance = (float)GD.RandRange(450, 850);
					Vector2 randomOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
					teleportPos = boss.GlobalPosition + randomOffset;
					
					if (teleportPos.DistanceTo(player.GlobalPosition) < 380.0f)
					{
						Vector2 pushOff = (teleportPos - player.GlobalPosition).Normalized();
						if (pushOff == Vector2.Zero) pushOff = Vector2.Right;
						teleportPos = player.GlobalPosition + (pushOff * 380.0f);
					}
					
					teleportPos.X = Mathf.Clamp(teleportPos.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
					teleportPos.Y = Mathf.Clamp(teleportPos.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);

					bool isTooClose = false;
					foreach (Vector2 existingPos in usedPositions)
					{
						if (teleportPos.DistanceTo(existingPos) < 200.0f)
						{
							isTooClose = true;
							break;
						}
					}

					if (!isTooClose)
					{
						break;
					}
				}
				
				usedPositions.Add(teleportPos);
				
				float selectedSpeed = speeds[i % speeds.Length];
				float selectedTrackTime = trackTimes[i % trackTimes.Length];
				
				ActiveOrbsCount++;
				orbInstance.TreeExited += OnOrbDestroyed;
				
				orbInstance.Initialize(teleportPos, selectedSpeed, selectedTrackTime, player);
			}

			await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;

			IsAttacking = false;
		}
	}

	public void ExecutePhase3Orbs(int orbCount)
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null || orbScene == null) return;

		IsAttacking = true;
		ActiveOrbsCount = 0;
		List<Vector2> usedPositions = new List<Vector2>();

		float[] speeds = { 600.0f, 1200.0f, 2300.0f };
		float[] trackTimes = { 1.0f, 2.0f, 3.0f };

		List<Rect2> chunks = new List<Rect2>();
		int gridX = 3;
		int gridY = 3;
		float chunkW = (boss.ArenaMaxBounds.X - boss.ArenaMinBounds.X) / gridX;
		float chunkH = (boss.ArenaMaxBounds.Y - boss.ArenaMinBounds.Y) / gridY;

		for (int x = 0; x < gridX; x++)
		{
			for (int y = 0; y < gridY; y++)
			{
				chunks.Add(new Rect2(boss.ArenaMinBounds.X + (x * chunkW), boss.ArenaMinBounds.Y + (y * chunkH), chunkW, chunkH));
			}
		}

		for (int i = 0; i < orbCount; i++)
		{
			OrbOfTime orbInstance = orbScene.Instantiate<OrbOfTime>();
			boss.GetParent().AddChild(orbInstance);
			
			float spawnAngle = i * (Mathf.Tau / orbCount);
			Vector2 spawnPos = boss.GlobalPosition + new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle)) * 150.0f;
			orbInstance.GlobalPosition = spawnPos;

			float[] weights = new float[chunks.Count];
			float totalWeight = 0.0f;

			for (int c = 0; c < chunks.Count; c++)
			{
				if (usedPositions.Count == 0)
				{
					weights[c] = 1.0f;
				}
				else
				{
					Vector2 chunkCenter = chunks[c].GetCenter();
					float minDistanceToOrbs = float.MaxValue;
					
					foreach (Vector2 pos in usedPositions)
					{
						float dist = chunkCenter.DistanceTo(pos);
						if (dist < minDistanceToOrbs) minDistanceToOrbs = dist;
					}

					float distanceToPlayer = chunkCenter.DistanceTo(player.GlobalPosition);
					
					float combinedScore = minDistanceToOrbs * 0.7f + distanceToPlayer * 0.3f;
					weights[c] = combinedScore * combinedScore; 
				}
				totalWeight += weights[c];
			}

			float roll = (float)GD.RandRange(0.0, totalWeight);
			float cumulative = 0.0f;
			Rect2 selectedChunk = chunks[0];

			for (int c = 0; c < chunks.Count; c++)
			{
				cumulative += weights[c];
				if (roll <= cumulative)
				{
					selectedChunk = chunks[c];
					break;
				}
			}

			Vector2 teleportPos = Vector2.Zero;
			int maxAttempts = 20;

			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				float randX = (float)GD.RandRange(selectedChunk.Position.X, selectedChunk.End.X);
				float randY = (float)GD.RandRange(selectedChunk.Position.Y, selectedChunk.End.Y);
				teleportPos = new Vector2(randX, randY);

				if (teleportPos.DistanceTo(player.GlobalPosition) < 380.0f)
				{
					Vector2 pushOff = (teleportPos - player.GlobalPosition).Normalized();
					if (pushOff == Vector2.Zero) pushOff = Vector2.Right;
					teleportPos = player.GlobalPosition + (pushOff * 380.0f);
					
					teleportPos.X = Mathf.Clamp(teleportPos.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
					teleportPos.Y = Mathf.Clamp(teleportPos.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);
				}

				bool isTooClose = false;
				foreach (Vector2 existingPos in usedPositions)
				{
					if (teleportPos.DistanceTo(existingPos) < 200.0f)
					{
						isTooClose = true;
						break;
					}
				}

				if (!isTooClose) break;
			}

			usedPositions.Add(teleportPos);

			float selectedSpeed = speeds[i % speeds.Length];
			float selectedTrackTime = trackTimes[i % trackTimes.Length];
			
			ActiveOrbsCount++;
			orbInstance.TreeExited += OnOrbDestroyed;
			orbInstance.Initialize(teleportPos, selectedSpeed, selectedTrackTime, player);
		}
		
		IsAttacking = false;
	}

	private void OnOrbDestroyed()
	{
		ActiveOrbsCount--;
		if (ActiveOrbsCount <= 0 && boss.CurrentState != ChronosBoss.State.Defeated && boss.CurrentState != ChronosBoss.State.Rewind)
		{
			if (IsInsideTree() && !IsAttacking)
			{
				boss.Brain.ResumeCooldown(1.0f);
			}
		}
	}

	public async void ExecuteSweep()
	{
		IsAttacking = true;
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		float distanceToPlayer = boss.GlobalPosition.DistanceTo(player.GlobalPosition);
		float sweepAttackRange = 450.0f; 

		if (distanceToPlayer > sweepAttackRange)
		{
			Vector2 teleportDirection = (player.GlobalPosition - boss.GlobalPosition).Normalized();
			if (teleportDirection == Vector2.Zero) teleportDirection = Vector2.Right;

			Vector2 targetPosition = player.GlobalPosition - (teleportDirection * 150.0f); 
			targetPosition.X = Mathf.Clamp(targetPosition.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
			targetPosition.Y = Mathf.Clamp(targetPosition.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);
			
			if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", false);
			
			boss.GlobalPosition = targetPosition;
			await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
			
			if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", true);
		}

		Vector2 attackDirection = (player.GlobalPosition - boss.GlobalPosition).Normalized();
		float baseAngle = attackDirection.Angle();
		
		float startAngle = baseAngle + Mathf.DegToRad(180.0f);
		float endAngle = startAngle - Mathf.DegToRad(270.0f);

		CollisionShape2D sweepShape = null;

		if (sweepHitbox != null)
		{
			sweepHitbox.Rotation = startAngle;
			sweepHitbox.Scale = new Vector2(0.0f, 1.0f);
			sweepHitbox.SetDeferred("monitoring", false);
			sweepHitbox.SetDeferred("monitorable", false);
			
			sweepShape = sweepHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (sweepShape != null) sweepShape.SetDeferred("disabled", true);
			
			ColorRect clockHand = sweepHitbox.GetNodeOrNull<ColorRect>("ClockHandVisual");
			if (clockHand != null)
			{
				clockHand.Visible = true;
				clockHand.Color = new Color(1, 0, 0, 0.3f); 
			}

			Tween summonTween = GetTree().CreateTween();
			summonTween.TweenProperty(sweepHitbox, "scale", new Vector2(1.0f, 1.0f), 0.6f);
		}

		await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
		if (boss.CurrentState == ChronosBoss.State.Defeated) return;

		float randomPause = (float)GD.RandRange(0.0, 0.3);
		if (randomPause > 0.0f)
		{
			await ToSignal(GetTree().CreateTimer(randomPause), SceneTreeTimer.SignalName.Timeout);
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
		}

		if (sweepHitbox != null)
		{
			sweepHitbox.SetDeferred("monitoring", true);
			sweepHitbox.SetDeferred("monitorable", true);
			
			if (sweepShape != null) sweepShape.SetDeferred("disabled", false);
			
			ColorRect clockHand = sweepHitbox.GetNodeOrNull<ColorRect>("ClockHandVisual");
			if (clockHand != null) clockHand.Color = new Color(1, 0, 0, 1.0f); 

			Tween sweepTween = GetTree().CreateTween();
			sweepTween.TweenProperty(sweepHitbox, "rotation", endAngle, 0.6f);
		}

		await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);

		if (sweepHitbox != null)
		{
			sweepHitbox.SetDeferred("monitoring", false);
			sweepHitbox.SetDeferred("monitorable", false);
			sweepHitbox.Scale = new Vector2(0.0f, 1.0f);
			
			if (sweepShape != null) sweepShape.SetDeferred("disabled", true);
			
			ColorRect clockHand = sweepHitbox.GetNodeOrNull<ColorRect>("ClockHandVisual");
			if (clockHand != null) clockHand.Visible = false;
		}

		IsAttacking = false;
		if (IsInsideTree()) boss.Brain.ResumeCooldown(1.0f);
	}

	private async void SpawnCascadingWave(Vector2 centerPos)
	{
		if (earthPillarScene == null) return;
		float tileSize = 40.0f;
		int maxRings = 7; 

		for (int d = 1; d <= maxRings; d++)
		{
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
			for (int x = -d; x <= d; x++)
			{
				for (int y = -d; y <= d; y++)
				{
					if (Mathf.Abs(x) + Mathf.Abs(y) == d)
					{
						Vector2 spawnPos = centerPos + new Vector2(x * tileSize, y * tileSize);
						Area2D pillar = earthPillarScene.Instantiate<Area2D>();
						boss.GetParent().AddChild(pillar);
						pillar.GlobalPosition = spawnPos;
					}
				}
			}
			await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public async void ExecuteJumpingAttack()
	{
		IsAttacking = true;
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		Vector2 targetLandingPos = player.GlobalPosition;
		targetLandingPos.X = Mathf.Clamp(targetLandingPos.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
		targetLandingPos.Y = Mathf.Clamp(targetLandingPos.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);

		if (boss.BossHurtbox != null) boss.BossHurtbox.SetDeferred("monitorable", false);
		if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", false);

		Tween leapTween = GetTree().CreateTween();
		leapTween.TweenProperty(boss, "global_position", targetLandingPos, 0.4f)
				 .SetTrans(Tween.TransitionType.Linear);

		if (bossVisuals != null)
		{
			Tween verticalTween = GetTree().CreateTween();
			verticalTween.TweenProperty(bossVisuals, "position", new Vector2(0, -300.0f), 0.2f)
						 .SetTrans(Tween.TransitionType.Quad)
						 .SetEase(Tween.EaseType.Out);
						
			verticalTween.TweenProperty(bossVisuals, "position", new Vector2(0, 0.0f), 0.2f)
						 .SetTrans(Tween.TransitionType.Quad)
						 .SetEase(Tween.EaseType.In);
		}

		await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
		if (boss.CurrentState == ChronosBoss.State.Defeated) return;

		if (boss.BossHurtbox != null) boss.BossHurtbox.SetDeferred("monitorable", true);
		if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", true);

		if (slamHitbox != null)
		{
			slamHitbox.SetDeferred("monitoring", true);
			slamHitbox.SetDeferred("monitorable", true);
			
			CollisionShape2D slamShape = slamHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (slamShape != null) slamShape.SetDeferred("disabled", false);
		}

		SpawnCascadingWave(targetLandingPos);

		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		
		if (slamHitbox != null)
		{
			slamHitbox.SetDeferred("monitoring", false);
			slamHitbox.SetDeferred("monitorable", false);
			
			CollisionShape2D slamShape = slamHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (slamShape != null) slamShape.SetDeferred("disabled", true);
		}

		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		if (boss.CurrentState == ChronosBoss.State.Defeated) return;

		IsAttacking = false;
		if (IsInsideTree()) boss.Brain.ResumeCooldown(1.2f);
	}

	public async void ExecuteEnhancedSweep() 
	{ 
		IsAttacking = true;
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		Vector2 arenaCenter = (boss.ArenaMinBounds + boss.ArenaMaxBounds) / 2.0f;
		
		if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", false);
		boss.GlobalPosition = arenaCenter;
		
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		if (boss.CurrentState == ChronosBoss.State.Defeated) return;
		if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", true);

		Vector2 dirToPlayer = (player.GlobalPosition - boss.GlobalPosition).Normalized();
		float startAngle = dirToPlayer.Angle() + Mathf.Pi;

		float halfW = (boss.ArenaMaxBounds.X - boss.ArenaMinBounds.X) / 2.0f;
		float halfH = (boss.ArenaMaxBounds.Y - boss.ArenaMinBounds.Y) / 2.0f;
		float swordLength = Mathf.Sqrt((halfW * halfW) + (halfH * halfH)) + 150.0f;

		Area2D sweepArea = new Area2D();
		sweepArea.AddToGroup("BossAttack");
		sweepArea.Rotation = startAngle;
		boss.AddChild(sweepArea);

		CollisionShape2D col = new CollisionShape2D();
		RectangleShape2D rect = new RectangleShape2D();
		rect.Size = new Vector2(swordLength, 40.0f); 
		col.Shape = rect;
		col.Position = new Vector2(swordLength / 2.0f, 0.0f);
		col.SetDeferred("disabled", true);
		sweepArea.AddChild(col);

		ColorRect visual = new ColorRect();
		visual.Size = new Vector2(swordLength, 40.0f); 
		visual.Position = new Vector2(0.0f, -20.0f);
		sweepArea.AddChild(visual);

		float[] sweepDurations = { 0.85f, 1.15f, 1.45f };
		Color[] sweepColors = {
			new Color(1.0f, 0.84f, 0.0f),
			new Color(0.75f, 0.75f, 0.75f),
			new Color(0.8f, 0.5f, 0.2f)
		};
		
		for (int j = 0; j < sweepDurations.Length; j++)
		{
			int randIdx = GD.RandRange(0, sweepDurations.Length - 1);
			float tempDur = sweepDurations[j];
			Color tempCol = sweepColors[j];
			sweepDurations[j] = sweepDurations[randIdx];
			sweepColors[j] = sweepColors[randIdx];
			sweepDurations[randIdx] = tempDur;
			sweepColors[randIdx] = tempCol;
		}

		visual.Color = new Color(sweepColors[0].R, sweepColors[0].G, sweepColors[0].B, 0.3f);

		await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
		if (boss.CurrentState == ChronosBoss.State.Defeated)
		{
			sweepArea.QueueFree();
			return;
		}

		col.SetDeferred("disabled", false);

		for (int i = 0; i < 3; i++)
		{
			if (boss.CurrentState == ChronosBoss.State.Defeated) break;

			visual.Color = sweepColors[i];

			Tween sweepTween = GetTree().CreateTween();
			float targetAngle = startAngle - (Mathf.Tau * (i + 1));
			
			sweepTween.TweenProperty(sweepArea, "rotation", targetAngle, sweepDurations[i])
					  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

			await ToSignal(sweepTween, Tween.SignalName.Finished);
			
			if (boss.CurrentState == ChronosBoss.State.Defeated) break;

			float pauseDuration = (float)GD.RandRange(0.4, 0.65);
			
			if (i < 2)
			{
				visual.Color = new Color(sweepColors[i + 1].R, sweepColors[i + 1].G, sweepColors[i + 1].B, 0.3f);
			}

			await ToSignal(GetTree().CreateTimer(pauseDuration), SceneTreeTimer.SignalName.Timeout);
		}

		sweepArea.QueueFree();
		IsAttacking = false;
		if (IsInsideTree()) boss.Brain.ResumeCooldown(1.0f);
	}

	private async void SpawnEnhancedCascadingWave(Vector2 centerPos)
	{
		if (earthPillarScene == null) return;
		float tileSize = 40.0f;
		int maxRings = 9; 

		for (int d = 1; d <= maxRings; d++)
		{
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
			for (int x = -d; x <= d; x++)
			{
				for (int y = -d; y <= d; y++)
				{
					if (Mathf.Abs(x) + Mathf.Abs(y) == d)
					{
						Vector2 spawnPos = centerPos + new Vector2(x * tileSize, y * tileSize);
						Area2D pillar = earthPillarScene.Instantiate<Area2D>();
						boss.GetParent().AddChild(pillar);
						pillar.GlobalPosition = spawnPos;
					}
				}
			}
			await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public async void ExecuteEnhancedJump()
	{
		IsAttacking = true;
		
		for (int i = 0; i < 3; i++)
		{
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;
			
			Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
			if (player == null) break;

			Vector2 targetLandingPos = player.GlobalPosition;
			targetLandingPos.X = Mathf.Clamp(targetLandingPos.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
			targetLandingPos.Y = Mathf.Clamp(targetLandingPos.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);

			if (boss.BossHurtbox != null) boss.BossHurtbox.SetDeferred("monitorable", false);
			if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", false);

			Tween leapTween = GetTree().CreateTween();
			leapTween.TweenProperty(boss, "global_position", targetLandingPos, 0.4f)
					 .SetTrans(Tween.TransitionType.Linear);

			if (bossVisuals != null)
			{
				Tween verticalTween = GetTree().CreateTween();
				verticalTween.TweenProperty(bossVisuals, "position", new Vector2(0, -400.0f), 0.2f)
							 .SetTrans(Tween.TransitionType.Quad)
							 .SetEase(Tween.EaseType.Out);
							
				verticalTween.TweenProperty(bossVisuals, "position", new Vector2(0, 0.0f), 0.2f)
							 .SetTrans(Tween.TransitionType.Quad)
							 .SetEase(Tween.EaseType.In);
			}

			await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
			if (boss.CurrentState == ChronosBoss.State.Defeated) return;

			if (boss.BossHurtbox != null) boss.BossHurtbox.SetDeferred("monitorable", true);
			if (contactHitbox != null) contactHitbox.SetDeferred("monitoring", true);

			if (slamHitbox != null)
			{
				slamHitbox.SetDeferred("monitoring", true);
				slamHitbox.SetDeferred("monitorable", true);
				
				CollisionShape2D slamShape = slamHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (slamShape != null) slamShape.SetDeferred("disabled", false);
			}

			SpawnEnhancedCascadingWave(targetLandingPos);

			await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
			
			if (slamHitbox != null)
			{
				slamHitbox.SetDeferred("monitoring", false);
				slamHitbox.SetDeferred("monitorable", false);
				
				CollisionShape2D slamShape = slamHitbox.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (slamShape != null) slamShape.SetDeferred("disabled", true);
			}

			await ToSignal(GetTree().CreateTimer(0.45f), SceneTreeTimer.SignalName.Timeout);
		}

		IsAttacking = false;
		if (IsInsideTree()) boss.Brain.ResumeCooldown(0.8f);
	}
}
