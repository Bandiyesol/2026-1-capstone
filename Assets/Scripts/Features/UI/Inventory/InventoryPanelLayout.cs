using TMPro;
using UnityEngine;

/// <summary>인벤토리 BoxPanel — row 위치 고정, 타이틀은 row 바로 위, 코인은 row 오른쪽 끝.</summary>
public static class InventoryPanelLayout
{
	public const float RowWidth = 1000f;
	public const float RowHeight = 160f;
	public const float PanelContentHeight = 960f;

	const float ContentLeft = 235f;
	const float ContentRight = ContentLeft + RowWidth;
	const float LabelWidth = 200f;
	const float LabelHeight = 50f;
	const float TitleGapAboveRow = 4f;
	const float SectionGap = 12f;
	const float FirstRowTop = 175f;

	const float BottomReserved = 178f;
	const float CoinBottomY = 118f;
	const float CoinWidth = 240f;
	const float CoinHeight = 44f;

	struct SectionTarget
	{
		public string[] labelNames;
		public string[] rowNames;
	}

	static readonly SectionTarget[] Sections =
	{
		new SectionTarget
		{
			labelNames = new[] { "WeaponRowLabel", "WeaponLabel" },
			rowNames = new[] { "WeaponRow", "WeaponSlots" },
		},
		new SectionTarget
		{
			labelNames = new[] { "AccessoryRowLabel", "AccessoryLabel" },
			rowNames = new[] { "AccessoryRow", "AccRow", "AccessorySlots" },
		},
		new SectionTarget
		{
			labelNames = new[] { "PotionRowLabel", "PotionLabel" },
			rowNames = new[] { "PotionRow", "PotionSlots" },
		},
	};

	public static void Apply(Transform panelRoot)
	{
		if (panelRoot == null)
			return;

		Transform box = OverlayPanelUILayout.FindBoxPanel(panelRoot) ?? panelRoot;

		float rowTop = FirstRowTop;
		for (int i = 0; i < Sections.Length; i++)
		{
			SectionTarget section = Sections[i];
			bool isLastSection = i == Sections.Length - 1;
			float maxRowHeight = isLastSection
				? Mathf.Max(RowHeight, PanelContentHeight - rowTop - BottomReserved)
				: float.PositiveInfinity;

			float rowHeight = ApplyRow(box, section.rowNames, rowTop, maxRowHeight);
			ApplyLabelAboveRow(box, section.labelNames, rowTop);

			rowTop += rowHeight + SectionGap + LabelHeight;
		}

		ApplyCoinLabel(box);
	}

	static void ApplyLabelAboveRow(Transform box, string[] names, float rowTopInset)
	{
		if (FindChild(box, names) is not RectTransform rect)
			return;

		float labelBottomInset = rowTopInset - TitleGapAboveRow;

		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 0f);
		rect.anchoredPosition = new Vector2(ContentLeft, -labelBottomInset);
		rect.sizeDelta = new Vector2(LabelWidth, LabelHeight);

		if (rect.TryGetComponent(out TextMeshProUGUI tmp))
		{
			tmp.verticalAlignment = VerticalAlignmentOptions.Bottom;
			tmp.horizontalAlignment = HorizontalAlignmentOptions.Left;
		}
	}

	static float ApplyRow(Transform box, string[] names, float topInset, float maxHeight)
	{
		if (FindChild(box, names) is not RectTransform rect)
			return RowHeight;

		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(ContentLeft, -topInset);
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RowWidth);

		float height = Mathf.Max(RowHeight, rect.rect.height);
		if (float.IsFinite(maxHeight))
			height = Mathf.Min(height, maxHeight);

		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		return height;
	}

	static void ApplyCoinLabel(Transform box)
	{
		foreach (string name in new[] { "GoldText", "GoldLabel", "goldLabel" })
		{
			Transform target = FindChild(box, name);
			if (target is not RectTransform rect)
				continue;

			rect.anchorMin = new Vector2(0f, 0f);
			rect.anchorMax = new Vector2(0f, 0f);
			rect.pivot = new Vector2(1f, 0f);
			rect.anchoredPosition = new Vector2(ContentRight, CoinBottomY);
			rect.sizeDelta = new Vector2(CoinWidth, CoinHeight);

			if (rect.TryGetComponent(out TextMeshProUGUI tmp))
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Right;
		}
	}

	static Transform FindChild(Transform root, params string[] names)
	{
		foreach (string name in names)
		{
			if (string.IsNullOrEmpty(name))
				continue;

			Transform direct = root.Find(name);
			if (direct != null)
				return direct;
		}

		foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
		{
			foreach (string name in names)
			{
				if (!string.IsNullOrEmpty(name) && t.name == name)
					return t;
			}
		}

		return null;
	}
}
