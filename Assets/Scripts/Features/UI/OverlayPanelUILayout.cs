using TMPro;
using UnityEngine;

/// <summary>Status / Inventory / Setting / Shop 등 BoxPanel 오버레이 공통 크기·폰트.</summary>
public static class OverlayPanelUILayout
{
	public static float FrameWidth => ChoiceSelectUILayout.PanelWidth;
	public static float FrameHeight => ChoiceSelectUILayout.PanelHeight;

	const float TitleTopInset = 138f;
	const float TitleRowCenterInset = 174f;
	const float StatsLeftEdge = -540f;
	const float StatsMainWidth = 420f;
	const float StatsSideWidth = 380f;
	const float StatsColumnGap = 28f;
	const float StatsSideTopInset = 292f;
	const float ShopBottomBarY = 120f;
	const float ShopBottomBarInsetX = 300f;
	const float ShopBottomBarFontSize = 26f;
	const float ShopMerchantDialogueY = -88f;
	const float ShopRerollButtonY = -198f;

	public static void Apply(Transform panelRoot)
	{
		if (panelRoot == null)
			return;

		Transform box = FindBoxPanel(panelRoot);
		if (box is not RectTransform frame)
			return;

		frame.anchorMin = new Vector2(0.5f, 0.5f);
		frame.anchorMax = new Vector2(0.5f, 0.5f);
		frame.pivot = new Vector2(0.5f, 0.5f);
		frame.anchoredPosition = Vector2.zero;
		frame.sizeDelta = new Vector2(FrameWidth, FrameHeight);

		ApplyFonts(frame);
		ApplyTitleArea(frame);
		ApplyCloseButton(frame);

		string panelName = panelRoot.name;
		if (panelName.Contains("Status"))
			ApplyStatusLayout(frame);
		else if (panelName.Contains("Shop"))
		{
			ApplyShopLayout(frame);
			ShopPanelLayout.Apply(panelRoot);
		}
		else if (panelName.Contains("Inventory"))
			InventoryPanelLayout.Apply(panelRoot);
		else if (panelName.Contains("Setting"))
			SettingsPanelLayout.Apply(panelRoot);
	}

	public static Transform FindBoxPanel(Transform root)
	{
		if (root == null)
			return null;

		Transform direct = root.Find("BoxPanel");
		if (direct != null)
			return direct;

		foreach (Transform child in root)
		{
			if (child.name == "BoxPanel")
				return child;
		}

		foreach (Transform child in root)
		{
			if (child is RectTransform rt && rt.sizeDelta.x >= 1200f && rt.sizeDelta.y >= 800f)
				return child;
		}

		return null;
	}

	static void ApplyFonts(Transform box)
	{
		foreach (TextMeshProUGUI tmp in box.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			if (tmp == null)
				continue;

			string name = tmp.gameObject.name;
			if (name == "Price")
				continue;

			float size = ResolveFontSize(name, tmp.fontSize);
			tmp.fontSize = size;
			tmp.textWrappingMode = TextWrappingModes.Normal;
			tmp.lineSpacing = name.Contains("Stats") ? 4f : 2f;

			if (name == "Title")
			{
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
				tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
			}
			else if (name.Contains("Stats"))
			{
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Left;
				tmp.verticalAlignment = VerticalAlignmentOptions.Top;
			}
			else if (name is "MerchantDialogue" or "MerchantText")
			{
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
				tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
				tmp.textWrappingMode = TextWrappingModes.Normal;
			}
			else if (name is "GoldText" or "GoldLabel" or "goldLabel")
			{
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Right;
				tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
			}
			else if (name is "ShopMessage" or "MessageText")
			{
				tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
				tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
			}
		}
	}

	static float ResolveFontSize(string objectName, float current)
	{
		return objectName switch
		{
			"Title" => 34f,
			"StatsText" or "StatsTextSide" => 28f,
			"GoldLabel" or "goldLabel" or "GoldText" => ShopBottomBarFontSize,
			"MerchantDialogue" or "MerchantText" => 22f,
			"ShopMessage" or "MessageText" => ShopBottomBarFontSize,
			var n when n.Contains("RowLabel") || n.Contains("Section") => 26f,
			var n when n.EndsWith("Label") => 24f,
			var n when n.Contains("Detail") => 24f,
			_ => current <= 20f ? 24f : current
		};
	}

	static void ApplyTitleArea(Transform box)
	{
		Transform target = box.Find("Title");
		if (target is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -TitleTopInset);
		rect.sizeDelta = new Vector2(720f, 72f);
	}

