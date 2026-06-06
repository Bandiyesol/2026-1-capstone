using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Status 패널 표시. StatusPanel(비활성 가능)에 붙이고, HUD 버튼에는 StatusHudButton을 사용하세요.
/// </summary>
public class StatusUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] Button closeButton;

	[Header("# 표시")]
	[SerializeField] TextMeshProUGUI titleLabel;
	[SerializeField] TextMeshProUGUI statsLabel;
	[SerializeField] TextMeshProUGUI statsSideLabel;
	[SerializeField] Image portraitImage;

	[Header("# 캐릭터")]
	[SerializeField] string playerDisplayName = "모험가";
	[SerializeField] Sprite portraitOverride;
	[SerializeField] bool usePlayerSpriteAsPortrait = true;

	[Header("# 폰트")]
	[SerializeField] TMP_FontAsset koreanFont;

	bool isOpen;
	bool initialized;
	bool pausedByStatus;
	PlayerStats subscribedStats;

	void Awake()
	{
		EnsureInitialized();
	}

	void OnEnable()
	{
		EnsureInitialized();
	}

	void OnDestroy()
	{
		ResumeGameIfPausedByStatus();
		UnsubscribeStats();

		if (closeButton != null)
			closeButton.onClick.RemoveListener(Close);
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
			Debug.LogError("[StatusUI] Panel이 없습니다. Inspector의 Panel에 StatusPanel을 연결하세요.");
			return;
		}

		isOpen = true;
		panel.SetActive(true);
		SubscribeStats();
		Refresh();
		PauseGameIfLive();
	}

	public void Close()
	{
		if (panel == null)
			return;

		isOpen = false;
		panel.SetActive(false);
		UnsubscribeStats();
		ResumeGameIfPausedByStatus();
	}

	/// <summary>Esc — CloseBtn 과 동일.</summary>
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

	void PauseGameIfLive()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return;

		GameManager.instance.PauseForOverlayPanel();
		pausedByStatus = true;
	}

	void ResumeGameIfPausedByStatus()
	{
		if (!pausedByStatus || GameManager.instance == null)
			return;

		pausedByStatus = false;
		GameManager.instance.ResumeGameplayFromOverlay();
	}

	void EnsureInitialized()
	{
		if (initialized)
			return;

		initialized = true;
		AutoBindReferences();

		if (panel == null)
		{
			Debug.LogError("[StatusUI] StatusPanel을 찾지 못했습니다. Panel 필드에 StatusPanel을 연결하세요.");
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
			Debug.LogWarning("[StatusUI] Close Button이 비어 있습니다. CloseBtn을 연결하세요.");

		ResolveKoreanFont();
		TmpKoreanFontUtility.ApplyFontToAll(koreanFont, titleLabel, statsLabel, statsSideLabel);
	}

	void AutoBindReferences()
	{
		if (panel == null)
		{
			if (gameObject.name.Contains("Panel") || gameObject.name.Contains("Status"))
				panel = gameObject;
			else
			{
				Transform child = transform.Find("StatusPanel");
				if (child != null)
					panel = child.gameObject;
			}
		}

		if (panel == null)
		{
			GameObject found = GameObject.Find("StatusPanel");
			if (found != null)
				panel = found;
		}

		Transform root = panel != null ? panel.transform : transform;
		Transform boxPanel = FindBoxPanel(root);

		if (titleLabel == null)
			titleLabel = FindTmpDeep(root, "Title");

		if (statsLabel == null)
		{
			if (boxPanel != null)
				statsLabel = FindTmpDeep(boxPanel, "StatsText") ?? FindTmpDeep(boxPanel, "Stats");
			if (statsLabel == null)
				statsLabel = FindTmpDeep(root, "StatsText") ?? FindTmpDeep(root, "Stats");
		}

		if (statsSideLabel == null)
		{
			if (boxPanel != null)
				statsSideLabel = FindTmpDeep(boxPanel, "StatsTextSide") ?? FindTmpDeep(boxPanel, "StatsSide");
			if (statsSideLabel == null)
				statsSideLabel = FindTmpDeep(root, "StatsTextSide") ?? FindTmpDeep(root, "StatsSide");
		}

		if (portraitImage == null)
		{
			Transform portrait = boxPanel != null ? boxPanel.Find("Portrait") : null;
			if (portrait == null)
				portrait = root.Find("Portrait");
			if (portrait != null)
				portraitImage = portrait.GetComponent<Image>();
		}

		if (closeButton == null)
		{
			Transform close = root.Find("CloseBtn") ?? root.Find("Close");
			if (close != null)
				closeButton = close.GetComponent<Button>();
		}
	}

	static Transform FindBoxPanel(Transform root)
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

		return null;
	}

	static void EnsureCloseButtonPressedSprite(GameObject closeBtnObject)
	{
		if (closeBtnObject == null)
			return;

		if (!closeBtnObject.TryGetComponent(out PixelButtonSpriteSwap swap))
			swap = closeBtnObject.AddComponent<PixelButtonSpriteSwap>();

		swap.Apply();
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

	void SubscribeStats()
	{
		UnsubscribeStats();
		subscribedStats = PlayerStats.Instance;
		if (subscribedStats != null)
			subscribedStats.OnStatsChanged += Refresh;
	}

	void UnsubscribeStats()
	{
		if (subscribedStats != null)
			subscribedStats.OnStatsChanged -= Refresh;
		subscribedStats = null;
	}

	public void Refresh()
	{
		PlayerStats stats = PlayerStats.Instance;
		if (stats == null)
			stats = FindFirstObjectByType<PlayerStats>();

		const string titleText = "내 정보";
		string statsHeader = BuildStatsHeader(stats);
		string mainStats = PlayerStatsDisplay.BuildMainColumn(stats);
		string sideStats = PlayerStatsDisplay.BuildSideColumn(stats);

		string mainText = string.IsNullOrEmpty(statsHeader)
			? mainStats
			: statsHeader + "\n\n" + mainStats;

		if (titleLabel != null)
			titleLabel.text = titleText;

		if (statsLabel != null)
			statsLabel.text = mainText;

		if (statsSideLabel != null)
			statsSideLabel.text = sideStats;

		ResolveKoreanFont();
		string allDynamicText = titleText + mainText + sideStats;
		TmpKoreanFontUtility.ApplyFontToAll(koreanFont, titleLabel, statsLabel, statsSideLabel);
		TmpKoreanFontUtility.EnsureStatusPanelFonts(koreanFont, statsLabel, allDynamicText);
		if (statsSideLabel != null)
			TmpKoreanFontUtility.EnsureGlyphs(statsSideLabel, koreanFont, sideStats);
		if (titleLabel != null)
			TmpKoreanFontUtility.EnsureGlyphs(titleLabel, koreanFont, titleText);

		RefreshPortrait();
	}

	void ResolveKoreanFont()
	{
		if (koreanFont != null)
			return;

#if UNITY_EDITOR
		koreanFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
#endif
	}

	void RefreshPortrait()
	{
		if (portraitImage == null)
			return;

		Sprite sprite = portraitOverride;
		if (sprite == null && usePlayerSpriteAsPortrait)
			sprite = ResolvePlayerSprite();

		portraitImage.sprite = sprite;
		portraitImage.enabled = sprite != null;
		portraitImage.preserveAspect = true;
	}

	static Sprite ResolvePlayerSprite()
	{
		if (GameManager.instance != null && GameManager.instance.player != null)
		{
			var renderer = GameManager.instance.player.spriter;
			if (renderer != null && renderer.sprite != null)
				return renderer.sprite;
		}

		Player player = FindFirstObjectByType<Player>();
		if (player != null && player.spriter != null)
			return player.spriter.sprite;

		return null;
	}

	string BuildStatsHeader(PlayerStats stats)
	{
		var sb = new StringBuilder();
		sb.AppendLine(playerDisplayName);

		if (stats != null)
			sb.AppendLine($"HP {stats.CurrentHP:F0} / {stats.MaxHP:F0}");

		if (GameManager.instance != null)
		{
			sb.AppendLine($"킬 {GameManager.instance.Kill}");
			sb.AppendLine($"코인 {GameManager.instance.Coin}");
		}

		return sb.ToString().TrimEnd();
	}

}

