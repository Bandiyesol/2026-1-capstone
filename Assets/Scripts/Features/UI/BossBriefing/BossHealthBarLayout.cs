using UnityEngine;
using UnityEngine.UI;

/// <summary>보스 체력바 HUD 레이아웃 상수 + Fill Area 여백.</summary>
public static class BossHealthBarLayout
{
	public const float HudBottomOffsetY = 58f;
	public const float BarScale = 4f;
	public const float BarWidth = 210f;
	public const float BarHeight = 25f;
	public const float BossNameFontSize = 34f;
	public const float BossNameOffsetY = 2f;

	const float PlayerBarWidth = 90f;
	const float PlayerFillHorizontalInset = 27.034f;
	const float PlayerFillVerticalInset = 9.722199f;

	public static void ConfigureFillInsets(Slider slider, float barWidth = BarWidth)
	{
		if (slider == null)
			return;

		Transform fillArea = slider.fillRect != null ? slider.fillRect.parent : slider.transform.Find("Fill Area");
		if (fillArea is RectTransform fillAreaRect)
		{
			fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
			fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
			fillAreaRect.anchoredPosition = Vector2.zero;

			float widthRatio = barWidth / PlayerBarWidth;
			float horizontalInset = PlayerFillHorizontalInset * widthRatio;
			fillAreaRect.sizeDelta = new Vector2(-horizontalInset, -PlayerFillVerticalInset);
		}

		if (slider.fillRect is RectTransform fillRect)
		{
			fillRect.anchorMin = new Vector2(0f, 0f);
			fillRect.anchorMax = new Vector2(0f, 1f);
			fillRect.pivot = new Vector2(0f, 0.5f);
			fillRect.anchoredPosition = Vector2.zero;
			fillRect.sizeDelta = Vector2.zero;
		}
	}
}
