using Godot;
using System;

public abstract partial class BossBase : CharacterBody2D
{
	[Signal]
	public delegate void BossDefeatedEventHandler();

	[Signal]
	public delegate void ImmortalityBrokenEventHandler();

	[Signal]
	public delegate void HealthUpdatedEventHandler(int currentHp, int maxHp);

	protected bool IsImmortal = true;
	public int AccumulatedHP = 0;
	public int CurrentHP { get; protected set; }

	public void AccumulateOneHP()
	{
		if (AccumulatedHP < 80)
		{
			AccumulatedHP++;
			GD.Print($"Chronos steals time! Phase 2 HP is now: {AccumulatedHP}");
			EmitSignal(SignalName.HealthUpdated, AccumulatedHP, AccumulatedHP);
		}
	}

	protected void BreakImmortality()
	{
		IsImmortal = false;
		CurrentHP = AccumulatedHP;
		EmitSignal(SignalName.HealthUpdated, CurrentHP, AccumulatedHP);
		EmitSignal(SignalName.ImmortalityBroken);
		TriggerImmortalityBroken();
	}

	protected virtual void TriggerImmortalityBroken() { }

	protected void SetBaseHP(int hp)
	{
		IsImmortal = false;
		CurrentHP = hp;
		EmitSignal(SignalName.HealthUpdated, CurrentHP, hp);
	}

	public void TakeDamage(int amount)
	{
		if (IsImmortal) return;

		CurrentHP -= amount;
		EmitSignal(SignalName.HealthUpdated, CurrentHP, AccumulatedHP);
		
		if (CurrentHP <= 0)
		{
			EmitSignal(SignalName.BossDefeated);
			TriggerBossDefeated();
		}
	}

	protected virtual void TriggerBossDefeated() { }

	public abstract void StartFight();
}
