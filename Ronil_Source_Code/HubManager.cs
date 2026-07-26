using Godot;
using System;

public partial class HubManager : Node2D
{
	private Area2D bossPortal;
	private Area2D quitPortal;

	public override void _Ready()
	{
		bossPortal = GetNodeOrNull<Area2D>("BossPortal");
		quitPortal = GetNodeOrNull<Area2D>("QuitPortal");

		if (bossPortal != null)
		{
			bossPortal.BodyEntered += OnBossPortalEntered;
		}

		if (quitPortal != null)
		{
			quitPortal.BodyEntered += OnQuitPortalEntered;
		}
	}

	private void OnBossPortalEntered(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			CallDeferred(MethodName.TransitionToArena);
		}
	}

	private void TransitionToArena()
	{
		GetTree().ChangeSceneToFile("res://TartarusArena.tscn");
	}

	private void OnQuitPortalEntered(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			GetTree().Quit();
		}
	}
}
