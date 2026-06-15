using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>하단 중앙 보스 체력바 + 보스 이름 HUD.</summary>
public class BossHealthHudUI : MonoBehaviour
{
	[SerializeField] Slider healthSlider;
	[SerializeField] TextMeshProUGUI nameLabel;
	[SerializeField] TMP_FontAsset koreanFont;

	CanvasGroup canvasGroup;
	StageManager stageManager;
	int appliedStageIndex = -1;
	bool visible;

	void Awake()
	{
		stageManager = FindFirstObjectByType<StageManager>();
		canvasGroup = GetComponent<CanvasGroup>();
		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		EnsureSlider();
		ApplyNameStyle(force: true);
		SetVisible(false);
	}

	void LateUpdate()
	{
		if (!ShouldShowHud())
		{
			SetVisible(false);
			return;
		}

		BossHealthHudResolver.Snapshot snap = BossHealthHudResolver.Resolve();
		if (!snap.IsValid)
		{
			SetVisible(false);
			return;
		}

		SetVisible(true);
		ApplyNameStyle(force: false);

		if (nameLabel != null)
			nameLabel.text = snap.DisplayName;

		if (healthSlider != null)
		{
			float ratio = snap.Max > 0f ? Mathf.Clamp01(snap.Current / snap.Max) : 0f;
			healthSlider.value = ratio;
		}
	}

	bool ShouldShowHud()
	{
		GameManager game = GameManager.instance;
		if (game == null || !game.isLive)
			return false;

		if (game.gameplayHud != null && !game.gameplayHud.activeInHierarchy)
			return false;

		return true;
	}

	void EnsureSlider()
	{
		if (healthSlider == null)
			healthSlider = GetComponentInChildren<Slider>(true);

		if (healthSlider == null)
			return;

		healthSlider.minValue = 0f;
		healthSlider.maxValue = 1f;
		healthSlider.wholeNumbers = false;
		healthSlider.interactable = false;
		BossHealthBarLayout.ConfigureFillInsets(healthSlider);
	}

	void ApplyNameStyle(bool force)
	{
		if (nameLabel == null)
			return;

		TmpKoreanFontUtility.ApplyFont(nameLabel, koreanFont);

		int stageIndex = stageManager != null ? stageManager.stageIndex : 0;
		if (!force && stageIndex == appliedStageIndex)
			return;

		appliedStageIndex = stageIndex;
		bool lightBackground = HudStatTextStyle.IsLightBackgroundStage(stageIndex);
		nameLabel.color = lightBackground ? HudStatTextStyle.DarkText : HudStatTextStyle.LightText;
		nameLabel.fontSize = BossHealthBarLayout.BossNameFontSize;
		nameLabel.alignment = TextAlignmentOptions.Center;
		nameLabel.raycastTarget = false;
	}

	void SetVisible(bool show)
	{
		if (visible == show)
			return;

		visible = show;
		if (canvasGroup != null)
		{
			canvasGroup.alpha = show ? 1f : 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}
	}
}
