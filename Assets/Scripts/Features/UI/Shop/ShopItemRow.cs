using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 상점 한 줄 — 프레임+아이콘(위) / 가격(아래) 분리 배치.
/// </summary>
public class ShopItemRow : MonoBehaviour
{
	const string DefaultFrameAssetPath =
		"Assets/Arts/UI/Vol 6 Ui Expansion Pack/Panels/Panels_06.png";

	const string DefaultFrameSpriteName = "Panels_06_0";

	[SerializeField] RectTransform slotContainer;
	[SerializeField] GameObject slotPrefab;
	[SerializeField] Sprite slotFrameSprite;
	[SerializeField] float slotSize = 64f;
	[SerializeField] float priceLabelHeight = 26f;
	[SerializeField] float slotSpacing = 8f;
	[SerializeField] float iconPadding = 10f;
	[SerializeField] float layoutWidth = ShopPanelLayout.ContentWidth;
	[SerializeField] TMP_FontAsset priceFont;
	[SerializeField] int priceFontSize = 24;
	[SerializeField] Color priceColor = new Color(1f, 0.92f, 0.45f);
	[SerializeField] Color soldOutColor = new Color(0.65f, 0.65f, 0.65f);

	readonly List<GameObject> spawnedSlots = new List<GameObject>();
	GridLayoutGroup gridLayout;

	public void ConfigureSlotVisual(Sprite frame, float padding, TMP_FontAsset font)
	{
		if (frame != null)
			slotFrameSprite = frame;

		if (padding > 0f)
			iconPadding = padding;

		if (font != null)
			priceFont = font;
	}

	public void Rebuild(IReadOnlyList<ShopSlotViewData> items)
	{
		EnsureSlotFrameSprite();
		ClearSpawned();

		if (items == null || items.Count == 0)
			return;

		RectTransform parent = slotContainer != null ? slotContainer : transform as RectTransform;
		EnsureGridLayout(parent);

		for (int i = 0; i < items.Count; i++)
		{
			ShopSlotViewData data = items[i];
			if (data == null)
				continue;

			GameObject slotGo = CreateSlotObject(parent, i);
			spawnedSlots.Add(slotGo);
			ApplySlotVisuals(slotGo, data);
		}

		if (gridLayout == null)
			return;

		Canvas.ForceUpdateCanvases();
		gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		gridLayout.constraintCount = CalculateColumnCount(parent);
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
	}

	void ApplySlotVisuals(GameObject slotGo, ShopSlotViewData data)
	{
		BuildSlotHierarchy(slotGo, out Image frameImage, out Image iconImage, out TextMeshProUGUI priceLabel);

		float priceBand = priceLabelHeight;

		if (frameImage != null)
		{
			frameImage.sprite = slotFrameSprite;
			frameImage.type = Image.Type.Simple;
			frameImage.preserveAspect = false;
			frameImage.color = data.soldOut ? soldOutColor : Color.white;
			frameImage.enabled = slotFrameSprite != null;
			frameImage.raycastTarget = true;
			LayoutIconBand(frameImage.rectTransform, priceBand, 0f);
		}

		if (iconImage != null)
		{
			iconImage.sprite = data.icon;
			iconImage.enabled = data.icon != null;
			iconImage.preserveAspect = true;
			iconImage.color = data.soldOut ? soldOutColor : Color.white;
			iconImage.raycastTarget = false;
			LayoutIconBand(iconImage.rectTransform, priceBand, iconPadding);
		}

		if (priceLabel != null)
		{
			priceLabel.text = data.soldOut ? "품절" : $"{data.price}G";
			priceLabel.color = data.soldOut ? soldOutColor : priceColor;
			priceLabel.fontSize = priceFontSize;
			priceLabel.alignment = TextAlignmentOptions.Center;
			priceLabel.overflowMode = TextOverflowModes.Overflow;
			priceLabel.raycastTarget = false;
			if (priceFont != null)
			{
				priceLabel.font = priceFont;
				TmpKoreanFontUtility.EnsureGlyphs(priceLabel, priceFont, priceLabel.text);
			}

			LayoutPriceBand(priceLabel.rectTransform, priceBand);
		}

		ShopSlotInteract interact = frameImage != null
			? frameImage.GetComponent<ShopSlotInteract>()
			: slotGo.GetComponent<ShopSlotInteract>();

		if (interact == null)
		{
			GameObject host = frameImage != null ? frameImage.gameObject : slotGo;
			interact = host.AddComponent<ShopSlotInteract>();
		}

		interact.Setup(data.tooltip, data.price, data.soldOut, data.onPurchase);
	}

