#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ProtoType_LTG 씬에 런타임 UI/매니저를 한 번에 배치합니다.
/// YAML 수동 편집 대신 Unity Editor API로 컴포넌트를 붙입니다.
/// </summary>
public static class ProtoTypeSceneSetup
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/Setup ProtoType Scene UI")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("씬 UI 설정", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		EnsureManagerComponents();
		EnsurePlayerComponents();
		EnsureGameManagerComponents();
		WireInventoryUI();
		RewardSelectUISceneSetup.SetupInScene(saveScene: false, showDialog: false);
		EnsureMainMenuLeaderboard();
		ChoiceSelectUILayoutSetup.ApplyAll(saveScene: false, showDialog: false);
		OverlayPanelUILayoutSetup.ApplyAll(saveScene: false, showDialog: false);
		GameplayHudSetupEditor.TrySetupInActiveScene();
		BossStageConfigurationEditor.ApplyConfiguration();
		ShopkeeperSetupEditor.TrySetupInActiveScene();

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		EditorUtility.DisplayDialog("씬 UI 설정", "ProtoType_LTG 씬 UI/매니저 설정을 완료했습니다.", "확인");
	}

	static void EnsureManagerComponents()
	{
		GameObject manager = ShopUIBootstrap.FindSceneObject("[ Manager ]");
		if (manager == null)
			return;

		EnsureComponent<AccessoryManager>(manager);
		EnsureComponent<AccessoryEffect>(manager);
		EnsureComponent<RewardRollService>(manager);

		PoolManager pool = manager.GetComponentInChildren<PoolManager>(true);
		if (pool == null)
			pool = Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);
		if (pool != null)
			PoolManagerMotionSetupEditor.TryLoadMotionPrefabs(pool);
	}

	static void EnsurePlayerComponents()
	{
		WeaponInventory weapon = Object.FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);
		if (weapon == null)
			return;

		GameObject player = weapon.gameObject;
		EnsurePlayerBodyCollider(player);
		EnsureComponent<AccessoryInventory>(player);
		EnsureComponent<PotionInventory>(player);
		EnsureComponent<PotionEffect>(player);
	}

	static void EnsureGameManagerComponents()
	{
		GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
		if (gm == null)
			return;

		EnsureComponent<OverlayPanelEscapeInput>(gm.gameObject);
		EnsureComponent<EndingSequenceController>(gm.gameObject);
	}

	static void WireInventoryUI()
	{
		InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
		AccessoryInventory accessory = Object.FindFirstObjectByType<AccessoryInventory>(FindObjectsInactive.Include);
		PotionInventory potion = Object.FindFirstObjectByType<PotionInventory>(FindObjectsInactive.Include);
		if (inventoryUI == null)
			return;

		SerializedObject so = new SerializedObject(inventoryUI);
		if (accessory != null)
			so.FindProperty("accessoryInventory").objectReferenceValue = accessory;
		if (potion != null)
			so.FindProperty("potionInventory").objectReferenceValue = potion;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void EnsureMainMenuLeaderboard()
	{
		MainMenuLeaderboardView view = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
		if (view == null)
			view = BuildMainMenuLeaderboard();

		if (view == null)
			return;

		GameStartMenuController menu = Object.FindFirstObjectByType<GameStartMenuController>(FindObjectsInactive.Include);
		MainMenuLeaderboardBootstrap.Ensure(menu != null ? menu.transform : null);
	}

	static MainMenuLeaderboardView BuildMainMenuLeaderboard()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
		if (canvas == null)
		{
			Debug.LogWarning("[ProtoTypeSceneSetup] Canvas를 찾을 수 없어 MainMenuLeaderboard를 만들지 못했습니다.");
			return null;
		}

		TMP_FontAsset font = TmpKoreanFontUtility.ResolveNeoDgmFont(null);

		GameObject root = new GameObject("MainMenuLeaderboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		Undo.RegisterCreatedObjectUndo(root, "Create MainMenuLeaderboard");
		root.layer = LayerMask.NameToLayer("UI");
		RectTransform rootRect = root.GetComponent<RectTransform>();
		rootRect.SetParent(canvas.transform, false);
		Image rootImage = root.GetComponent<Image>();
		rootImage.color = new Color(0.08f, 0.1f, 0.16f, 0.82f);

		TextMeshProUGUI title = CreateTmpText(root.transform, "Title", "클리어 랭킹", 22, new Vector2(0f, -6f), new Vector2(0f, 32f));
		TextMeshProUGUI subtitle = CreateTmpText(root.transform, "Subtitle", "닉네임 · 최단 플레이타임 · 탭 상세", 14, new Vector2(0f, -36f), new Vector2(0f, 22f));

		GameObject rowsGo = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
		Undo.RegisterCreatedObjectUndo(rowsGo, "Create Leaderboard Rows");
		rowsGo.layer = root.layer;
		RectTransform rowsRect = rowsGo.GetComponent<RectTransform>();
		rowsRect.SetParent(rootRect, false);
		StretchRect(rowsRect, 10f, 10f, -10f, -62f);
		VerticalLayoutGroup vlg = rowsGo.GetComponent<VerticalLayoutGroup>();
		vlg.spacing = 2f;
		vlg.childControlWidth = true;
		vlg.childControlHeight = true;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = true;

		var buttons = new Button[GameRunLeaderboard.MaxRankCount];
		var labels = new TextMeshProUGUI[GameRunLeaderboard.MaxRankCount];
		for (int i = 0; i < GameRunLeaderboard.MaxRankCount; i++)
		{
			int rank = i + 1;
			GameObject row = new GameObject($"RankRow{rank}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
			Undo.RegisterCreatedObjectUndo(row, "Create Rank Row");
			row.layer = root.layer;
			RectTransform rowRect = row.GetComponent<RectTransform>();
			rowRect.SetParent(rowsRect, false);
			rowRect.sizeDelta = new Vector2(0f, 32f);
			row.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.3f, 0.55f);

			GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			Undo.RegisterCreatedObjectUndo(labelGo, "Create Rank Label");
			labelGo.layer = root.layer;
			RectTransform labelRect = labelGo.GetComponent<RectTransform>();
			labelRect.SetParent(rowRect, false);
			StretchRect(labelRect, 8f, 0f, -4f, 0f);
			TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
			label.text = $"{rank}. —";
			label.fontSize = 17;
			label.alignment = TextAlignmentOptions.MidlineLeft;
			label.raycastTarget = false;
			TmpKoreanFontUtility.ApplyFont(label, font);

			buttons[i] = row.GetComponent<Button>();
			labels[i] = label;
		}

		MainMenuLeaderboardView view = Undo.AddComponent<MainMenuLeaderboardView>(root);
		view.Configure(title, subtitle, buttons, labels);

		SerializedObject viewSo = new SerializedObject(view);
		viewSo.FindProperty("koreanFont").objectReferenceValue = font;
		viewSo.ApplyModifiedPropertiesWithoutUndo();

		MainMenuLeaderboardBootstrap.ApplyTopRightLayout(rootRect, canvas.transform);
		return view;
	}

	static TextMeshProUGUI CreateTmpText(Transform parent, string name, string text, float size, Vector2 anchoredPos, Vector2 sizeDelta)
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		Undo.RegisterCreatedObjectUndo(go, "Create TMP");
		go.layer = parent.gameObject.layer;
		RectTransform rect = go.GetComponent<RectTransform>();
		rect.SetParent(parent, false);
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = anchoredPos;
		rect.sizeDelta = sizeDelta;

		TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = size;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.raycastTarget = false;
		TmpKoreanFontUtility.ApplyFont(tmp, TmpKoreanFontUtility.ResolveNeoDgmFont(null));
		return tmp;
	}

	static void StretchRect(RectTransform rect, float left, float bottom, float right, float top)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(left, bottom);
		rect.offsetMax = new Vector2(right, top);
	}

	static void EnsurePlayerBodyCollider(GameObject player)
	{
		if (!player.TryGetComponent(out CapsuleCollider2D capsule))
			return;

		if (capsule.isTrigger)
		{
			capsule.isTrigger = false;
			EditorUtility.SetDirty(player);
		}
	}

	static T EnsureComponent<T>(GameObject target) where T : Component
	{
		T existing = target.GetComponent<T>();
		if (existing != null)
			return existing;

		return Undo.AddComponent<T>(target);
	}
}
#endif
