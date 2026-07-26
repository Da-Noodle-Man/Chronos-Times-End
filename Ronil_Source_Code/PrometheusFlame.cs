using Godot;
using System;

public partial class PrometheusFlame : StaticBody2D
{
	public override void _Ready()
	{
		AddToGroup("PrometheusFlame");
	}

	public void Consume()
	{
		Node boss = GetTree().GetFirstNodeInGroup("Boss");
		if (boss != null && boss.HasMethod("OnFlameDevoured"))
		{
			boss.Call("OnFlameDevoured");
		}
		
		QueueFree();
	}
}