	/// <summary>슬롯 상단 — 프레임·아이콘 영역.</summary>
	static void LayoutIconBand(RectTransform rect, float priceBandHeight, float padding)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(padding, priceBandHeight + padding);
		rect.offsetMax = new Vector2(-padding, -padding);
		rect.localScale = Vector3.one;
	}

	/// <summary>슬롯 하단 — 가격 텍스트 전용 띠 (프레임 밖).</summary>
	static void LayoutPriceBand(RectTransform rect, float height)
	{
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.offsetMin = new Vector2(2f, 0f);
		rect.offsetMax = new Vector2(-2f, height);
		rect.localScale = Vector3.one;
	}

	void BuildSlotHierarchy(
		GameObject slotGo,
		out Image frameImage,
		out Image iconImage,
		out TextMeshProUGUI priceLabel)
	{
		frameImage = null;
		iconImage = null;
		priceLabel = null;

		Transform frame = slotGo.transform.Find("Frame");
		Transform icon = slotGo.transform.Find("Icon");
		Transform price = slotGo.transform.Find("Price");

		if (frame != null)
			frame.TryGetComponent(out frameImage);
		if (icon != null)
			icon.TryGetComponent(out iconImage);
		if (price != null)
			price.TryGetComponent(out priceLabel);

		if (frameImage == null)
		{
			GameObject frameGo = GetOrCreateChild(slotGo.transform, "Frame");
			frameImage = GetOrAddImage(frameGo);
		}

		if (iconImage == null)
		{
			GameObject iconGo = GetOrCreateChild(slotGo.transform, "Icon");
			iconImage = GetOrAddImage(iconGo);
		}

		if (priceLabel == null)
		{
			GameObject priceGo = GetOrCreateChild(slotGo.transform, "Price");
			priceLabel = GetOrAddTmp(priceGo);
			priceGo.transform.SetAsLastSibling();
		}
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

	static TextMeshProUGUI GetOrAddTmp(GameObject go)
	{
		if (!go.TryGetComponent(out TextMeshProUGUI label))
			label = go.AddComponent<TextMeshProUGUI>();

		return label;
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

			if (layout != null)
				DestroyImmediate(layout);
		}

		if (gridLayout == null)
			gridLayout = parent.gameObject.AddComponent<GridLayoutGroup>();

		float cellHeight = slotSize + priceLabelHeight;
		gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
		gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
		gridLayout.childAlignment = TextAnchor.UpperLeft;
		gridLayout.cellSize = new Vector2(slotSize, cellHeight);
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
			width = ShopPanelLayout.ContentWidth;

		int columns = Mathf.FloorToInt((width + slotSpacing) / (slotSize + slotSpacing));
		return Mathf.Max(1, columns);
	}

	GameObject CreateSlotObject(RectTransform parent, int index)
	{
		GameObject slotGo;

		if (slotPrefab != null)
		{
			slotGo = Instantiate(slotPrefab, parent);
			slotGo.name = $"ShopSlot_{index}";
		}
		else
		{
			slotGo = new GameObject($"ShopSlot_{index}", typeof(RectTransform));
			slotGo.transform.SetParent(parent, false);
		}

		slotGo.GetComponent<RectTransform>().localScale = Vector3.one;

		if (slotGo.TryGetComponent(out LayoutElement layoutElement))
			DestroyImmediate(layoutElement);

		return slotGo;
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
