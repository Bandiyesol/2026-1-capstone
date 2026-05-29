using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 패널 — 아이템 획득 시 줄마다 아이콘이 자동 생성됩니다.
/// Unity 에서는 Row 컨테이너·툴팁·소지금·닫기만 배치하면 됩니다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] Button closeButton;

	[Header("# 줄 (빈 컨테이너만 — 슬롯은 코드가 생성)")]
	[SerializeField] InventoryItemRow weaponRow;
	[SerializeField] InventoryItemRow accessoryRow;
	[SerializeField] InventoryItemRow potionRow;

	[Header("# 표시")]
	[SerializeField] TextMeshProUGUI goldLabel;

	[Header("# 구역 라벨 (각 Row 위)")]
	[SerializeField] TextMeshProUGUI weaponRowLabel;
	[SerializeField] TextMeshProUGUI accessoryRowLabel;
	[SerializeField] TextMeshProUGUI potionRowLabel;
	[SerializeField] string weaponSectionTitle = "무기";
	[SerializeField] string accessorySectionTitle = "악세서리";
	[SerializeField] string potionSectionTitle = "물약";

	[Header("# 툴팁 (마우스 오버)")]
	[SerializeField] GameObject tooltipPanel;
	[SerializeField] TextMeshProUGUI tooltipLabel;
	[SerializeField] Vector2 tooltipOffset = new Vector2(8f, 0f);
	[SerializeField] Vector2 tooltipPadding = new Vector2(20f, 16f);
	[SerializeField] float tooltipMinWidth = 140f;
	[SerializeField] float tooltipMinHeight = 48f;
	[SerializeField] float tooltipMaxWidth = 360f;
	[SerializeField] int tooltipSortingOrder = 200;

	[Header("# 데이터 (비우면 Player 에서 자동 탐색)")]
	[SerializeField] WeaponInventory weaponInventory;
	[SerializeField] AccessoryInventory accessoryInventory;
	[SerializeField] PotionInventory potionInventory;

	[Header("# 폰트")]
	[SerializeField] TMP_FontAsset koreanFont;

	static InventoryUI activeInstance;

	bool isOpen;
	bool initialized;
	bool pausedByInventory;
	bool inventoriesSubscribed;
	bool tooltipVisible;
	RectTransform tooltipRect;
	Canvas rootCanvas;
	Transform currentTooltipAnchor;

	void Awake()
	{
		activeInstance = this;
		EnsureInitialized();
	}

	void OnDisable()
	{
		HideTooltip();
	}

	void OnEnable()
	{
		EnsureInitialized();
	}

	void OnDestroy()
	{
		ResumeGameIfPausedByInventory();
		UnsubscribeInventories();

		if (closeButton != null)
			closeButton.onClick.RemoveListener(Close);
	}

	void Update()
	{
		if (!isOpen)
			return;

		RefreshGoldOnly();
	}

	public void Toggle()
	{
		EnsureInitialized();

		if (isOpen)
			Close();
		else
			Open();
	}

	public void Open()
	{
		EnsureInitialized();

		if (panel == null)
		{
			Debug.LogError("[InventoryUI] Panel이 없습니다. InventoryPanel을 연결하세요.");
			return;
		}

		isOpen = true;
		panel.SetActive(true);
		Refresh();
		PauseGameIfLive();
	}

	public void Close()
	{
		if (panel == null)
			return;

		isOpen = false;
		panel.SetActive(false);
		ResumeGameIfPausedByInventory();
	}

	void PauseGameIfLive()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return;

		GameManager.instance.Stop();
		pausedByInventory = true;
	}

	void ResumeGameIfPausedByInventory()
	{
		if (!pausedByInventory || GameManager.instance == null)
			return;

		pausedByInventory = false;
		GameManager.instance.Resume();
	}

	void EnsureInitialized()
	{
		if (initialized)
			return;

		initialized = true;
		AutoBindReferences();
		ResolveInventories();
		SubscribeInventories();

		if (panel == null)
		{
			Debug.LogError("[InventoryUI] InventoryPanel을 찾지 못했습니다.");
			return;
		}

		if (!isOpen)
			panel.SetActive(false);

		if (closeButton != null)
		{
			closeButton.onClick.AddListener(Close);
			EnsureCloseButtonPressedSprite(closeButton.gameObject);
		}
		else
			Debug.LogWarning("[InventoryUI] CloseBtn을 연결하세요.");

		if (tooltipLabel == null && tooltipPanel != null)
			tooltipLabel = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>(true);

		rootCanvas = panel != null ? panel.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();

		EnsureTooltipOnTopLayer();
		ApplySectionLabels();

		ResolveKoreanFont();
		TmpKoreanFontUtility.ApplyFontToAll(
			koreanFont,
			goldLabel,
			tooltipLabel,
			weaponRowLabel,
			accessoryRowLabel,
			potionRowLabel);
	}

	void AutoBindReferences()
	{
		if (panel == null)
		{
			if (gameObject.name.Contains("Inventory"))
				panel = gameObject;
			else
			{
				Transform child = transform.Find("InventoryPanel");
				if (child != null)
					panel = child.gameObject;
			}
		}

		if (panel == null)
		{
			GameObject found = GameObject.Find("InventoryPanel");
			if (found != null)
				panel = found;
		}

		Transform root = panel != null ? panel.transform : transform;

		if (goldLabel == null)
			goldLabel = FindTmpDeep(root, "GoldText") ?? FindTmpDeep(root, "GoldLabel");

		if (tooltipPanel == null)
		{
			Transform tip = FindDeepChild(root, "ItemTooltip", "itemTooltip", "TooltipPanel");
			if (tip != null)
				tooltipPanel = tip.gameObject;
		}

		if (tooltipLabel == null && tooltipPanel != null)
			tooltipLabel = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>(true);

		if (closeButton == null)
		{
			Transform close = root.Find("CloseBtn") ?? root.Find("Close");
			if (close != null)
				closeButton = close.GetComponent<Button>();
		}

		if (weaponRow == null)
			weaponRow = FindRow(root, "WeaponRow", "WeaponSlots");
		if (accessoryRow == null)
			accessoryRow = FindRow(root, "AccessoryRow", "AccRow", "AccessorySlots");
		if (potionRow == null)
			potionRow = FindRow(root, "PotionRow", "PotionSlots");

		if (weaponRowLabel == null)
			weaponRowLabel = FindTmpDeep(root, "WeaponRowLabel") ?? FindTmpDeep(root, "WeaponLabel");
		if (accessoryRowLabel == null)
			accessoryRowLabel = FindTmpDeep(root, "AccessoryRowLabel") ?? FindTmpDeep(root, "AccessoryLabel");
		if (potionRowLabel == null)
			potionRowLabel = FindTmpDeep(root, "PotionRowLabel") ?? FindTmpDeep(root, "PotionLabel");
	}

	static InventoryItemRow FindRow(Transform root, params string[] names)
	{
		foreach (string name in names)
		{
			Transform t = FindDeepChild(root, name);
			if (t == null)
				continue;

			if (t.TryGetComponent(out InventoryItemRow row))
				return row;

			return t.gameObject.AddComponent<InventoryItemRow>();
		}

		return null;
	}

	void ResolveInventories()
	{
		if (weaponInventory == null)
			weaponInventory = FindFirstObjectByType<WeaponInventory>();

		if (accessoryInventory == null)
			accessoryInventory = FindFirstObjectByType<AccessoryInventory>();

		if (potionInventory == null)
			potionInventory = FindFirstObjectByType<PotionInventory>();
	}

	void SubscribeInventories()
	{
		if (inventoriesSubscribed)
			return;

		inventoriesSubscribed = true;

		if (weaponInventory != null)
			weaponInventory.OnInventoryChanged += Refresh;

		if (accessoryInventory != null)
			accessoryInventory.OnInventoryChanged += Refresh;

		if (potionInventory != null)
			potionInventory.OnInventoryChanged += Refresh;
	}

	void UnsubscribeInventories()
	{
		if (!inventoriesSubscribed)
			return;

		inventoriesSubscribed = false;

		if (weaponInventory != null)
			weaponInventory.OnInventoryChanged -= Refresh;

		if (accessoryInventory != null)
			accessoryInventory.OnInventoryChanged -= Refresh;

		if (potionInventory != null)
			potionInventory.OnInventoryChanged -= Refresh;
	}

	public void Refresh()
	{
		ResolveInventories();

		HideTooltip();
		RefreshGoldOnly();
		RefreshWeaponRow();
		RefreshAccessoryRow();
		RefreshPotionRow();
		ApplySectionLabels();

		if (goldLabel != null)
			TmpKoreanFontUtility.EnsureGlyphs(goldLabel, koreanFont, goldLabel.text);
	}

	public static void ShowTooltipStatic(string text, Transform anchor)
	{
		if (activeInstance != null)
			activeInstance.ShowTooltip(text, anchor);
	}

	public static void HideTooltipStatic()
	{
		if (activeInstance != null)
			activeInstance.HideTooltip();
	}

	public void ShowTooltip(string text, Transform anchor)
	{
		if (string.IsNullOrEmpty(text))
			return;

		if (tooltipPanel == null || tooltipLabel == null)
			AutoBindReferences();

		if (tooltipPanel == null || tooltipLabel == null)
		{
			Debug.LogWarning("[InventoryUI] ItemTooltip / Tooltip Label이 연결되지 않았습니다. BoxPanel 아래 ItemTooltip + TMP를 확인하세요.");
			return;
		}

		EnsureTooltipIgnoresRaycasts();

		bool sameAnchor = tooltipVisible && currentTooltipAnchor == anchor;
		currentTooltipAnchor = anchor;

		if (!sameAnchor || tooltipLabel.text != text)
		{
			tooltipLabel.text = text;
			TmpKoreanFontUtility.EnsureGlyphs(tooltipLabel, koreanFont, text);
		}

		EnsureTooltipOnTopLayer();

		if (!tooltipPanel.activeSelf)
			tooltipPanel.SetActive(true);

		tooltipVisible = true;

		if (tooltipRect == null)
			tooltipRect = tooltipPanel.GetComponent<RectTransform>();

		ResizeTooltipToFitText();
		PositionTooltipNear(anchor);
	}

	public void HideTooltip()
	{
		tooltipVisible = false;
		currentTooltipAnchor = null;

		if (tooltipPanel != null)
			tooltipPanel.SetActive(false);
	}

	void ApplySectionLabels()
	{
		SetSectionLabel(weaponRowLabel, weaponSectionTitle);
		SetSectionLabel(accessoryRowLabel, accessorySectionTitle);
		SetSectionLabel(potionRowLabel, potionSectionTitle);
	}

	static void SetSectionLabel(TextMeshProUGUI label, string title)
	{
		if (label == null)
			return;

		label.text = title;
		label.gameObject.SetActive(!string.IsNullOrEmpty(title));
	}

	void EnsureTooltipOnTopLayer()
	{
		if (tooltipPanel == null || panel == null)
			return;

		Transform panelRoot = panel.transform;
		if (tooltipPanel.transform.parent != panelRoot)
			tooltipPanel.transform.SetParent(panelRoot, false);

		tooltipPanel.transform.SetAsLastSibling();

		if (tooltipRect == null)
			tooltipRect = tooltipPanel.GetComponent<RectTransform>();

		tooltipPanel.SetActive(false);
		EnsureTooltipIgnoresRaycasts();
		ConfigureTooltipPivotForRightPlacement();

		Canvas tooltipCanvas = tooltipPanel.GetComponent<Canvas>();
		if (tooltipCanvas == null)
			tooltipCanvas = tooltipPanel.AddComponent<Canvas>();

		tooltipCanvas.overrideSorting = true;
		tooltipCanvas.sortingOrder = tooltipSortingOrder;

		if (tooltipPanel.GetComponent<GraphicRaycaster>() == null)
			tooltipPanel.AddComponent<GraphicRaycaster>();

		EnsureTooltipIgnoresRaycasts();
	}

	void EnsureTooltipIgnoresRaycasts()
	{
		if (tooltipPanel == null)
			return;

		if (!tooltipPanel.TryGetComponent(out CanvasGroup canvasGroup))
			canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

		canvasGroup.blocksRaycasts = false;
		canvasGroup.interactable = false;

		foreach (Graphic graphic in tooltipPanel.GetComponentsInChildren<Graphic>(true))
			graphic.raycastTarget = false;
	}

	void ConfigureTooltipPivotForRightPlacement()
	{
		if (tooltipRect == null)
			return;

		tooltipRect.pivot = new Vector2(0f, 1f);
		tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
		tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
	}

	void ResizeTooltipToFitText()
	{
		if (tooltipRect == null || tooltipLabel == null)
			return;

		tooltipLabel.textWrappingMode = TextWrappingModes.Normal;
		tooltipLabel.overflowMode = TextOverflowModes.Overflow;
		tooltipLabel.ForceMeshUpdate();

		Vector2 preferred = tooltipLabel.GetPreferredValues(tooltipMaxWidth, 0f);
		if (preferred.x <= 1f || preferred.y <= 1f)
			preferred = tooltipLabel.GetPreferredValues();

		float width = Mathf.Clamp(preferred.x + tooltipPadding.x, tooltipMinWidth, tooltipMaxWidth);
		float height = Mathf.Max(tooltipMinHeight, preferred.y + tooltipPadding.y);
		tooltipRect.sizeDelta = new Vector2(width, height);

		RectTransform labelRect = tooltipLabel.rectTransform;
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = new Vector2(tooltipPadding.x * 0.5f, tooltipPadding.y * 0.5f);
		labelRect.offsetMax = new Vector2(-tooltipPadding.x * 0.5f, -tooltipPadding.y * 0.5f);

		LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
	}

	void PositionTooltipNear(Transform anchor)
	{
		if (tooltipRect == null || anchor == null)
			return;

		RectTransform anchorRect = anchor as RectTransform;
		if (anchorRect == null)
			return;

		ConfigureTooltipPivotForRightPlacement();

		Vector3[] corners = new Vector3[4];
		anchorRect.GetWorldCorners(corners);

		// 슬롯 오른쪽 위 → 툴팁 왼쪽 위 (월드 좌표 — 부모 앵커와 무관)
		Vector3 right = anchorRect.TransformDirection(Vector3.right);
		tooltipRect.position = corners[2] + right * tooltipOffset.x;
	}

	void RefreshGoldOnly()
	{
		if (goldLabel == null)
			return;

		int coin = GameManager.instance != null ? GameManager.instance.Coin : 0;
		goldLabel.text = $"코인 {coin}";
	}

	void RefreshWeaponRow()
	{
		if (weaponRow == null)
			return;

		IReadOnlyList<WeaponInstance> source = weaponInventory != null ? weaponInventory.Weapons : null;
		List<InventorySlotViewData> slots = InventoryDisplayService.BuildWeaponSlots(source);
		weaponRow.Rebuild(slots);
	}

	void RefreshAccessoryRow()
	{
		if (accessoryRow == null)
			return;

		IReadOnlyList<AccessoryData> source = accessoryInventory != null ? accessoryInventory.Accessories : null;
		List<InventorySlotViewData> slots = InventoryDisplayService.BuildAccessorySlots(source);
		accessoryRow.Rebuild(slots);
	}

	void RefreshPotionRow()
	{
		if (potionRow == null)
			return;

		IReadOnlyList<PotionInventory.PotionStack> source = potionInventory != null ? potionInventory.Stacks : null;
		List<InventorySlotViewData> slots = InventoryDisplayService.BuildPotionSlots(source);
		potionRow.Rebuild(slots);
	}

	static TextMeshProUGUI FindTmpDeep(Transform root, string objectName)
	{
		if (root == null)
			return null;

		foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			if (tmp.name == objectName)
				return tmp;
		}

		return null;
	}

	static Transform FindDeepChild(Transform parent, string name)
	{
		return FindDeepChild(parent, new[] { name });
	}

	static Transform FindDeepChild(Transform parent, params string[] names)
	{
		if (parent == null || names == null)
			return null;

		foreach (string name in names)
		{
			if (parent.name == name)
				return parent;
		}

		foreach (Transform child in parent)
		{
			Transform found = FindDeepChild(child, names);
			if (found != null)
				return found;
		}

		return null;
	}

	void ResolveKoreanFont()
	{
		if (koreanFont != null)
			return;

#if UNITY_EDITOR
		koreanFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
#endif
	}

	static void EnsureCloseButtonPressedSprite(GameObject closeBtnObject)
	{
		if (closeBtnObject == null)
			return;

		if (!closeBtnObject.TryGetComponent(out PixelButtonSpriteSwap swap))
			swap = closeBtnObject.AddComponent<PixelButtonSpriteSwap>();

		swap.Apply();
	}
}
