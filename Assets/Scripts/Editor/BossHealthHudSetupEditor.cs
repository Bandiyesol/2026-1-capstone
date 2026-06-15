#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>플레이어 체력바 스타일을 복제해 하단 중앙 보스 체력 HUD를 배치합니다.</summary>
public static class BossHealthHudSetupEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const float BottomOffsetY = BossHealthBarLayout.HudBottomOffsetY;
	const float BarScale = BossHealthBarLayout.BarScale;
	const float BarWidth = BossHealthBarLayout.BarWidth;
	const float BarHeight = BossHealthBarLayout.BarHeight;
	const float BossNameFontSize = BossHealthBarLayout.BossNameFontSize;
	const float BossNameOffsetY = BossHealthBarLayout.BossNameOffsetY;

	static readonly string[] StripFromClonedBar =
	{
		"BossBriefTrigger",
		"BossBriefTooltip",
		"Handle Slide Area",
	};

	[MenuItem("Tools/UI/Setup Boss Health HUD")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("Boss Health HUD", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		if (!Apply(saveScene: true))
		{
			EditorUtility.DisplayDialog("Boss Health HUD", "Health 슬라이더 또는 HUD를 찾지 못했습니다.", "확인");
			return;
		}

		EditorUtility.DisplayDialog("Boss Health HUD", "보스 체력 HUD를 설정했습니다.", "확인");
	}

	public static bool Apply(bool saveScene)
	{
		Slider playerHealth = FindPlayerHealthSlider();
		Transform hudRoot = FindHudRoot(playerHealth);
		if (playerHealth == null || hudRoot == null)
			return false;

		BossHealthHudUI hudUi = Object.FindFirstObjectByType<BossHealthHudUI>(FindObjectsInactive.Include);
		GameObject hudRootGo;
		if (hudUi != null)
		{
			hudRootGo = hudUi.gameObject;
		}
		else
		{
			hudRootGo = new GameObject("BossHealthHud", typeof(RectTransform));
			Undo.RegisterCreatedObjectUndo(hudRootGo, "Create BossHealthHud");
			hudRootGo.transform.SetParent(hudRoot, false);
			hudUi = Undo.AddComponent<BossHealthHudUI>(hudRootGo);
		}

		if (hudRootGo.transform is RectTransform hudRect)
		{
			hudRect.anchorMin = new Vector2(0.5f, 0f);
			hudRect.anchorMax = new Vector2(0.5f, 0f);
			hudRect.pivot = new Vector2(0.5f, 0f);
			hudRect.anchoredPosition = new Vector2(0f, BottomOffsetY);
			hudRect.sizeDelta = new Vector2(BarWidth * BarScale, BarHeight * BarScale + 36f);
		}

		Slider bossSlider = EnsureBossHealthBar(hudRootGo.transform, playerHealth);
		TextMeshProUGUI nameLabel = EnsureBossNameLabel(hudRootGo.transform);

		SerializedObject so = new SerializedObject(hudUi);
		so.FindProperty("healthSlider").objectReferenceValue = bossSlider;
		so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
		so.FindProperty("koreanFont").objectReferenceValue = TmpKoreanFontUtility.ResolveNeoDgmFont(null);
		so.ApplyModifiedPropertiesWithoutUndo();

		hudRootGo.SetActive(true);
		if (!hudRootGo.TryGetComponent(out CanvasGroup group))
			group = hudRootGo.AddComponent<CanvasGroup>();
		group.alpha = 0f;
		group.interactable = false;
		group.blocksRaycasts = false;
		EditorUtility.SetDirty(hudRootGo);

		if (saveScene)
		{
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
		}

		return true;
	}

	static Slider FindPlayerHealthSlider()
	{
		foreach (UHD uhd in Object.FindObjectsByType<UHD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (uhd.type != UHD.InfoType.Health)
				continue;

			Slider slider = uhd.GetComponent<Slider>();
			if (slider != null)
				return slider;
		}

		return null;
	}

	static Transform FindHudRoot(Slider playerHealth)
	{
		if (playerHealth != null)
			return playerHealth.transform.parent;

		GameObject hud = GameObject.Find("HUD");
		return hud != null ? hud.transform : null;
	}

	static Slider EnsureBossHealthBar(Transform parent, Slider playerHealthTemplate)
	{
		Transform existing = parent.Find("BossHealthBar");
		GameObject barGo;
		if (existing != null)
		{
			barGo = existing.gameObject;
		}
		else
		{
			barGo = Object.Instantiate(playerHealthTemplate.gameObject, parent);
			barGo.name = "BossHealthBar";
			Undo.RegisterCreatedObjectUndo(barGo, "Create BossHealthBar");
		}

		UHD uhd = barGo.GetComponent<UHD>();
		if (uhd != null)
			Object.DestroyImmediate(uhd);

		StripClonedHudChildren(barGo.transform);

		if (barGo.transform is RectTransform barRect)
		{
			barRect.anchorMin = new Vector2(0.5f, 0f);
			barRect.anchorMax = new Vector2(0.5f, 0f);
			barRect.pivot = new Vector2(0.5f, 0f);
			barRect.anchoredPosition = Vector2.zero;
			barRect.sizeDelta = new Vector2(BarWidth, BarHeight);
			barRect.localScale = new Vector3(BarScale, BarScale, 1f);
		}

		Slider slider = barGo.GetComponent<Slider>();
		if (slider != null)
		{
			slider.interactable = false;
			slider.minValue = 0f;
			slider.maxValue = 1f;
			ConfigureFillInsets(slider, BarWidth);
		}

		foreach (Graphic graphic in barGo.GetComponentsInChildren<Graphic>(true))
			graphic.raycastTarget = false;

		return slider;
	}

	static void StripClonedHudChildren(Transform barRoot)
	{
		for (int i = barRoot.childCount - 1; i >= 0; i--)
		{
			Transform child = barRoot.GetChild(i);
			if (child == null)
				continue;

			for (int j = 0; j < StripFromClonedBar.Length; j++)
			{
				if (child.name != StripFromClonedBar[j])
					continue;

				Object.DestroyImmediate(child.gameObject);
				break;
			}
		}
	}

	static void ConfigureFillInsets(Slider slider, float barWidth) =>
		BossHealthBarLayout.ConfigureFillInsets(slider, barWidth);

	static TextMeshProUGUI EnsureBossNameLabel(Transform parent)
	{
		Transform existing = parent.Find("BossNameLabel");
		GameObject go;
		if (existing != null)
		{
			go = existing.gameObject;
		}
		else
		{
			go = new GameObject("BossNameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			go.transform.SetParent(parent, false);
			Undo.RegisterCreatedObjectUndo(go, "Create BossNameLabel");
		}

		if (go.transform is RectTransform rect)
		{
			rect.anchorMin = new Vector2(0.5f, 0f);
			rect.anchorMax = new Vector2(0.5f, 0f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.anchoredPosition = new Vector2(0f, BarHeight * BarScale + BossNameOffsetY);
			rect.sizeDelta = new Vector2(BarWidth * BarScale + 40f, 40f);
		}

		TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
		tmp.text = "보스";
		tmp.fontSize = BossNameFontSize;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.raycastTarget = false;
		TmpKoreanFontUtility.ApplyFont(tmp, TmpKoreanFontUtility.ResolveNeoDgmFont(null));
		return tmp;
	}
}
#endif
