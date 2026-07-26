using Godot;
using System;

public partial class ChronosBoss : BossBase
{
	[Signal] public delegate void DialogTriggeredEventHandler(string eventName);

	public enum State { Immortality, Execution, Rewind, TrueForm, UltimateOverride, Defeated }
	public State CurrentState = State.Immortality;

	[Export] public Vector2 ArenaMinBounds = new Vector2(100.0f, 100.0f);
	[Export] public Vector2 ArenaMaxBounds = new Vector2(1820.0f, 980.0f);

	public int ActiveFlamesCount = 6;
	public ColorRect ShieldVisual;
	public Area2D BossHurtbox;
	
	public ChronosBrain Brain;
	public ChronosAttacks Attacks;

	public float TimeInCloseRange = 0.0f;
	public float TimeInFarRange = 0.0f;
	private bool ultimate66Triggered = false;
	private bool ultimate33Triggered = false;

	public override void _Ready()
	{
		ShieldVisual = GetNodeOrNull<ColorRect>("ShieldVisual");
		BossHurtbox = GetNodeOrNull<Area2D>("BossHurtbox");
		Brain = GetNode<ChronosBrain>("Brain");
		Attacks = GetNode<ChronosAttacks>("Attacks");
		
		if (BossHurtbox != null)
		{
			BossHurtbox.AddToGroup("Boss");
		}

		HealthUpdated += OnBossHealthUpdated;
		ImmortalityBroken += OnImmortalityBroken;
		BossDefeated += OnBossDefeated;

		GD.Randomize();
		StartFight();
	}

	public override void StartFight()
	{
		EmitSignal(SignalName.DialogTriggered, "FightStart");
		CurrentState = State.Immortality;
		IsImmortal = true;
		AccumulatedHP = 0;
		
		if (ShieldVisual != null) ShieldVisual.Visible = true;
		
		Brain.StartPhase1();
	}

	public new void TakeDamage(int damageAmount)
	{
		if (CurrentState == State.Defeated || CurrentState == State.Rewind || CurrentState == State.UltimateOverride) return;
		if (IsImmortal) return;

		CurrentHP -= damageAmount;
		if (CurrentHP < 0) CurrentHP = 0;

		EmitSignal(SignalName.HealthUpdated, CurrentHP, AccumulatedHP);

		if (CurrentState == State.TrueForm)
		{
			float hpPercent = (float)CurrentHP / AccumulatedHP;
			if (hpPercent <= 0.66f && !ultimate66Triggered)
			{
				ultimate66Triggered = true;
				TriggerUltimateOverride();
			}
			else if (hpPercent <= 0.33f && !ultimate33Triggered)
			{
				ultimate33Triggered = true;
				TriggerUltimateOverride();
			}
		}

		if (CurrentHP <= 0) OnBossDefeated();
	}

	private void TriggerUltimateOverride()
	{
		CurrentState = State.UltimateOverride;
		IsImmortal = true;
		Brain.StopAll();
		GetTree().CallGroup("BossAttack", "QueueFree");
		Attacks.ActiveOrbsCount = 0;
		Attacks.IsAttacking = false;
		
		if (Brain.HasMethod("ExecuteClockworkCleave"))
		{
			Brain.Call("ExecuteClockworkCleave");
		}
	}

	public void OnFlameDevoured()
	{
		ActiveFlamesCount--;
		if (ActiveFlamesCount <= 0 && CurrentState == State.Immortality)
		{
			OnImmortalityBroken();
		}
	}

	public void OnImmortalityBroken()
	{
		CurrentState = State.Execution;
		IsImmortal = false;
		CurrentHP = AccumulatedHP; 
		EmitSignal(SignalName.HealthUpdated, CurrentHP, AccumulatedHP); 
		
		if (ShieldVisual != null) ShieldVisual.Visible = false;
		
		GetTree().CallGroup("BossAttack", "QueueFree");
		GetTree().CallGroup("GluttonyTrap", "QueueFree");
		
		Brain.TransitionToPhase2();
	}

	private void OnBossHealthUpdated(int currentHp, int maxHp) { }

	private void OnBossDefeated()
	{
		if (CurrentState == State.Execution)
		{
			TriggerPhase3Rewind();
		}
		else if (CurrentState == State.TrueForm || CurrentState == State.UltimateOverride)
		{
			CurrentState = State.Defeated;
			EmitSignal(SignalName.DialogTriggered, "BossDefeated");
			Brain.StopAll();

			GlobalManager global = GetNode<GlobalManager>("/root/GlobalManager");
			global.TriggerVictorySequence(this);
		}
	}

	private void TriggerPhase3Rewind()
	{
		CurrentState = State.Rewind;
		GetTree().CallGroup("BossAttack", "QueueFree");
		Brain.StartRewind();
	}

