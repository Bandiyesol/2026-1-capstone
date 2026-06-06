using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메인 메뉴 화면 오른쪽 위 클리어 랭킹 패널(Canvas 기준 고정 크기).</summary>
public static class MainMenuLeaderboardBootstrap
{
	public const float PanelWidth = 340f;
	public const float PanelHeight = 440f;
	public const float MarginRight = 24f;
	public const float MarginTop = 24f;

	public static MainMenuLeaderboardView Ensure(Transform gameStartRoot)
	{
		if (gameStartRoot == null)
		{
			MainMenuLeaderboardView orphan = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
			if (orphan != null)
				ApplyTopRightLayout(orphan.transform as RectTransform, ResolveCanvasTransform(orphan.transform));
			return orphan;
		}

		Canvas canvas = gameStartRoot.GetComponentInParent<Canvas>();
		Transform canvasRoot = canvas != null ? canvas.transform : gameStartRoot;

		MainMenuLeaderboardView existing = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
		if (existing != null)
		{
			ApplyTopRightLayout(existing.transform as RectTransform, canvasRoot);
			existing.BindMenuRoot(gameStartRoot);
			return existing;
		}

		Transform legacyUnderMenu = gameStartRoot.Find("MainMenuLeaderboard");
		if (legacyUnderMenu != null)
			Object.Destroy(legacyUnderMenu.gameObject);

		MainMenuLeaderboardView view = Build(canvasRoot);
		view.BindMenuRoot(gameStartRoot);
		return view;
	}

	public static void ApplyTopRightLayout(RectTransform rect, Transform canvasRoot)
	{
		if (rect == null)
			return;

		if (canvasRoot != null && rect.parent != canvasRoot)
			rect.SetParent(canvasRoot, false);

		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 1f);
		rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
		rect.anchoredPosition = new Vector2(-MarginRight, -MarginTop);
		rect.localScale = Vector3.one;
		rect.localRotation = Quaternion.identity;
	}

	static Transform ResolveCanvasTransform(Transform from)
	{
		Canvas canvas = from != null ? from.GetComponentInParent<Canvas>() : null;
		return canvas != null ? canvas.transform : from;
	}

	static MainMenuLeaderboardView Build(Transform canvasRoot)
	{
		GameObject root = CreateUiObject("MainMenuLeaderboard", canvasRoot);
		ApplyTopRightLayout(root.GetComponent<RectTransform>(), canvasRoot);

		Image bg = root.AddComponent<Image>();
		bg.color = new Color(0.08f, 0.1f, 0.16f, 0.82f);
		bg.raycastTarget = true;

		GameObject title = CreateText("Title", root.transform, "클리어 랭킹", 22f, TextAlignmentOptions.Center);
		RectTransform titleRect = title.GetComponent<RectTransform>();
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.sizeDelta = new Vector2(0f, 32f);
		titleRect.anchoredPosition = new Vector2(0f, -6f);

		GameObject subtitle = CreateText("Subtitle", root.transform, "닉네임 · 최단 플레이타임 · 탭 상세", 14f, TextAlignmentOptions.Center);
		RectTransform subRect = subtitle.GetComponent<RectTransform>();
		subRect.anchorMin = new Vector2(0f, 1f);
		subRect.anchorMax = new Vector2(1f, 1f);
		subRect.pivot = new Vector2(0.5f, 1f);
		subRect.sizeDelta = new Vector2(0f, 22f);
		subRect.anchoredPosition = new Vector2(0f, -36f);

		GameObject rowsRoot = CreateUiObject("Rows", root.transform);
		RectTransform rowsRect = rowsRoot.GetComponent<RectTransform>();
		rowsRect.anchorMin = new Vector2(0f, 0f);
		rowsRect.anchorMax = new Vector2(1f, 1f);
		rowsRect.offsetMin = new Vector2(10f, 10f);
		rowsRect.offsetMax = new Vector2(-10f, -62f);

		VerticalLayoutGroup layout = rowsRoot.AddComponent<VerticalLayoutGroup>();
		layout.childAlignment = TextAnchor.UpperLeft;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = true;
		layout.spacing = 2f;
		layout.padding = new RectOffset(0, 0, 0, 0);

		var rowButtons = new Button[GameRunLeaderboard.MaxRankCount];
		var rowLabels = new TextMeshProUGUI[GameRunLeaderboard.MaxRankCount];

		for (int i = 0; i < GameRunLeaderboard.MaxRankCount; i++)
		{
			int rank = i + 1;
			GameObject row = CreateUiObject($"RankRow{rank}", rowsRoot.transform);
			Image rowBg = row.AddComponent<Image>();
			rowBg.color = new Color(0.18f, 0.22f, 0.3f, 0.55f);

			Button button = row.AddComponent<Button>();
			button.targetGraphic = rowBg;
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
			colors.pressedColor = new Color(0.7f, 0.78f, 0.95f, 1f);
			colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
			button.colors = colors;

			GameObject labelGo = CreateText("Label", row.transform, GameRunLeaderboard.FormatRankLine(rank, null), 17f, TextAlignmentOptions.MidlineLeft);
			RectTransform labelRect = labelGo.GetComponent<RectTransform>();
			StretchFull(labelRect);
			labelRect.offsetMin = new Vector2(8f, 0f);
			labelRect.offsetMax = new Vector2(-4f, 0f);

			rowButtons[i] = button;
			rowLabels[i] = labelGo.GetComponent<TextMeshProUGUI>();
		}

		MainMenuLeaderboardView view = root.AddComponent<MainMenuLeaderboardView>();
		view.Configure(
			title.GetComponent<TextMeshProUGUI>(),
			subtitle.GetComponent<TextMeshProUGUI>(),
			rowButtons,
			rowLabels);

		return view;
	}

	static GameObject CreateUiObject(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	static GameObject CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
	{
		GameObject go = CreateUiObject(name, parent);
		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = size;
		tmp.alignment = align;
		tmp.color = Color.white;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.overflowMode = TextOverflowModes.Ellipsis;
		TmpKoreanFontUtility.ApplyFont(tmp, TmpKoreanFontUtility.ResolveNeoDgmFont(null));
		return go;
	}

	static void StretchFull(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
}
