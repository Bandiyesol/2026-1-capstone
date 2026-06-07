#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD Stage(마법진) / Kill(해골) 블록을 씬에 직접 구성합니다.
/// 보스 포털(SpriteRenderer)과 분리 — HUD Image 색·스프라이트만 변경합니다.
/// </summary>
public static class GameplayHudSetupEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const string MagicCircleAssetPath = "Assets/Arts/Effects/Magic Circle.png";
	const string MagicCircleSpriteName = "magic_circle_spritesheet_0";
	const string SkullAssetPath = "Assets/Arts/UI/skull/skull.png";
	const string SkullSpriteName = "skull_0";

	static readonly Color HudMagicCircleTint = new Color(1f, 0.78f, 0.08f, 1f);
	static readonly Color HudMagicCircleAccent = new Color(0.92f, 0.58f, 0.05f, 0.9f);
	static readonly Color HudMagicCircleHalo = new Color(1f, 0.68f, 0.08f, 1f);
	static readonly Color HudIconOutline = new Color(0.08f, 0.04f, 0f, 1f);
	static readonly Color HudStatTextColor = HudStatTextStyle.LightText;
	const float StageIconSize = 64f;
	const float StageIconHaloExpand = 4f;
	const float StageIconAccentExpand = 1f;
	const float StageKillGap = 56f;
	const float HudStatRowAnchorY = -79f;

	[MenuItem("Tools/Game/Setup HUD Stage and Kill")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("HUD 설정", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		if (!RunSetup(saveScene: true, showDialog: true))
			EditorUtility.DisplayDialog("HUD 설정", "HUD 또는 Stage 오브젝트를 찾지 못했습니다.", "확인");
	}

	public static void SetupBatch()
	{
		RunSetup(saveScene: true, showDialog: false);
	}

	static bool RunSetup(bool saveScene, bool showDialog)
	{
		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		if (!TrySetupInActiveScene())
			return false;

		EditorSceneManager.MarkSceneDirty(scene);
		if (saveScene)
			EditorSceneManager.SaveScene(scene);

		if (showDialog)
		{
			EditorUtility.DisplayDialog(
				"HUD 설정",
				"Stage(마법진) / Kill(해골) HUD를 씬에 배치했습니다.\n보스 포털 마법진 에셋은 변경하지 않습니다.",
				"확인");
		}

		return true;
	}

	public static bool TrySetupInActiveScene()
	{
		Transform hud = FindHudRoot();
		if (hud == null)
			return false;

		Transform stage = hud.Find("Stage");
		if (stage == null)
			return false;

		Sprite magicCircle = LoadSprite(MagicCircleAssetPath, MagicCircleSpriteName);
		Sprite skull = LoadSprite(SkullAssetPath, SkullSpriteName);

		ApplyStageBlock(stage, magicCircle);
		Transform kill = EnsureKillBlock(hud, stage, skull);
		PositionKillBlock(stage, kill);

		return true;
	}

	static Transform FindHudRoot()
	{
		GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
		if (gm != null && gm.gameplayHud != null)
			return gm.gameplayHud.transform;

		GameObject hud = GameObject.Find("HUD");
		return hud != null ? hud.transform : null;
	}

	static void ApplyStageBlock(Transform stage, Sprite magicCircle)
	{
		if (stage.TryGetComponent(out RectTransform stageRect))
		{
			stageRect.sizeDelta = new Vector2(StageIconSize, StageIconSize);
			stageRect.anchoredPosition = new Vector2(-700f, GetStageAnchoredY());
		}

		EnsureStageIconLayers(stage, magicCircle);

		Transform stageTextTransform = stage.Find("Stage Text");
		if (stageTextTransform != null)
		{
			ConfigureStatText(stageTextTransform, UHD.InfoType.Stage, new Vector2(120f, 40f), new Vector2(76f, 0f));
		}
	}

	static void EnsureStageIconLayers(Transform stage, Sprite magicCircle)
	{
		if (stage.TryGetComponent(out Image rootImage))
			Object.DestroyImmediate(rootImage);

		RemoveLegacyStageLayer(stage, "Stage Backdrop");
		RemoveLegacyStageLayer(stage, "Stage Icon Glow");
		RemoveLegacyStageLayer(stage, "Stage Frame");

		Transform halo = EnsureHudImageChild(stage, "Stage Icon Halo", magicCircle, HudMagicCircleHalo);
		StretchFill(halo as RectTransform, expand: StageIconHaloExpand);

		Transform accent = EnsureHudImageChild(stage, "Stage Icon Accent", magicCircle, HudMagicCircleAccent);
		StretchFill(accent as RectTransform, expand: StageIconAccentExpand);

		Transform icon = EnsureHudImageChild(stage, "Stage Icon", magicCircle, HudMagicCircleTint);
		StretchFill(icon as RectTransform);
		EnsureIconOutline(icon.gameObject);

		halo.SetSiblingIndex(0);
		accent.SetSiblingIndex(1);
		icon.SetSiblingIndex(2);

		Transform stageText = stage.Find("Stage Text");
		if (stageText != null)
			stageText.SetSiblingIndex(3);
	}

	static void EnsureIconOutline(GameObject iconObject)
	{
		if (!iconObject.TryGetComponent(out Outline outline))
			outline = iconObject.AddComponent<Outline>();

		outline.effectColor = HudIconOutline;
		outline.effectDistance = new Vector2(2.5f, -2.5f);
		outline.useGraphicAlpha = true;
	}

	static void RemoveLegacyStageLayer(Transform stage, string layerName)
	{
		Transform legacy = stage.Find(layerName);
		if (legacy != null)
			Object.DestroyImmediate(legacy.gameObject);
	}

	static Transform EnsureHudImageChild(Transform parent, string name, Sprite sprite, Color color)
	{
		Transform existing = parent.Find(name);
		GameObject go;
		if (existing != null)
		{
			go = existing.gameObject;
		}
		else
		{
			go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			Undo.RegisterCreatedObjectUndo(go, "Create " + name);
			go.transform.SetParent(parent, false);
			go.layer = parent.gameObject.layer;
		}

		Image image = go.GetComponent<Image>();
		if (sprite != null)
			image.sprite = sprite;
		image.color = color;
		image.preserveAspect = true;
		image.raycastTarget = false;
		return go.transform;
	}

	static void StretchFill(RectTransform rect, float expand = 0f)
	{
		if (rect == null)
			return;

		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = new Vector2(expand * 2f, expand * 2f);
	}

	static Transform EnsureKillBlock(Transform hud, Transform stage, Sprite skull)
	{
		Transform existing = hud.Find("Kill");
		if (existing != null)
		{
			ApplyKillBlock(existing, skull);
			return existing;
		}

		GameObject killGo = Object.Instantiate(stage.gameObject, hud);
		killGo.name = "Kill";
		Undo.RegisterCreatedObjectUndo(killGo, "Create Kill HUD");

		Transform killText = killGo.transform.Find("Stage Text");
		if (killText != null)
			killText.name = "Kill Text";

		ApplyKillBlock(killGo.transform, skull);
		return killGo.transform;
	}

	static void ApplyKillBlock(Transform kill, Sprite skull)
	{
		if (kill.TryGetComponent(out Image killImage) && skull != null)
		{
			killImage.sprite = skull;
			killImage.color = Color.white;
			killImage.preserveAspect = true;
			killImage.raycastTarget = false;
		}

		if (kill.TryGetComponent(out RectTransform killRect))
		{
			killRect.sizeDelta = new Vector2(56f, 56f);
			killRect.anchoredPosition = new Vector2(killRect.anchoredPosition.x, HudStatRowAnchorY);
		}

		Transform killText = kill.Find("Kill Text") ?? kill.Find("Stage Text");
		if (killText != null)
		{
			killText.name = "Kill Text";
			ConfigureStatText(killText, UHD.InfoType.Kill, new Vector2(80f, 40f), new Vector2(64f, 0f));
		}
	}

	static void ConfigureStatText(Transform textTransform, UHD.InfoType infoType, Vector2 size, Vector2 anchoredPosition)
	{
		if (!textTransform.TryGetComponent(out Text text))
			return;

		text.color = HudStatTextColor;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.raycastTarget = false;
		HudStatTextStyle.Apply(text, 0);

		if (!textTransform.TryGetComponent(out RectTransform rect))
			return;

		rect.sizeDelta = size;
		rect.anchoredPosition = anchoredPosition;

		UHD uhd = textTransform.GetComponent<UHD>();
		if (uhd == null)
			uhd = textTransform.gameObject.AddComponent<UHD>();
		uhd.type = infoType;
	}

	static void PositionKillBlock(Transform stage, Transform kill)
	{
		if (!stage.TryGetComponent(out RectTransform stageRect)
		    || !kill.TryGetComponent(out RectTransform killRect))
			return;

		Transform stageTextTransform = stage.Find("Stage Text");
		float textOffset = 64f;
		float textWidth = 120f;
		if (stageTextTransform != null && stageTextTransform.TryGetComponent(out RectTransform stageTextRect))
		{
			textOffset = stageTextRect.anchoredPosition.x;
			textWidth = stageTextRect.sizeDelta.x;
		}

		const float gap = StageKillGap;
		float killX = stageRect.anchoredPosition.x + textOffset + textWidth + gap;
		killRect.anchorMin = stageRect.anchorMin;
		killRect.anchorMax = stageRect.anchorMax;
		killRect.pivot = stageRect.pivot;
		killRect.anchoredPosition = new Vector2(killX, HudStatRowAnchorY);
		kill.SetSiblingIndex(stage.GetSiblingIndex() + 1);
	}

	/// <summary>Kill/Coin(56px)과 Stage(96px) 아이콘 중심을 같은 HUD 라인에 맞춥니다.</summary>
	static float GetStageAnchoredY()
	{
		const float referenceIconHeight = 56f;
		return HudStatRowAnchorY + (StageIconSize - referenceIconHeight) * 0.5f;
	}

	static Sprite LoadSprite(string assetPath, string spriteName)
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite && sprite.name == spriteName)
				return sprite;
		}

		return null;
	}
}
#endif