	public void CompleteRewind()
	{
		CurrentState = State.TrueForm;
		CurrentHP = AccumulatedHP;
		EmitSignal(SignalName.HealthUpdated, CurrentHP, AccumulatedHP);
		EmitSignal(SignalName.DialogTriggered, "Phase3Start");
		Brain.StartPhase3();
	}

	private Vector2 GetEvasionVector(Vector2 bossPos, Vector2 playerPos)
	{
		float marginX = (ArenaMaxBounds.X - ArenaMinBounds.X) * 0.25f;
		float marginY = (ArenaMaxBounds.Y - ArenaMinBounds.Y) * 0.25f;

		bool inDangerZone = bossPos.X < ArenaMinBounds.X + marginX ||
							bossPos.X > ArenaMaxBounds.X - marginX ||
							bossPos.Y < ArenaMinBounds.Y + marginY ||
							bossPos.Y > ArenaMaxBounds.Y - marginY;

		Vector2 directionAway = (bossPos - playerPos).Normalized();

		if (inDangerZone)
		{
			Vector2 arenaCenter = (ArenaMinBounds + ArenaMaxBounds) / 2.0f;
			Vector2 dirToCenter = (arenaCenter - bossPos).Normalized();
			Vector2 dirToPlayer = (playerPos - bossPos).Normalized();

			Vector2 orbit1 = new Vector2(-dirToPlayer.Y, dirToPlayer.X);
			Vector2 orbit2 = new Vector2(dirToPlayer.Y, -dirToPlayer.X);

			if (orbit1.Dot(dirToCenter) > orbit2.Dot(dirToCenter))
			{
				return (directionAway + orbit1 * 2.0f).Normalized();
			}
			else
			{
				return (directionAway + orbit2 * 2.0f).Normalized();
			}
		}

		return directionAway;
	}

	public override void _PhysicsProcess(double delta)
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		if (CurrentState == State.TrueForm)
		{
			float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distanceToPlayer <= 650.0f)
			{
				TimeInCloseRange += (float)delta;
				TimeInFarRange = 0.0f;
			}
			else
			{
				TimeInFarRange += (float)delta;
				TimeInCloseRange = 0.0f;
			}
		}

		if (CurrentState == State.Execution)
		{
			float bossSpeed = 350.0f; 

			if (Brain.IsPositioning)
			{
				float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
				Vector2 directionToPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
				Vector2 newPosition = GlobalPosition;

				if (Brain.UpcomingAttack == ChronosBrain.Phase2Move.Sweep)
				{
					if (distance > 380.0f)
					{
						newPosition += directionToPlayer * bossSpeed * (float)delta;
					}
					else if (distance < 320.0f)
					{
						Vector2 evasionDir = GetEvasionVector(GlobalPosition, player.GlobalPosition);
						newPosition += evasionDir * bossSpeed * (float)delta;
					}
				}
				else if (Brain.UpcomingAttack == ChronosBrain.Phase2Move.Jump)
				{
					newPosition += directionToPlayer * bossSpeed * (float)delta;
				}
				else if (Brain.UpcomingAttack == ChronosBrain.Phase2Move.Orbs)
				{
					Vector2 evasionDir = GetEvasionVector(GlobalPosition, player.GlobalPosition);
					newPosition += evasionDir * bossSpeed * (float)delta;
				}

				newPosition.X = Mathf.Clamp(newPosition.X, ArenaMinBounds.X, ArenaMaxBounds.X);
				newPosition.Y = Mathf.Clamp(newPosition.Y, ArenaMinBounds.Y, ArenaMaxBounds.Y);
				GlobalPosition = newPosition;
			}
			else if (Attacks.ActiveOrbsCount > 0 && !Attacks.IsAttacking)
			{
				Vector2 evasionDir = GetEvasionVector(GlobalPosition, player.GlobalPosition);
				Vector2 newPosition = GlobalPosition + (evasionDir * bossSpeed * (float)delta);
				
				newPosition.X = Mathf.Clamp(newPosition.X, ArenaMinBounds.X, ArenaMaxBounds.X);
				newPosition.Y = Mathf.Clamp(newPosition.Y, ArenaMinBounds.Y, ArenaMaxBounds.Y);
				GlobalPosition = newPosition;
			}
		}
		else if (CurrentState == State.TrueForm && !Attacks.IsAttacking && !Brain.IsPositioning)
		{
			float bossSpeed = 225.0f;
			float safetyRadius = 250.0f;
			
			float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
			
			if (distanceToPlayer > safetyRadius)
			{
				Vector2 directionToTarget = (player.GlobalPosition - GlobalPosition).Normalized();
				Vector2 newPosition = GlobalPosition + (directionToTarget * bossSpeed * (float)delta);
				
				newPosition.X = Mathf.Clamp(newPosition.X, ArenaMinBounds.X, ArenaMaxBounds.X);
				newPosition.Y = Mathf.Clamp(newPosition.Y, ArenaMinBounds.Y, ArenaMaxBounds.Y);
				GlobalPosition = newPosition;
			}
		}
	}
}