	static void ApplyStatusLayout(Transform box)
	{
		float sideLeftEdge = StatsLeftEdge + StatsMainWidth + StatsColumnGap;

		// 모험가 — 내 정보와 같은 높이(왼쪽)
		ApplyTextRectTopLeft(box, "StatsText", StatsLeftEdge, TitleRowCenterInset, new Vector2(StatsMainWidth, 640f));
		ApplyTextRectTopLeft(box, "StatsTextSide", sideLeftEdge, StatsSideTopInset, new Vector2(StatsSideWidth, 520f));
		ApplyPortrait(box);
	}

	static void ApplyPortrait(Transform box)
	{
		Transform target = box.Find("Portrait");
		if (target is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(1f, 0.5f);
		rect.anchorMax = new Vector2(1f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(-350f, -58f);
		rect.sizeDelta = new Vector2(240f, 400f);
	}

	static void ApplyCloseButton(Transform box)
	{
		Transform target = box.Find("CloseBtn") ?? box.Find("Close");
		if (target is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(-200f, -170f);
		rect.sizeDelta = new Vector2(60f, 60f);
	}

	static void ApplyShopLayout(Transform box)
	{
		ApplyImageRect(box, "MerchantPortrait", new Vector2(-450f, 85f), new Vector2(180f, 180f));
		ApplyTextRect(box, "MerchantDialogue", new Vector2(-450f, ShopMerchantDialogueY), new Vector2(320f, 88f));
		ApplyTextRect(box, "MerchantText", new Vector2(-450f, ShopMerchantDialogueY), new Vector2(320f, 88f));
		ApplyChildRect(box, "RerollBtn", new Vector2(-450f, ShopRerollButtonY), new Vector2(170f, 46f));
		ApplyChildRect(box, "RerollButton", new Vector2(-450f, ShopRerollButtonY), new Vector2(170f, 46f));
		ApplyChildRect(box, "RefreshBtn", new Vector2(-450f, ShopRerollButtonY), new Vector2(170f, 46f));

		ApplyChildRect(box, "GoldText", new Vector2(-ShopBottomBarInsetX, ShopBottomBarY), new Vector2(240f, 44f), anchorBottomRight: true);
		ApplyChildRect(box, "GoldLabel", new Vector2(-ShopBottomBarInsetX, ShopBottomBarY), new Vector2(240f, 44f), anchorBottomRight: true);

		ApplyBottomCenterRect(box, "ShopMessage", ShopBottomBarY, 720f, 44f);
		ApplyBottomCenterRect(box, "MessageText", ShopBottomBarY, 720f, 44f);
	}

	static void ApplyTextRectTopLeft(Transform box, string childName, float leftFromCenter, float topInset, Vector2 size)
	{
		Transform target = box.Find(childName);
		if (target is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(leftFromCenter, -topInset);
		rect.sizeDelta = size;
	}

	static void ApplyBottomCenterRect(Transform box, string childName, float bottomY, float width, float height)
	{
		Transform target = box.Find(childName);
		if (target is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 0f);
		rect.anchorMax = new Vector2(0.5f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.anchoredPosition = new Vector2(0f, bottomY);
		rect.sizeDelta = new Vector2(width, height);
	}

	static void ApplyChildRect(Transform box, string childName, Vector2 anchoredPos, Vector2 size, bool anchorBottomRight = false)
	{
		Transform target = box.Find(childName);
		if (target == null)
			return;

		ApplyRectTransform(target as RectTransform, anchoredPos, size, anchorBottomRight);
	}

	static void ApplyImageRect(Transform box, string childName, Vector2 anchoredPos, Vector2 size)
	{
		ApplyChildRect(box, childName, anchoredPos, size);
	}

	static void ApplyTextRect(Transform box, string childName, Vector2 anchoredPos, Vector2 size, bool anchorBottomRight = false)
	{
		ApplyChildRect(box, childName, anchoredPos, size, anchorBottomRight);
	}

	static void ApplyRectTransform(RectTransform rect, Vector2 anchoredPos, Vector2 size, bool anchorBottomRight = false)
	{
		if (rect == null)
			return;

		if (anchorBottomRight)
		{
			rect.anchorMin = new Vector2(1f, 0f);
			rect.anchorMax = new Vector2(1f, 0f);
			rect.pivot = new Vector2(1f, 0f);
		}
		else
		{
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
		}

		rect.anchoredPosition = anchoredPos;
		rect.sizeDelta = size;
	}
}
