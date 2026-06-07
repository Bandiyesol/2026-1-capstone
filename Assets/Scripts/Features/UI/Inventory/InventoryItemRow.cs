using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 인벤토리 한 줄 — 슬롯 프레임 안에 아이콘을 표시하고, 가로 공간이 넘치면 줄바꿈합니다.
/// </summary>
public class InventoryItemRow : MonoBehaviour
{
	const string DefaultFrameAssetPath =
		"Assets/Arts/UI/Vol 6 Ui Expansion Pack/Panels/Panels_06.png";

	const string DefaultFrameSpriteName = "Panels_06_0";

	[SerializeField] RectTransform slotContainer;
	[SerializeField] GameObject slotPrefab;
	[SerializeField] Sprite slotFrameSprite;
	[SerializeField] float slotSize = 64f;
	[SerializeField] float slotSpacing = 8f;
	[SerializeField] float iconPadding = 12f;
	[SerializeField] float fallbackRowWidth = 960f;
	[SerializeField] float minRowHeight = 160f;
	[SerializeField] float layoutWidth = InventoryPanelLayout.RowWidth;

	readonly List<GameObject> spawnedSlots = new List<GameObject>();
	GridLayoutGroup gridLayout;

	public void ConfigureSlotVisual(Sprite frame, float padding)
	{
		if (frame != null)
			slotFrameSprite = frame;

		if (padding > 0f)
			iconPadding = padding;
	}

	public void Rebuild(IReadOnlyList<InventorySlotViewData> items)
	{
		EnsureSlotFrameSprite();
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

			GameObject slotGo = CreateSlotObject(parent, spawnedSlots.Count);
			spawnedSlots.Add(slotGo);
			ApplySlotVisuals(slotGo, data);
		}

		if (gridLayout == null)
			return;

		Canvas.ForceUpdateCanvases();
		gridLayout.constraintCount = CalculateColumnCount(parent);
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
		ResizeRowHeight(parent);
	}

	void ApplySlotVisuals(GameObject slotGo, InventorySlotViewData data)
	{
		if (!TryGetSlotImages(slotGo, out Image frameImage, out Image iconImage))
			BuildSlotHierarchy(slotGo, out frameImage, out iconImage);

		if (frameImage != null)
		{
			frameImage.sprite = slotFrameSprite;
			frameImage.type = Image.Type.Simple;
			frameImage.preserveAspect = false;
			frameImage.color = Color.white;
			frameImage.enabled = slotFrameSprite != null;
			frameImage.raycastTarget = true;
			StretchToParent(frameImage.rectTransform);
		}

		if (iconImage != null)
		{
			iconImage.sprite = data.icon;
			iconImage.enabled = true;
			iconImage.preserveAspect = true;
			iconImage.color = Color.white;
			iconImage.raycastTarget = false;
			ApplyIconInset(iconImage.rectTransform);
		}

		InventorySlotHover hover = frameImage != null
			? frameImage.GetComponent<InventorySlotHover>()
			: slotGo.GetComponent<InventorySlotHover>();

		if (hover == null)
		{
			GameObject hoverHost = frameImage != null ? frameImage.gameObject : slotGo;
			hover = hoverHost.AddComponent<InventorySlotHover>();
		}

		hover.SetTooltip(data.tooltip);
	}

	static bool TryGetSlotImages(GameObject slotGo, out Image frameImage, out Image iconImage)
	{
		frameImage = null;
		iconImage = null;

		Transform frame = slotGo.transform.Find("Frame");
		Transform icon = slotGo.transform.Find("Icon");

		if (frame != null)
			frame.TryGetComponent(out frameImage);
		if (icon != null)
			icon.TryGetComponent(out iconImage);

		return frameImage != null && iconImage != null;
	}

	void BuildSlotHierarchy(GameObject slotGo, out Image frameImage, out Image iconImage)
	{
		if (slotGo.TryGetComponent(out Image legacyRootImage))
		{
			legacyRootImage.enabled = false;
			legacyRootImage.raycastTarget = false;
		}

		if (slotGo.TryGetComponent(out InventorySlotHover legacyHover))
			DestroyLayoutGroupImmediate(legacyHover);

		GameObject frameGo = GetOrCreateChild(slotGo.transform, "Frame");
		frameImage = GetOrAddImage(frameGo);
		StretchToParent(frameGo.GetComponent<RectTransform>());

		GameObject iconGo = GetOrCreateChild(slotGo.transform, "Icon");
		iconImage = GetOrAddImage(iconGo);
		ApplyIconInset(iconGo.GetComponent<RectTransform>());
	}

	static GameObject GetOrCreateChild(Transform parent, string childName)
	{
		Transform existing = parent.Find(childName);
		if (existing != null)
			return existing.gameObject;

		var go = new GameObject(childName, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	static Image GetOrAddImage(GameObject go)
	{
		if (!go.TryGetComponent(out Image image))
			image = go.AddComponent<Image>();

		return image;
	}

	void ApplyIconInset(RectTransform iconRect)
	{
		iconRect.anchorMin = Vector2.zero;
		iconRect.anchorMax = Vector2.one;
		iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
		iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);
		iconRect.localScale = Vector3.one;
	}

	static void StretchToParent(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		rect.localScale = Vector3.one;
	}

	void EnsureSlotFrameSprite()
	{
		if (slotFrameSprite != null)
			return;

		InventorySlotVisualSettings settings = InventorySlotVisualSettings.Instance;
		if (settings != null && settings.slotFrameSprite != null)
		{
			slotFrameSprite = settings.slotFrameSprite;
			if (iconPadding <= 0f)
				iconPadding = settings.iconPadding;
		}

#if UNITY_EDITOR
		if (slotFrameSprite == null)
			slotFrameSprite = LoadDefaultFrameSprite();
#endif
	}

	public static Sprite LoadDefaultFrameSprite()
	{
#if UNITY_EDITOR
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(DefaultFrameAssetPath);
		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite && sprite.name == DefaultFrameSpriteName)
				return sprite;
		}

		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite)
				return sprite;
		}
#endif
		return null;
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
		if (width < layoutWidth * 0.75f)
			width = layoutWidth;
		if (width < 2f)
			width = Mathf.Max(fallbackRowWidth, layoutWidth);

		int columns = Mathf.FloorToInt((width + slotSpacing) / (slotSize + slotSpacing));
		return Mathf.Max(1, columns);
	}

	void ResizeRowHeight(RectTransform parent)
	{
		if (parent == null || gridLayout == null || spawnedSlots.Count == 0)
			return;

		int columns = Mathf.Max(1, gridLayout.constraintCount);
		int rowCount = Mathf.CeilToInt(spawnedSlots.Count / (float)columns);
		float height = rowCount * slotSize
		               + Mathf.Max(0, rowCount - 1) * slotSpacing
		               + gridLayout.padding.top
		               + gridLayout.padding.bottom;
		height = Mathf.Max(minRowHeight, height);
		parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
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
			slotGo = new GameObject($"ItemSlot_{index}", typeof(RectTransform));
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

#if UNITY_EDITOR
	void OnValidate()
	{
		if (slotFrameSprite == null)
			slotFrameSprite = LoadDefaultFrameSprite();
	}
#endif
}
