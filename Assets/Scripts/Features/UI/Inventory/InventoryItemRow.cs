using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 한 줄 — 아이콘을 왼쪽·위부터 배치하고, 가로 공간이 넘치면 다음 줄로 줄바꿈합니다.
/// </summary>
public class InventoryItemRow : MonoBehaviour
{
	[SerializeField] RectTransform slotContainer;
	[SerializeField] GameObject slotPrefab;
	[SerializeField] float slotSize = 64f;
	[SerializeField] float slotSpacing = 8f;
	[SerializeField] float fallbackRowWidth = 960f;

	readonly List<GameObject> spawnedSlots = new List<GameObject>();
	GridLayoutGroup gridLayout;

	public void Rebuild(IReadOnlyList<InventorySlotViewData> items)
	{
		ClearSpawned();

		if (items == null || items.Count == 0)
			return;

		RectTransform parent = slotContainer != null ? slotContainer : transform as RectTransform;
		EnsureGridLayout(parent);

		for (int i = 0; i < items.Count; i++)
		{
			InventorySlotViewData data = items[i];
			if (data == null || data.icon == null)
				continue;

			GameObject slotGo = CreateSlotObject(parent, i);
			spawnedSlots.Add(slotGo);

			Image image = slotGo.GetComponent<Image>();
			image.sprite = data.icon;
			image.enabled = true;
			image.preserveAspect = true;
			image.raycastTarget = true;

			if (!slotGo.TryGetComponent(out InventorySlotHover hover))
				hover = slotGo.AddComponent<InventorySlotHover>();

			hover.SetTooltip(data.tooltip);
		}

		if (gridLayout == null)
			return;

		Canvas.ForceUpdateCanvases();
		gridLayout.constraintCount = CalculateColumnCount(parent);
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
	}

	void EnsureGridLayout(RectTransform parent)
	{
		if (parent == null)
			return;

		if (parent.TryGetComponent(out Image rowBackground))
			rowBackground.raycastTarget = false;

		gridLayout = parent.GetComponent<GridLayoutGroup>();

		LayoutGroup[] layoutGroups = parent.GetComponents<LayoutGroup>();
		foreach (LayoutGroup layout in layoutGroups)
		{
			if (layout == null || layout is GridLayoutGroup)
				continue;

			DestroyLayoutGroupImmediate(layout);
		}

		if (gridLayout == null)
			gridLayout = parent.gameObject.AddComponent<GridLayoutGroup>();

		if (gridLayout == null)
		{
			Debug.LogError($"[InventoryItemRow] {parent.name}에 GridLayoutGroup을 붙일 수 없습니다. Horizontal Layout Group을 Inspector에서 제거하세요.");
			return;
		}

		gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
		gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
		gridLayout.childAlignment = TextAnchor.UpperLeft;
		gridLayout.cellSize = new Vector2(slotSize, slotSize);
		gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
		gridLayout.padding = new RectOffset(0, 0, 0, 0);
		gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		gridLayout.constraintCount = CalculateColumnCount(parent);
	}

	int CalculateColumnCount(RectTransform parent)
	{
		float width = parent.rect.width;
		if (width < 2f)
			width = fallbackRowWidth;

		int columns = Mathf.FloorToInt((width + slotSpacing) / (slotSize + slotSpacing));
		return Mathf.Max(1, columns);
	}

	GameObject CreateSlotObject(RectTransform parent, int index)
	{
		GameObject slotGo;

		if (slotPrefab != null)
		{
			slotGo = Instantiate(slotPrefab, parent);
			slotGo.name = $"ItemSlot_{index}";
		}
		else
		{
			slotGo = new GameObject($"ItemSlot_{index}", typeof(RectTransform), typeof(Image), typeof(InventorySlotHover));
			slotGo.transform.SetParent(parent, false);
		}

		RectTransform rt = slotGo.GetComponent<RectTransform>();
		rt.localScale = Vector3.one;

		if (slotGo.TryGetComponent(out LayoutElement layoutElement))
			DestroyLayoutGroupImmediate(layoutElement);

		return slotGo;
	}

	static void DestroyLayoutGroupImmediate(Component component)
	{
		if (component != null)
			Object.DestroyImmediate(component);
	}

	void ClearSpawned()
	{
		foreach (GameObject slot in spawnedSlots)
		{
			if (slot != null)
				Destroy(slot);
		}

		spawnedSlots.Clear();
	}

	void OnDestroy()
	{
		ClearSpawned();
	}
}
