using TMPro;
using UnityEngine;

/// <summary>WeaponSelectUI / RewardSelectUI 공통 레이아웃.</summary>
public static class ChoiceSelectUILayout
{
	public const float PanelWidth = 1520f;
	public const float PanelHeight = 960f;
	public const float CardWidth = 405f;
	public const float CardHeight = 500f;
	public const float CardOffsetX = 418f;
	public const float CardOffsetY = 12f;
	public const float CardGroupOffsetX = 0f;
	public const float HeaderTopInset = 138f;

	static readonly float[] CardX = { -CardOffsetX, 0f, CardOffsetX };

	public static void Apply(Transform uiRoot)
	{
		if (uiRoot == null)
			return;

		Transform panel = FindChoicePanel(uiRoot);
		if (panel == null)
			return;

		ApplyPanel(panel);
		ApplyHeaderTitle(panel);
		ApplyCards(panel);
	}

	static Transform FindChoicePanel(Transform uiRoot)
	{
		foreach (Transform t in uiRoot.GetComponentsInChildren<Transform>(true))
		{
			if (t.name != "Btn0")
				continue;

			Transform parent = t.parent;
			if (parent != null && parent.Find("Btn1") != null && parent.Find("Btn2") != null)
				return parent;
		}

		return null;
	}

	static void ApplyPanel(Transform panel)
	{
		if (panel is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
	}

	static void ApplyHeaderTitle(Transform panel)
	{
		foreach (Transform child in panel)
		{
			if (child.name.StartsWith("Btn"))
				continue;

			if (child is not RectTransform rect)
				continue;

			TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
			if (tmp == null)
				continue;

			rect.anchorMin = new Vector2(0.5f, 1f);
			rect.anchorMax = new Vector2(0.5f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			rect.anchoredPosition = new Vector2(0f, -HeaderTopInset);
			rect.sizeDelta = new Vector2(720f, 72f);

			tmp.fontSize = 34f;
			tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
			tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			return;
		}
	}

	static void ApplyCards(Transform panel)
	{
		for (int i = 0; i < CardX.Length; i++)
		{
			Transform card = panel.Find($"Btn{i}");
			if (card is RectTransform rect)
				ApplyCard(rect, CardX[i]);
		}
	}

	static void ApplyCard(RectTransform card, float x)
	{
		card.anchorMin = new Vector2(0.5f, 0.5f);
		card.anchorMax = new Vector2(0.5f, 0.5f);
		card.pivot = new Vector2(0.5f, 0.5f);
		card.anchoredPosition = new Vector2(x + CardGroupOffsetX, CardOffsetY);
		card.sizeDelta = new Vector2(CardWidth, CardHeight);

		ApplyCardTitle(card);
		ApplyCardIcon(card);
		ApplyCardDetail(card);
	}

	static void ApplyCardTitle(RectTransform card)
	{
		Transform title = card.Find("Title");
		if (title is not RectTransform rect)
			return;

		// 카드 상단 타이틀 칸에 맞춤
		rect.anchorMin = new Vector2(0.14f, 0.838f);
		rect.anchorMax = new Vector2(0.86f, 0.928f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		TextMeshProUGUI tmp = title.GetComponent<TextMeshProUGUI>();
		if (tmp == null)
			return;

		tmp.fontSize = 24f;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.overflowMode = TextOverflowModes.Ellipsis;
		tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
		tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
	}

	static void ApplyCardIcon(RectTransform card)
	{
		Transform icon = card.Find("Icon");
		if (icon is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(0f, -200f);
		rect.sizeDelta = new Vector2(84f, 84f);
	}

	static void ApplyCardDetail(RectTransform card)
	{
		Transform detail = card.Find("Detail");
		if (detail is not RectTransform rect)
			return;

		// 노란 설명 칸 상단에 등급이 맞도록 — 상단 고정, 텍스트는 아래로만 늘어남
		rect.anchorMin = new Vector2(0.13f, 0.07f);
		rect.anchorMax = new Vector2(0.87f, 0.31f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		TextMeshProUGUI tmp = detail.GetComponent<TextMeshProUGUI>();
		if (tmp == null)
			return;

		tmp.textWrappingMode = TextWrappingModes.Normal;
		tmp.richText = true;
		tmp.overflowMode = TextOverflowModes.Overflow;
		tmp.fontSize = 22f;
		tmp.lineSpacing = 0f;
		tmp.paragraphSpacing = 0f;
		tmp.margin = new Vector4(10f, 2f, 10f, 6f);
		tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
		tmp.verticalAlignment = VerticalAlignmentOptions.Top;
	}
}