/// <summary>PlayerStats 최종값 → Status 패널용 텍스트.</summary>
public static class PlayerStatsDisplay
{
	const string MissingStatsMessage =
		"PlayerStats를 찾을 수 없습니다.\nPlayer 오브젝트에 PlayerStats가 있는지 확인하세요.";

	/// <summary>왼쪽 열: 공격 + 방어</summary>
	public static string BuildMainColumn(PlayerStats stats)
	{
		if (stats == null)
			return MissingStatsMessage;

		var sb = new StringBuilder(384);

		AppendSection(sb, "공격");
		sb.AppendLine($"공격력: {stats.AttackPower:F2}");
		sb.AppendLine($"공격 속도: {stats.AttackSpeed:F2}");
		sb.AppendLine($"투사체 수: {stats.ProjectileCount}");
		sb.AppendLine($"투사체 속도: {stats.ProjectileSpeed:F2}");
		sb.AppendLine($"투사체 사거리: {stats.ProjectileRange:F2}");
		sb.AppendLine($"근접 범위: {stats.MeleeRange:F2}");
		sb.AppendLine($"치명타 확률: {stats.CritChance:P0}");
		sb.AppendLine($"치명타 피해: {stats.CritDamage:F2}");

		AppendSection(sb, "방어");
		sb.AppendLine($"방어력: {stats.Defense:F2}");
		sb.AppendLine($"최대 HP: {stats.MaxHP:F0}");
		sb.AppendLine($"현재 HP: {stats.CurrentHP:F0}");
		sb.AppendLine($"피해 감소: {stats.DamageReduction:P0}");
		sb.AppendLine($"회피: {stats.Evasion:P0}");
		sb.AppendLine($"무적 시간: {stats.InvincibilityFrames:F2}s");
		sb.AppendLine($"회복 배율: {stats.HealingBonus:F2}");

		return sb.ToString().TrimEnd();
	}

