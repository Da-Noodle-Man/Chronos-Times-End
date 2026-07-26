using Godot;
using System;

public partial class GlobalManager : Node
{
	public int TotalDeaths = 0;
	public int TotalVictories = 0;

	public void TriggerDeathSequence(Node contextNode)
	{
		TotalDeaths++;
		ExecuteTransition(contextNode, new Color(0, 0, 0, 1), "DEFEAT", new Color(1, 0, 0, 1));
	}

	public void TriggerVictorySequence(Node contextNode)
	{
		TotalVictories++;
		ExecuteTransition(contextNode, new Color(1, 1, 1, 1), "VICTORY", new Color(0, 0, 1, 1));
	}

	private void ExecuteTransition(Node contextNode, Color bgColor, string mainText, Color textColor)
	{
		CanvasLayer canvas = new CanvasLayer();
		canvas.Layer = 100;
		contextNode.AddChild(canvas);

		ColorRect bg = new ColorRect();
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bg.Color = new Color(bgColor.R, bgColor.G, bgColor.B, 0);
		canvas.AddChild(bg);

		VBoxContainer vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
		vbox.Modulate = new Color(1, 1, 1, 0);
		canvas.AddChild(vbox);

		Label title = new Label();
		title.Text = mainText;
		title.AddThemeColorOverride("font_color", textColor);
		title.AddThemeFontSizeOverride("font_size", 120);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		Label stats = new Label();
		stats.Text = $"Victories: {TotalVictories} | Deaths: {TotalDeaths}";
		stats.AddThemeColorOverride("font_color", textColor);
		stats.AddThemeFontSizeOverride("font_size", 40);
		stats.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(stats);

		Tween tween = contextNode.GetTree().CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(bg, "color:a", 1.0f, 4.0f);
		tween.TweenProperty(vbox, "modulate:a", 1.0f, 4.0f);

		tween.Chain().TweenCallback(Callable.From(() =>
		{
			contextNode.GetTree().ChangeSceneToFile("res://hub.tscn");
			canvas.QueueFree();
		}));
	}
}
