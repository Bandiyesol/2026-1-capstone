using UnityEngine;

/// <summary>상점 BoxPanel — 인벤토리와 별도 row·라벨 배치.</summary>
public static class ShopPanelLayout
{
	const float ContentLeft = -228f;
	public const float ContentWidth = 660f;

	// ShopItemRow: cell = slot(64) + price(26), 2줄 + spacing(8)
	public const float RowHeight = 188f;
	const float SectionGap = 24f;

	public static void Apply(Transform shopRoot)
	{
		if (shopRoot == null)
			return;

		Transform box = OverlayPanelUILayout.FindBoxPanel(shopRoot) ?? shopRoot;

		float weaponRowY = 230f;
		float accessoryRowY = weaponRowY - RowHeight - SectionGap;
		float potionRowY = accessoryRowY - RowHeight - SectionGap;
		float halfRow = RowHeight * 0.5f;

		ApplyChildRectLeft(box, "WeaponRowLabel", ContentLeft, weaponRowY + halfRow + 8f, new Vector2(ContentWidth, 36f));
		ApplyChildRectLeft(box, "WeaponRow", ContentLeft, weaponRowY, new Vector2(ContentWidth, RowHeight));
		ApplyChildRectLeft(box, "AccessoryRowLabel", ContentLeft, accessoryRowY + halfRow + 8f, new Vector2(ContentWidth, 36f));
		ApplyChildRectLeft(box, "AccessoryRow", ContentLeft, accessoryRowY, new Vector2(ContentWidth, RowHeight));
		ApplyChildRectLeft(box, "PotionRowLabel", ContentLeft, potionRowY + halfRow + 8f, new Vector2(ContentWidth, 36f));
		ApplyChildRectLeft(box, "PotionRow", ContentLeft, potionRowY, new Vector2(ContentWidth, RowHeight));
	}

	static void ApplyChildRectLeft(Transform box, string childName, float leftFromCenter, float y, Vector2 size)
	{
		if (FindChild(box, childName) is not RectTransform rect)
			return;

		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.anchoredPosition = new Vector2(leftFromCenter, y);
		rect.sizeDelta = size;
	}

	static Transform FindChild(Transform root, string name)
	{
		if (root == null || string.IsNullOrEmpty(name))
			return null;

		Transform direct = root.Find(name);
		if (direct != null)
			return direct;

		foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
		{
			if (t.name == name)
				return t;
		}

		return null;
	}
}
