using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 패널 — InventoryUI와 동일한 Esc/일시정지 패턴.
/// Canvas 하위 ShopPanel + WeaponRow/AccessoryRow/PotionRow + MerchantPortrait/Dialogue 를 연결하세요.
/// </summary>
public class ShopUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] Button closeButton;
	[SerializeField] Button rerollButton;
	[SerializeField] TextMeshProUGUI rerollButtonLabel;

	[Header("# 줄 (빈 컨테이너 — 슬롯은 코드가 생성)")]
	[SerializeField] ShopItemRow weaponRow;
	[SerializeField] ShopItemRow accessoryRow;
	[SerializeField] ShopItemRow potionRow;

	[Header("# 표시")]
	[SerializeField] TextMeshProUGUI goldLabel;
	[SerializeField] TextMeshProUGUI messageLabel;
	[SerializeField] Image merchantPortrait;
	[SerializeField] TextMeshProUGUI merchantDialogueLabel;

	[Header("# 구역 라벨")]
	[SerializeField] TextMeshProUGUI weaponRowLabel;
	[SerializeField] TextMeshProUGUI accessoryRowLabel;
	[SerializeField] TextMeshProUGUI potionRowLabel;
	[SerializeField] string weaponSectionTitle = "무기";
	[SerializeField] string accessorySectionTitle = "악세서리";
	[SerializeField] string potionSectionTitle = "물약";

	[Header("# 툴팁")]
	[SerializeField] GameObject tooltipPanel;
	[SerializeField] TextMeshProUGUI tooltipLabel;
	[SerializeField] Vector2 tooltipOffset = new Vector2(12f, -12f);
	[SerializeField] Vector2 tooltipPadding = new Vector2(24f, 36f);
	[SerializeField] float tooltipMinWidth = 140f;
	[SerializeField] float tooltipMinHeight = 96f;
	[SerializeField] float tooltipMaxWidth = 360f;
	[SerializeField] float tooltipExtraHeight = 32f;
	[SerializeField] int tooltipSortingOrder = 200;

	[Header("# 슬롯 프레임")]
	[SerializeField] Sprite slotFrameSprite;
	[SerializeField] float slotIconPadding = 12f;

	[Header("# 카탈로그 (비우면 Resources/Data/ShopCatalogSettings)")]
	[SerializeField] ShopCatalogSettings catalog;

	[Header("# 폰트")]
	[SerializeField] TMP_FontAsset koreanFont;

	static ShopUI activeInstance;

	bool isOpen;
	bool initialized;
	bool pausedByShop;
	bool tooltipVisible;
	RectTransform tooltipRect;
	Transform currentTooltipAnchor;

	readonly List<ShopListing> weaponStock = new List<ShopListing>();
	readonly List<ShopListing> accessoryStock = new List<ShopListing>();
	readonly List<ShopListing> potionStock = new List<ShopListing>();

	int nextRerollCost;
	bool stockGeneratedForRun;

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
		ResumeGameIfPausedByShop();

		if (closeButton != null)
			closeButton.onClick.RemoveListener(Close);

		if (rerollButton != null)
			rerollButton.onClick.RemoveListener(TryPaidReroll);
	}

	void Update()
	{
		if (!isOpen)
			return;

		RefreshGoldOnly();
	}

	public void Toggle()
	{
		EnsureReady();

		// 패널만 비활성인데 isOpen이 true로 남은 경우 닫힘으로 간주
		if (isOpen && !IsPanelRootActiveInHierarchy())
			isOpen = false;

		if (isOpen)
			Close();
		else
			Open();
	}

	/// <summary>패널 참조·버튼 리스너 등 1회 초기화 (부트스트랩 후에도 재호출 가능).</summary>
	public void EnsureReady()
	{
		EnsureInitialized();
	}

	public void Open()
	{
		EnsureInitialized();

		if (panel == null)
		{
			Debug.LogError("[ShopUI] Panel이 없습니다. ShopPanel 루트에 ShopUI를 붙이고 Panel 필드를 비워 두세요.");
			return;
		}

		if (isOpen && !IsPanelRootActiveInHierarchy())
			isOpen = false;

		if (IsPanelOpen)
		{
			Refresh();
			return;
		}

		isOpen = true;
		ShowPanelRoot();
		EnsureStockForRun();
		TmpKoreanFontUtility.EnsureAllAccessoryGlyphs(koreanFont);
		RefreshMerchantPresentation();
		Refresh();
		OverlayPanelUILayout.Apply(panel.transform);
		PauseGameIfLive();
	}

	public void Close()
	{
		if (panel == null)
			return;

		isOpen = false;
		HidePanelRoot();
		HideTooltip();
		ResumeGameIfPausedByShop();
	}

	public bool TryHandleEscape()
	{
		if (!isOpen || panel == null || !panel.activeInHierarchy)
			return false;

		if (closeButton != null)
			closeButton.onClick.Invoke();
		else
			Close();

		return true;
	}

	/// <summary>상점 패널이 실제로 열려 있는지.</summary>
	public bool IsPanelOpen => isOpen && IsPanelRootActiveInHierarchy();

	void PauseGameIfLive()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return;

		GameManager.instance.PauseForOverlayPanel();
		pausedByShop = true;
	}

	void ResumeGameIfPausedByShop()
	{
		if (!pausedByShop || GameManager.instance == null)
			return;

		pausedByShop = false;
		GameManager.instance.ResumeGameplayFromOverlay();
	}

	void EnsureInitialized()
	{
		AutoBindReferences();
		ResolveCatalog();

		if (panel == null)
		{
			Debug.LogError("[ShopUI] ShopPanel을 찾지 못했습니다.");
			return;
		}

		if (initialized)
			return;

		initialized = true;

		if (!isOpen)
			HidePanelRoot();

		if (closeButton != null)
		{
			closeButton.onClick.AddListener(Close);
			EnsureCloseButtonPressedSprite(closeButton.gameObject);
		}

		if (rerollButton != null)
			rerollButton.onClick.AddListener(TryPaidReroll);

		BindRerollButtonLabel();
		UpdateRerollButtonLabel();

		if (tooltipLabel == null && tooltipPanel != null)
			tooltipLabel = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>(true);

		if (tooltipLabel != null)
			tooltipLabel.richText = true;

		EnsureTooltipOnTopLayer();
		EnsureSlotFrameSprite();
		ApplySlotVisualSettingsToRows();
		ApplySectionLabels();

		ResolveKoreanFont();
		TmpKoreanFontUtility.EnsureAllAccessoryGlyphs(koreanFont);
		TmpKoreanFontUtility.ApplyFontToAll(
			koreanFont,
			goldLabel,
			messageLabel,
			merchantDialogueLabel,
			rerollButtonLabel,
			tooltipLabel,
			weaponRowLabel,
			accessoryRowLabel,
			potionRowLabel);
		OverlayPanelUILayout.Apply(panel != null ? panel.transform : transform);
		SetMessage(string.Empty);
	}

	void AutoBindReferences()
	{
		if (panel == null)
		{
			if (gameObject.name == "ShopPanel" || gameObject.name.Contains("Shop"))
				panel = gameObject;
			else
			{
				Transform child = transform.Find("ShopPanel");
				if (child != null)
					panel = child.gameObject;
			}
		}

		if (panel == null)
		{
			GameObject found = ShopUIBootstrap.FindSceneObject("ShopPanel");
			if (found != null)
				panel = found;
		}

		// Panel 필드가 BoxPanel 등 자식을 가리키면 루트 ShopPanel로 통일
		if (panel != null && panel.name != "ShopPanel")
		{
			GameObject panelRoot = GetShopPanelRoot();
			if (panelRoot != null)
				panel = panelRoot;
		}

		Transform root = panel != null ? panel.transform : transform;

		if (goldLabel == null)
			goldLabel = FindTmpDeep(root, "GoldText") ?? FindTmpDeep(root, "GoldLabel");

		if (messageLabel == null)
			messageLabel = FindTmpDeep(root, "ShopMessage") ?? FindTmpDeep(root, "MessageText");

		if (merchantDialogueLabel == null)
			merchantDialogueLabel = FindTmpDeep(root, "MerchantDialogue") ?? FindTmpDeep(root, "MerchantText");

		if (merchantPortrait == null)
		{
			Transform portrait = FindDeepChild(root, "MerchantPortrait", "ShopkeeperPortrait");
			if (portrait != null)
				merchantPortrait = portrait.GetComponent<Image>();
		}

		if (tooltipPanel == null)
		{
			Transform tip = FindDeepChild(root, "ItemTooltip", "ShopTooltip", "TooltipPanel");
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

		if (rerollButton == null)
		{
			Transform reroll = root.Find("RerollBtn") ?? root.Find("RerollButton")
			                       ?? root.Find("RefreshBtn") ?? root.Find("Refresh");
			if (reroll != null)
				rerollButton = reroll.GetComponent<Button>();
		}

		if (rerollButton == null)
		{
			Transform merchant = FindDeepChild(root, "MerchantPortrait", "ShopkeeperPortrait");
			if (merchant != null && merchant.parent != null)
			{
				foreach (Button btn in merchant.parent.GetComponentsInChildren<Button>(true))
				{
					if (btn == closeButton || btn == rerollButton)
						continue;

					rerollButton = btn;
					break;
				}
			}
		}

		BindRerollButtonLabel();

		if (weaponRow == null)
			weaponRow = FindRow(root, "WeaponRow", "ShopWeaponRow");
		if (accessoryRow == null)
			accessoryRow = FindRow(root, "AccessoryRow", "ShopAccessoryRow");
		if (potionRow == null)
			potionRow = FindRow(root, "PotionRow", "ShopPotionRow");

		if (weaponRowLabel == null)
			weaponRowLabel = FindTmpDeep(root, "WeaponRowLabel") ?? FindTmpDeep(root, "WeaponLabel");
		if (accessoryRowLabel == null)
			accessoryRowLabel = FindTmpDeep(root, "AccessoryRowLabel") ?? FindTmpDeep(root, "AccessoryLabel");
		if (potionRowLabel == null)
			potionRowLabel = FindTmpDeep(root, "PotionRowLabel") ?? FindTmpDeep(root, "PotionLabel");
	}

	void ResolveCatalog()
	{
		if (catalog == null)
			catalog = ShopCatalogSettings.Instance;
	}

	void EnsureSlotFrameSprite()
	{
		if (slotFrameSprite != null)
			return;

		InventorySlotVisualSettings settings = InventorySlotVisualSettings.Instance;
		if (settings != null && settings.slotFrameSprite != null)
			slotFrameSprite = settings.slotFrameSprite;

#if UNITY_EDITOR
		if (slotFrameSprite == null)
			slotFrameSprite = ShopItemRow.LoadDefaultFrameSprite();
#endif
	}

	void ApplySlotVisualSettingsToRows()
	{
		ConfigureRow(weaponRow);
		ConfigureRow(accessoryRow);
		ConfigureRow(potionRow);
	}

	void ConfigureRow(ShopItemRow row)
	{
		if (row == null)
			return;

		row.ConfigureSlotVisual(slotFrameSprite, slotIconPadding, koreanFont);
	}

	public void RollStockInternal()
	{
		ResolveCatalog();
		if (catalog == null)
		{
			Debug.LogWarning("[ShopUI] ShopCatalogSettings를 찾지 못했습니다.");
			return;
		}

		weaponStock.Clear();
		accessoryStock.Clear();
		potionStock.Clear();

		weaponStock.AddRange(ShopStockGenerator.GenerateWeapons(catalog));
		accessoryStock.AddRange(ShopStockGenerator.GenerateAccessories(catalog));
		potionStock.AddRange(ShopStockGenerator.GeneratePotions(catalog));
		stockGeneratedForRun = true;
	}

	/// <summary>새 게임/메인 메뉴 복귀 시 진열·리롤 비용을 초기화합니다.</summary>
	public void ResetSession()
	{
		stockGeneratedForRun = false;
		nextRerollCost = 0;
		weaponStock.Clear();
		accessoryStock.Clear();
		potionStock.Clear();

		if (isOpen)
			Refresh();
	}

	void EnsureStockForRun()
	{
		if (stockGeneratedForRun)
			return;

		ResetRerollCost();
		RollStockInternal();
	}

	public void TryPaidReroll()
	{
		int cost = nextRerollCost;

		if (GameManager.instance == null)
		{
			SetMessage("게임 상태를 확인할 수 없습니다.");
			return;
		}

		if (cost > 0 && !GameManager.instance.TrySpendCoin(cost))
		{
			SetMessage($"리롤에 {cost}G가 필요합니다.");
			return;
		}

		RollStockInternal();
		if (cost > 0)
			nextRerollCost = cost * 2;

		Refresh();
		SetMessage(cost > 0 ? $"진열을 변경했습니다. (-{cost}G)" : "진열을 변경했습니다.");
	}

	/// <summary>레거시 — 무료 재진열 (디버그/외부 호출용).</summary>
	public void RerollStock()
	{
		RollStockInternal();
		Refresh();
	}

	public void Refresh()
	{
		ResolveCatalog();
		EnsureSlotFrameSprite();
		ApplySlotVisualSettingsToRows();

		HideTooltip();
		RefreshGoldOnly();
		RefreshWeaponRow();
		RefreshAccessoryRow();
		RefreshPotionRow();
		ShopPanelLayout.Apply(panel != null ? panel.transform : transform);
		ApplySectionLabels();
		UpdateRerollButtonLabel();
	}

	void BindRerollButtonLabel()
	{
		if (rerollButtonLabel != null)
			return;

		if (rerollButton == null)
			return;

		rerollButtonLabel = rerollButton.GetComponentInChildren<TextMeshProUGUI>(true);
	}

	void ResetRerollCost()
	{
		ResolveCatalog();
		nextRerollCost = catalog != null ? Mathf.Max(0, catalog.rerollCost) : 10;
	}

	void UpdateRerollButtonLabel()
	{
		BindRerollButtonLabel();
		if (rerollButtonLabel == null)
			return;

		int cost = nextRerollCost;
		rerollButtonLabel.text = cost > 0 ? $"리롤 {cost}G" : "리롤";
		TmpKoreanFontUtility.EnsureGlyphs(rerollButtonLabel, koreanFont, rerollButtonLabel.text);
	}

	void RefreshMerchantPresentation()
	{
		if (merchantPortrait != null)
		{
			Sprite portrait = catalog != null && catalog.merchantPortrait != null
				? catalog.merchantPortrait
				: null;

			merchantPortrait.sprite = portrait;
			merchantPortrait.enabled = portrait != null;
			merchantPortrait.preserveAspect = true;
			merchantPortrait.type = Image.Type.Simple;
		}

		if (merchantDialogueLabel == null)
			return;

		string[] lines = catalog != null && catalog.merchantDialogues != null && catalog.merchantDialogues.Length > 0
			? catalog.merchantDialogues
			: ShopDefaults.MerchantDialogues;

		string line = NormalizeMerchantDialogue(lines[Random.Range(0, lines.Length)]);
		merchantDialogueLabel.text = line;
		TmpKoreanFontUtility.EnsureGlyphs(merchantDialogueLabel, koreanFont, line);
	}

	static string NormalizeMerchantDialogue(string line)
	{
		if (string.IsNullOrEmpty(line))
			return line;

		if (line.Contains("챙겨") || line.Contains("언제뒠") || line.Contains("책임지"))
			return ShopDefaults.MerchantDialogues[3];

		return line;
	}

	void RefreshWeaponRow()
	{
		if (weaponRow == null)
			return;

		List<ShopSlotViewData> slots = ShopDisplayService.BuildSlots(weaponStock, TryPurchaseListing);
		weaponRow.Rebuild(slots);
	}

	void RefreshAccessoryRow()
	{
		if (accessoryRow == null)
			return;

		List<ShopSlotViewData> slots = ShopDisplayService.BuildSlots(accessoryStock, TryPurchaseListing);
		accessoryRow.Rebuild(slots);
	}

	void RefreshPotionRow()
	{
		if (potionRow == null)
			return;

		List<ShopSlotViewData> slots = ShopDisplayService.BuildSlots(potionStock, TryPurchaseListing);
		potionRow.Rebuild(slots);
	}

	void TryPurchaseListing(ShopListing listing)
	{
		if (ShopService.TryPurchase(listing, out string message))
		{
			SetMessage(message);
			Refresh();
			return;
		}

		SetMessage(message);
	}

	void SetMessage(string text)
	{
		if (messageLabel == null)
			return;

		string value = text ?? string.Empty;
		messageLabel.text = value;
		messageLabel.gameObject.SetActive(!string.IsNullOrEmpty(value));
		TmpKoreanFontUtility.EnsureGlyphs(messageLabel, koreanFont, value);

		if (!string.IsNullOrEmpty(value) && panel != null)
			OverlayPanelUILayout.Apply(panel.transform);
	}

	void RefreshGoldOnly()
	{
		if (goldLabel == null)
			return;

		int coin = GameManager.instance != null ? GameManager.instance.Coin : 0;
		goldLabel.text = $"코인 {coin}";
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
			return;

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

		DisableTooltipAutoLayout();

		tooltipLabel.textWrappingMode = TextWrappingModes.Normal;
		tooltipLabel.overflowMode = TextOverflowModes.Overflow;
		tooltipLabel.verticalAlignment = VerticalAlignmentOptions.Top;
		tooltipLabel.ForceMeshUpdate(true, true);

		Vector2 unconstrained = tooltipLabel.GetPreferredValues(0f, 0f);
		float width = Mathf.Clamp(unconstrained.x + tooltipPadding.x * 2f, tooltipMinWidth, tooltipMaxWidth);
		float innerWidth = Mathf.Max(1f, width - tooltipPadding.x * 2f);

		tooltipRect.sizeDelta = new Vector2(width, tooltipMinHeight);
		ApplyTooltipLabelInsets();

		tooltipLabel.ForceMeshUpdate(true, true);
		float textHeight = tooltipLabel.GetPreferredValues(innerWidth, 0f).y;
		textHeight = Mathf.Max(
			textHeight,
			tooltipLabel.preferredHeight,
			tooltipLabel.GetRenderedValues(false).y);

		float height = Mathf.Max(
			tooltipMinHeight,
			textHeight + tooltipPadding.y * 2f + tooltipExtraHeight);

		tooltipRect.sizeDelta = new Vector2(width, height);
		ApplyTooltipLabelInsets();
		tooltipLabel.ForceMeshUpdate(true, true);

		LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
	}

	void ApplyTooltipLabelInsets()
	{
		if (tooltipLabel == null)
			return;

		RectTransform labelRect = tooltipLabel.rectTransform;
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = new Vector2(tooltipPadding.x, tooltipPadding.y);
		labelRect.offsetMax = new Vector2(-tooltipPadding.x, -tooltipPadding.y);
	}

	void DisableTooltipAutoLayout()
	{
		if (tooltipPanel == null)
			return;

		if (tooltipPanel.TryGetComponent(out ContentSizeFitter fitter))
			fitter.enabled = false;

		if (tooltipPanel.TryGetComponent(out LayoutElement layoutElement))
		{
			layoutElement.minHeight = -1f;
			layoutElement.preferredHeight = -1f;
			layoutElement.flexibleHeight = -1f;
		}
	}

	void PositionTooltipNear(Transform anchor)
	{
		if (tooltipRect == null || anchor == null)
			return;

		if (anchor is not RectTransform anchorRect)
			return;

		ConfigureTooltipPivotForRightPlacement();

		Vector3[] corners = new Vector3[4];
		anchorRect.GetWorldCorners(corners);

		Vector3 right = anchorRect.TransformDirection(Vector3.right);
		tooltipRect.position = corners[2] + right * tooltipOffset.x;
	}

	static ShopItemRow FindRow(Transform root, params string[] names)
	{
		foreach (string name in names)
		{
			Transform t = FindDeepChild(root, name);
			if (t == null)
				continue;

			if (t.TryGetComponent(out ShopItemRow row))
				return row;

			return t.gameObject.AddComponent<ShopItemRow>();
		}

		return null;
	}

	static TextMeshProUGUI FindTmpDeep(Transform root, string objectName)
	{
		if (root == null)
			return null;

		foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			if (tmp.name == objectName)
				return tmp;
		}

		return null;
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

	GameObject GetShopPanelRoot()
	{
		if (panel == null)
			return gameObject;

		Transform t = panel.transform;
		Transform shopRoot = null;

		while (t != null)
		{
			if (t.name == "ShopPanel")
				shopRoot = t;

			if (t.GetComponent<Canvas>() != null)
				break;

			t = t.parent;
		}

		if (shopRoot != null)
			return shopRoot.gameObject;

		return panel;
	}

	void ShowPanelRoot()
	{
		GameObject root = GetShopPanelRoot();
		if (root == null)
			return;

		Transform t = root.transform;
		while (t != null)
		{
			t.gameObject.SetActive(true);
			if (t.GetComponent<Canvas>() != null)
				break;
			t = t.parent;
		}

		root.transform.SetAsLastSibling();
	}

	void HidePanelRoot()
	{
		GameObject root = GetShopPanelRoot();
		if (root != null)
			root.SetActive(false);
	}

	bool IsPanelRootActiveInHierarchy()
	{
		GameObject root = GetShopPanelRoot();
		return root != null && root.activeInHierarchy;
	}
}
