using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void PlayerDiedEventHandler();

	private float moveSpeed = 700.0f;
	private float acceleration = 2800.0f;
	private float friction = 2000.0f;
	private float gravityMultiplier = 1.0f;

	private float baseDashSpeed = 1800.0f;
	private float dashSpeed = 1800.0f;
	private float dashDuration = 0.2f;
	
	private float maxDashEnergy = 100.0f;
	private float dashEnergy = 100.0f;
	private float dashCost = 100.0f;
	private float dashCooldown = 1.5f; 

	private bool isDashing = false;
	private float dashTimer = 0.0f;
	private Vector2 dashDirection = Vector2.Zero;

	public bool hasHermesSandals = false;
	public bool isInvincible = false;

	private Area2D hurtbox;
	private SimpleSword equippedSword;
	
	private ProgressBar healthBar;
	private ProgressBar dashBar;
	private ColorRect bossHealthBar;

	public int maxHp = 100;
	public int currentHp = 100;
	private bool isDead = false;

	public override void _Ready()
	{
		hasHermesSandals = false;
		isInvincible = false;
		dashSpeed = baseDashSpeed;
		dashCost = 100.0f;
		dashCooldown = 1.5f;
		dashEnergy = maxDashEnergy;

		hurtbox = GetNode<Area2D>("PlayerHurtbox");
		equippedSword = GetNode<SimpleSword>("WeaponPivot/SimpleSword");
		
		healthBar = GetNodeOrNull<ProgressBar>("PlayerUIManager/HealthBar");
		dashBar = GetNodeOrNull<ProgressBar>("PlayerUIManager/DashBar");
		bossHealthBar = GetNodeOrNull<ColorRect>("PlayerUIManager/BossHealthBar");
		
		if (hurtbox != null)
		{
			hurtbox.AreaEntered += OnHurtboxEntered;
		}

		if (bossHealthBar != null)
		{
			bossHealthBar.Size = new Vector2(0, 30.0f);
			bossHealthBar.Position = new Vector2(GetViewportRect().Size.X / 2.0f, 40.0f);
		}

		if (equippedSword != null)
		{
			CollisionShape2D swordShape = equippedSword.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (swordShape != null)
			{
				swordShape.SetDeferred("disabled", true);
			}

			ColorRect swordVisual = equippedSword.GetNodeOrNull<ColorRect>("ColorRect");
			if (swordVisual != null)
			{
				swordVisual.Visible = false;
			}
		}

		Node bossNode = GetTree().GetFirstNodeInGroup("Boss");
		if (bossNode != null)
		{
			bossNode.Connect("HealthUpdated", new Callable(this, MethodName.OnBossHealthUpdated));
		}

		CallDeferred(MethodName.ApplyHermesSandals);
	}

	public void ApplyHermesSandals()
	{
		if (GetTree().CurrentScene != null && GetTree().CurrentScene.Name.ToString().ToLower().Contains("tartarus"))
		{
			hasHermesSandals = true;
			dashCost = 50.0f; 
			dashCooldown = 2.0f; 
			dashSpeed = baseDashSpeed * 0.75f; 
			GD.Print("SYSTEM: Hermes Sandals Activated.");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;

		if (dashEnergy < maxDashEnergy)
		{
			float regenRate = dashCost / dashCooldown;
			dashEnergy += regenRate * (float)delta;
			
			if (dashEnergy > maxDashEnergy)
			{
				dashEnergy = maxDashEnergy;
			}
		}

		if (isDashing)
		{
			ExecuteDash(delta);
		}
		else
		{
			HandleNormalMovement(delta);
			HandleAiming();
			CheckDashInput();
		}

		MoveAndSlide();
		UpdateUI();
	}

	private void HandleNormalMovement(double delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		float currentMultiplier = equippedSword != null ? equippedSword.speedMultiplier : 1.0f;

		if (inputDirection != Vector2.Zero)
		{
			Velocity = Velocity.MoveToward(inputDirection * (moveSpeed * gravityMultiplier * currentMultiplier), acceleration * (float)delta);
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, friction * (float)delta);
		}
	}

	private void HandleAiming()
	{
		LookAt(GetGlobalMousePosition());
	}

	private void CheckDashInput()
	{
		if (Input.IsActionJustPressed("dash") && dashEnergy >= dashCost && !isDashing)
		{
			StartDash();
		}
	}

	private void StartDash()
	{
		isDashing = true;
		dashTimer = dashDuration;
		dashEnergy -= dashCost;

		dashDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		
		if (dashDirection == Vector2.Zero)
		{
			if (Velocity != Vector2.Zero)
			{
				dashDirection = Velocity.Normalized();
			}
			else
			{
				dashDirection = Vector2.Right.Rotated(Rotation);
			}
		}
		else
		{
			dashDirection = dashDirection.Normalized();
		}

		if (hasHermesSandals)
		{
			isInvincible = true;
		}
	}

	private void ExecuteDash(double delta)
	{
		Velocity = dashDirection * dashSpeed;
		dashTimer -= (float)delta;

		if (dashTimer <= 0)
		{
			isDashing = false;
			Velocity *= 0.5f;

			if (hasHermesSandals)
			{
				isInvincible = false;
			}
		}
	}

	private void UpdateUI()
	{
		if (healthBar != null)
		{
			healthBar.MaxValue = maxHp;
			healthBar.Value = currentHp;
		}

		if (dashBar != null)
		{
			dashBar.MaxValue = maxDashEnergy;
			dashBar.Value = dashEnergy;
		}
	}

	private void OnBossHealthUpdated(int currentHp, int maxHp)
	{
		if (bossHealthBar != null)
		{
			float newWidth = currentHp * 15.0f;
			bossHealthBar.Size = new Vector2(newWidth, 30.0f);
			
			float screenWidth = GetViewportRect().Size.X;
			float centeredX = (screenWidth / 2.0f) - (newWidth / 2.0f);
			
			bossHealthBar.Position = new Vector2(centeredX, 40.0f);
		}
	}

	private void OnHurtboxEntered(Area2D area)
	{
		if (isDead) return;

		if (area.IsInGroup("BossAttack") || area.IsInGroup("UnparryableBossAttack"))
		{
			TakeDamage(100);
		}
	}

	public void TakeDamage(int damageAmount)
	{
		if (isDead || isInvincible) return;

		currentHp -= damageAmount;

		if (currentHp <= 0)
		{
			Die("Player was struck down.");
		}
	}

	public async void Die(string causeOfDeath = "Player was struck down.")
	{
		if (isDead || isInvincible) return;
		
		isDead = true;
		GD.Print(causeOfDeath);
		EmitSignal(SignalName.PlayerDied);

		SetPhysicsProcess(false);
		SetProcess(false);
		RemoveFromGroup("Player");

		if (equippedSword != null)
		{
			equippedSword.SetPhysicsProcess(false);
		}

		AnimationPlayer anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (anim != null)
		{
			anim.Play("death");
		}

		GlobalManager global = GetNode<GlobalManager>("/root/GlobalManager");
		global.TriggerDeathSequence(this);
	}

	private void ChangeSceneToHub()
	{
		GetTree().ChangeSceneToFile("res://hub.tscn");
	}
}
