using Godot;
using System;

public partial class ChronosBrain : Node
{
	private ChronosBoss boss;
	public Timer AccumulateHpTimer;
	public Timer AttackCooldownTimer;
	public Timer RewindTimer;
	public Timer PositioningTimer;
	
	public enum Phase2Move { None, Sweep, Jump, Orbs }
	private Phase2Move lastAttack = Phase2Move.None;
	public Phase2Move UpcomingAttack = Phase2Move.None;
	
	public bool IsPositioning = false;
	private bool interceptorComboActive = false;
	private bool enragerComboActive = false;
	private bool mode1ComboActive = false;

	public override void _Ready()
	{
		boss = GetParent<ChronosBoss>();
		
		AccumulateHpTimer = boss.GetNode<Timer>("AccumulateHpTimer");
		AttackCooldownTimer = boss.GetNode<Timer>("AttackCooldownTimer");
		RewindTimer = boss.GetNode<Timer>("RewindTimer");
		
		PositioningTimer = new Timer();
		AddChild(PositioningTimer);
		PositioningTimer.WaitTime = 1.2f;
		PositioningTimer.OneShot = true;
		PositioningTimer.Timeout += OnPositioningTimerTimeout;

		AccumulateHpTimer.WaitTime = 1.0f;
		AccumulateHpTimer.OneShot = false;
		AttackCooldownTimer.OneShot = true;

		AccumulateHpTimer.Timeout += OnAccumulateHpTimerTimeout;
		AttackCooldownTimer.Timeout += OnAttackCooldownTimerTimeout;
		RewindTimer.Timeout += OnRewindTimerTimeout;
	}

	public void StartPhase1()
	{
		AccumulateHpTimer.Start();
		AttackCooldownTimer.Start();
		boss.Attacks.IsAttacking = false;
	}

	public void TransitionToPhase2()
	{
		AccumulateHpTimer.Stop();
		boss.Attacks.IsAttacking = false;
		lastAttack = Phase2Move.None;
		UpcomingAttack = Phase2Move.None;
		IsPositioning = false;
		ResumeCooldown(2.0f);
	}

	public void StartRewind()
	{
		AttackCooldownTimer.Stop();
		PositioningTimer.Stop();
		boss.Attacks.IsAttacking = false;
		RewindTimer.Start();
	}

	public void StartPhase3()
	{
		AttackCooldownTimer.Start();
	}

	public void StopAll()
	{
		AttackCooldownTimer.Stop();
		AccumulateHpTimer.Stop();
		RewindTimer.Stop();
		PositioningTimer.Stop();
	}

	private void OnAccumulateHpTimerTimeout()
	{
		if (boss.CurrentState == ChronosBoss.State.Immortality)
		{
			boss.AccumulateOneHP();
		}
	}

	private void OnRewindTimerTimeout()
	{
		boss.CompleteRewind();
	}

	private void DecidePhase2Attack()
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		float distance = boss.GlobalPosition.DistanceTo(player.GlobalPosition);
		bool isFar = distance > 750.0f;
		int roll = GD.RandRange(0, 99);

