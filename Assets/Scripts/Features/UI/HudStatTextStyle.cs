using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD 통계 텍스트(Coin/Stage/Time/Kill) 가독성 — 바이옴별 대비·외곽선.</summary>
public static class HudStatTextStyle
{
	public static readonly Color LightText = new Color(0.96f, 0.96f, 0.96f, 1f);
	public static readonly Color DarkText = new Color(0.11f, 0.13f, 0.17f, 1f);

	static readonly Color DarkOutline = new Color(0.02f, 0.02f, 0.05f, 0.92f);
	static readonly Color LightOutline = new Color(0.98f, 0.99f, 1f, 0.7f);
	static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.55f);

	// 4 = Snow (5스테이지)
	static readonly int[] LightBackgroundStages = { 4 };

	public static bool IsLightBackgroundStage(int stageIndex)
	{
		for (int i = 0; i < LightBackgroundStages.Length; i++)
		{
			if (LightBackgroundStages[i] == stageIndex)
				return true;
		}

		return false;
	}

	public static void Apply(Text text, int stageIndex)
	{
		if (text == null)
			return;

		bool lightBackground = IsLightBackgroundStage(stageIndex);
		text.color = lightBackground ? DarkText : LightText;

		Outline outline = text.GetComponent<Outline>();
		if (outline == null)
			outline = text.gameObject.AddComponent<Outline>();

		outline.effectColor = lightBackground ? LightOutline : DarkOutline;
		outline.effectDistance = new Vector2(1.5f, -1.5f);
		outline.useGraphicAlpha = true;

		Shadow shadow = text.GetComponent<Shadow>();
		if (shadow == null)
			shadow = text.gameObject.AddComponent<Shadow>();

		shadow.effectColor = lightBackground
			? new Color(1f, 1f, 1f, 0.35f)
			: ShadowColor;
		shadow.effectDistance = new Vector2(2f, -2f);
		shadow.useGraphicAlpha = true;
	}
}