	/// <summary>오른쪽 열: 유틸 + 속성</summary>
	public static string BuildSideColumn(PlayerStats stats)
	{
		if (stats == null)
			return "";

		var sb = new StringBuilder(256);

		AppendSection(sb, "유틸");
		sb.AppendLine($"이동 속도: {stats.MovementSpeed:F2}");
		sb.AppendLine($"자석 범위: {stats.MagnetRange:F2}");
		sb.AppendLine($"쿨다운 감소: {stats.CooldownReduction:P0}");
		sb.AppendLine($"시야 범위: {stats.VisionRange:F2}");

		AppendSection(sb, "속성");
		sb.AppendLine($"화염: {OnOff(stats.fireEnabled)} ({stats.FirePower:F2})");
		sb.AppendLine($"독: {OnOff(stats.poisonEnabled)} ({stats.PoisonPower:F2})");
		sb.AppendLine($"빙결: {OnOff(stats.freezeEnabled)} ({stats.FreezePower:F2})");
		sb.AppendLine($"물: {OnOff(stats.waterEnabled)} ({stats.WaterPower:F2})");
		sb.AppendLine($"번개: {OnOff(stats.lightningEnabled)} ({stats.LightningPower:F2})");

		return sb.ToString().TrimEnd();
	}

	/// <summary>한 줄에 전부 (레거시).</summary>
	public static string BuildBody(PlayerStats stats)
	{
		if (stats == null)
			return MissingStatsMessage;

		return BuildMainColumn(stats) + "\n\n" + BuildSideColumn(stats);
	}

	static void AppendSection(StringBuilder sb, string name)
	{
		if (sb.Length > 0)
			sb.AppendLine();
		sb.AppendLine($"── {name} ──");
	}

	static string OnOff(bool enabled) => enabled ? "ON" : "OFF";
}
