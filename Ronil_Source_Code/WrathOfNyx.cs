using Godot;
using System;

public partial class WrathOfNyx : Node
{
	private CanvasModulate darknessModulate;
	private PointLight2D playerLight;
	private float baseLightScale = 1.0f;

	public override void _Ready()
	{
		darknessModulate = GetNode<CanvasModulate>("CanvasModulate");
		playerLight = GetParent().GetNode<PointLight2D>("player/PointLight2D");
		
		GD.Randomize();

		baseLightScale = playerLight.TextureScale;

		darknessModulate.Color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
		playerLight.Energy = 1.0f;

		TriggerDarknessEvent();
	}

	private void TriggerDarknessEvent()
	{
		int roll = GD.RandRange(0, 5);
		float targetScale = baseLightScale;
		float holdDuration = 15.0f;

		switch (roll)
		{
			case 0: 
				targetScale = baseLightScale * 3.75f;
				holdDuration = (float)GD.RandRange(15.0, 20.0);
				break;
			case 1: 
				targetScale = baseLightScale * 3.50f;
				holdDuration = (float)GD.RandRange(15.0, 20.0);
				break;
			case 2: 
				targetScale = baseLightScale * 3.00f;
				holdDuration = (float)GD.RandRange(20.0, 25.0);
				break;
			case 3: 
				targetScale = baseLightScale * 2.75f;
				holdDuration = (float)GD.RandRange(20.0, 25.0);
				break;
			case 4: 
				targetScale = baseLightScale * 2.25f;
				holdDuration = (float)GD.RandRange(25.0, 30.0);
				break;
			case 5: 
				targetScale = baseLightScale * 2.00f;
				holdDuration = (float)GD.RandRange(25.0, 30.0);
				break;
		}

		float fadeDuration = 3.5f;

		Tween tween = GetTree().CreateTween();
		
		tween.TweenProperty(playerLight, "texture_scale", targetScale, fadeDuration);
		tween.TweenInterval(holdDuration);
		tween.TweenCallback(Callable.From(TriggerDarknessEvent));
	}
}