		if (lastAttack == Phase2Move.None)
		{
			if (isFar)
			{
				if (roll < 40) { UpcomingAttack = Phase2Move.Jump; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Sweep; }
				else { UpcomingAttack = Phase2Move.Orbs; }
			}
			else
			{
				if (roll < 50) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Orbs; }
				else { UpcomingAttack = Phase2Move.Jump; }
			}
		}
		else if (lastAttack == Phase2Move.Sweep)
		{
			if (isFar)
			{
				if (roll < 50) { UpcomingAttack = Phase2Move.Jump; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Orbs; }
				else { UpcomingAttack = Phase2Move.Sweep; }
			}
			else
			{
				if (roll < 40) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Orbs; }
				else { UpcomingAttack = Phase2Move.Jump; }
			}
		}
		else if (lastAttack == Phase2Move.Jump)
		{
			if (isFar)
			{
				if (roll < 50) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Orbs; }
				else { UpcomingAttack = Phase2Move.Jump; }
			}
			else
			{
				if (roll < 50) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Orbs; }
				else { UpcomingAttack = Phase2Move.Jump; }
			}
		}
		else if (lastAttack == Phase2Move.Orbs)
		{
			if (isFar)
			{
				if (roll < 40) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 80) { UpcomingAttack = Phase2Move.Jump; }
				else { UpcomingAttack = Phase2Move.Orbs; }
			}
			else
			{
				if (roll < 60) { UpcomingAttack = Phase2Move.Sweep; }
				else if (roll < 90) { UpcomingAttack = Phase2Move.Jump; }
				else { UpcomingAttack = Phase2Move.Orbs; }
			}
		}

		lastAttack = UpcomingAttack;
		IsPositioning = true;
		PositioningTimer.Start();
	}

	private void OnPositioningTimerTimeout()
	{
		IsPositioning = false;
		if (boss.CurrentState == ChronosBoss.State.Defeated) return;

		switch (UpcomingAttack)
		{
			case Phase2Move.Jump: boss.Attacks.ExecuteJumpingAttack(); break;
			case Phase2Move.Sweep: boss.Attacks.ExecuteSweep(); break;
			case Phase2Move.Orbs: boss.Attacks.ExecuteOrbsOfTime(3); break;
		}
	}

	private void OnAttackCooldownTimerTimeout()
	{
		if (boss.CurrentState == ChronosBoss.State.Defeated || boss.CurrentState == ChronosBoss.State.Rewind || boss.Attacks.IsAttacking) return;

		if (boss.CurrentState == ChronosBoss.State.Immortality)
		{
			boss.Attacks.ExecuteGluttony();
			boss.Attacks.ExecuteOrbsOfTime(3);
		}
		else if (boss.CurrentState == ChronosBoss.State.Execution)
		{
			DecidePhase2Attack();
		}
		else if (boss.CurrentState == ChronosBoss.State.TrueForm)
		{
			DecidePhase3Attack();
		}
	}

	public void ExecuteClockworkCleave()
	{
		boss.Attacks.ExecuteEnhancedSweep();
	}

	private void DecidePhase3Attack()
	{
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null) return;

		float hpPercent = (float)boss.CurrentHP / Math.Max(1, boss.AccumulatedHP);
		float farLimit = 2.5f;
		float closeLimit = 3.0f;

		if (hpPercent <= 0.33f)
		{
			farLimit = 0.5f;
			closeLimit = 0.8f;
		}
		else if (hpPercent <= 0.66f)
		{
			farLimit = 1.5f;
			closeLimit = 2.0f;
		}

		if (interceptorComboActive)
		{
			interceptorComboActive = false;
			boss.TimeInFarRange = 0.0f;
			int roll = GD.RandRange(0, 99);
			if (roll < 70) boss.Attacks.ExecuteEnhancedGluttony();
			else boss.Attacks.ExecuteEnhancedJump();
			return;
		}

		if (enragerComboActive)
		{
			enragerComboActive = false;
			boss.TimeInCloseRange = 0.0f;
			int roll = GD.RandRange(0, 99);
			if (roll < 60) boss.Attacks.ExecuteEnhancedGluttony();
			else boss.Attacks.ExecutePhase3Orbs(6);
			return;
		}

		if (mode1ComboActive)
		{
			mode1ComboActive = false;
			int roll = GD.RandRange(0, 99);
			if (roll < 60) boss.Attacks.ExecuteEnhancedGluttony();
			else boss.Attacks.ExecuteEnhancedJump();
			return;
		}

		if (boss.TimeInFarRange >= farLimit)
		{
			boss.EmitSignal(ChronosBoss.SignalName.DialogTriggered, "TooFar");
			
			Vector2 targetPos = player.GlobalPosition;
			Variant playerVelocity = player.Get("Velocity");
			if (playerVelocity.VariantType == Variant.Type.Vector2 && playerVelocity.AsVector2() != Vector2.Zero)
			{
				targetPos += playerVelocity.AsVector2().Normalized() * 350.0f;
			}
			else 
			{
				Vector2 dirToPlayer = (player.GlobalPosition - boss.GlobalPosition).Normalized();
				if (dirToPlayer == Vector2.Zero) dirToPlayer = Vector2.Right;
				targetPos = player.GlobalPosition + (dirToPlayer * 350.0f);
			}

			targetPos.X = Mathf.Clamp(targetPos.X, boss.ArenaMinBounds.X, boss.ArenaMaxBounds.X);
			targetPos.Y = Mathf.Clamp(targetPos.Y, boss.ArenaMinBounds.Y, boss.ArenaMaxBounds.Y);
			
			boss.GlobalPosition = targetPos;
			boss.Attacks.ExecutePhase3Orbs(6);
			interceptorComboActive = true;
		}
		else if (boss.TimeInCloseRange >= closeLimit)
		{
			boss.EmitSignal(ChronosBoss.SignalName.DialogTriggered, "TooClose");
			boss.Attacks.ExecuteEnhancedJump();
			enragerComboActive = true;
		}
		else
		{
			int roll = GD.RandRange(0, 99);
			if (roll < 50)
			{
				boss.Attacks.ExecuteEnhancedGluttony();
			}
			else if (roll < 80)
			{
				boss.Attacks.ExecutePhase3Orbs(6);
				mode1ComboActive = true;
			}
			else
			{
				boss.Attacks.ExecuteEnhancedJump();
			}
		}
	}
	
	public void ResumeCooldown(float baseWaitTime)
	{
		if (boss.CurrentState != ChronosBoss.State.Defeated)
		{
			float randomDelay = (float)GD.RandRange(0.8, 1.4);
			AttackCooldownTimer.WaitTime = baseWaitTime + randomDelay;
			AttackCooldownTimer.Start();
		}
	}
}
