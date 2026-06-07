using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메인 메뉴 Canvas 우상단 클리어 랭킹 — 씬에 MainMenuLeaderboard가 있어야 합니다.</summary>
public static class MainMenuLeaderboardBootstrap
{
	public const float PanelWidth = 340f;
	public const float PanelHeight = 440f;
	public const float MarginRight = 24f;
	public const float MarginTop = 24f;

	public static MainMenuLeaderboardView Ensure(Transform gameStartRoot)
	{
		MainMenuLeaderboardView existing = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
		if (existing == null)
		{
			Debug.LogWarning(
				"[MainMenuLeaderboardBootstrap] MainMenuLeaderboard가 씬에 없습니다. " +
				"Tools → Setup ProtoType Scene UI 를 실행하세요.");
			return null;
		}

		Transform canvasRoot = ResolveCanvasTransform(gameStartRoot != null ? gameStartRoot : existing.transform);
		ApplyTopRightLayout(existing.transform as RectTransform, canvasRoot);
		if (gameStartRoot != null)
			existing.BindMenuRoot(gameStartRoot);
		return existing;
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
}
