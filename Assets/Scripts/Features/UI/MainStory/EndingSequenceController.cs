using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 보스 처치 후 — 백색 확산, 룬 회귀 대사, 엔딩 스토리 순서로 재생합니다.
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
	[Header("# 연결 (비우면 자동 탐색)")]
	[SerializeField] EndingStoryUI endingStoryUI;
	[SerializeField] Canvas targetCanvas;

	[Header("# 연출")]
	[SerializeField] float flashExpandDuration = 2.2f;
	[SerializeField] float flashStartSize = 48f;
	[SerializeField] float flashMaxAlpha = 0.92f;
	[SerializeField] float runeLineHoldSeconds = 2.8f;
	[SerializeField] string runeReturnLineOverride;

	[Header("# 폰트 (선택)")]
	[SerializeField] TMP_FontAsset koreanFont;

	bool isPlaying;

	public bool IsPlaying => isPlaying;

	public void PlayFromBoss(Vector3 bossWorldPosition)
	{
		if (isPlaying)
			return;

		EnsureReferences();
		StartCoroutine(PlayRoutine(bossWorldPosition));
	}

	void EnsureReferences()
	{
		if (endingStoryUI == null)
			endingStoryUI = FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);

		if (endingStoryUI == null)
			endingStoryUI = EndingStoryUIBootstrap.EnsureEndingStoryUI();

		if (targetCanvas == null)
		{
			if (endingStoryUI != null)
				targetCanvas = endingStoryUI.GetComponentInParent<Canvas>(true);

			if (targetCanvas == null)
				targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
		}

		ResolveKoreanFont();
		TmpKoreanFontUtility.EnsureGlyphsInFont(
			koreanFont,
			EndingStoryDefaults.RuneReturnLine,
			"EndingRuneLine");
	}

	void ResolveKoreanFont()
	{
		if (koreanFont != null)
			return;

		if (endingStoryUI != null)
			koreanFont = endingStoryUI.KoreanFont;

		if (koreanFont == null)
		{
			MainStoryUI mainStory = FindFirstObjectByType<MainStoryUI>(FindObjectsInactive.Include);
			if (mainStory != null)
				koreanFont = mainStory.KoreanFont;
		}

		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
	}

	IEnumerator PlayRoutine(Vector3 bossWorldPosition)
	{
		isPlaying = true;

		if (GameManager.instance != null)
		{
			GameManager.instance.isLive = false;
			GameManager.instance.Stop();
			GameManager.instance.HideGameplayHud();
			GameManager.CloseOverlayPanels();
		}

		yield return PlayWhiteFlashAndLine(bossWorldPosition);

		if (endingStoryUI != null)
			endingStoryUI.Show();
		else
			Debug.LogWarning("[EndingSequenceController] EndingStoryUI가 없어 엔딩 스토리를 건너뜁니다.");

		isPlaying = false;
	}

	IEnumerator PlayWhiteFlashAndLine(Vector3 bossWorldPosition)
	{
		if (targetCanvas == null)
			yield break;

		RectTransform canvasRect = targetCanvas.transform as RectTransform;
		if (canvasRect == null)
			yield break;

		Vector2 localPoint = WorldToCanvasLocal(canvasRect, targetCanvas, bossWorldPosition);

		GameObject flashRoot = new GameObject("EndingWhiteFlash", typeof(RectTransform));
		flashRoot.transform.SetParent(canvasRect, false);
		flashRoot.transform.SetAsLastSibling();

		RectTransform flashRect = flashRoot.GetComponent<RectTransform>();
		flashRect.anchorMin = new Vector2(0.5f, 0.5f);
		flashRect.anchorMax = new Vector2(0.5f, 0.5f);
		flashRect.pivot = new Vector2(0.5f, 0.5f);
		flashRect.anchoredPosition = localPoint;
		flashRect.sizeDelta = new Vector2(flashStartSize, flashStartSize);

		Image flashImage = flashRoot.AddComponent<Image>();
		ApplyDefaultUiSprite(flashImage);
		flashImage.color = new Color(1f, 1f, 1f, 0f);
		flashImage.raycastTarget = false;

		float coverScale = ComputeCoverScale(canvasRect, localPoint, flashStartSize);

		float elapsed = 0f;
		while (elapsed < flashExpandDuration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / flashExpandDuration);
			float eased = 1f - Mathf.Pow(1f - t, 2.4f);

			flashRect.localScale = Vector3.one * Mathf.Lerp(0.05f, coverScale, eased);
			flashImage.color = new Color(1f, 1f, 1f, flashMaxAlpha * eased);
			yield return null;
		}

		string line = string.IsNullOrWhiteSpace(runeReturnLineOverride)
			? EndingStoryDefaults.RuneReturnLine
			: runeReturnLineOverride;

		GameObject lineRoot = CreateRuneLineOverlay(canvasRect, line);
		yield return new WaitForSecondsRealtime(runeLineHoldSeconds);

		if (lineRoot != null)
			Destroy(lineRoot);

		float fadeElapsed = 0f;
		const float fadeDuration = 0.45f;
		Color startColor = flashImage.color;
		while (fadeElapsed < fadeDuration)
		{
			fadeElapsed += Time.unscaledDeltaTime;
			float t = fadeElapsed / fadeDuration;
			flashImage.color = new Color(1f, 1f, 1f, startColor.a * (1f - t));
			yield return null;
		}

		Destroy(flashRoot);
	}

	GameObject CreateRuneLineOverlay(RectTransform canvasRect, string line)
	{
		GameObject root = new GameObject("EndingRuneLine", typeof(RectTransform));
		root.transform.SetParent(canvasRect, false);
		root.transform.SetAsLastSibling();

		RectTransform rect = root.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		GameObject textGo = new GameObject("Text", typeof(RectTransform));
		textGo.transform.SetParent(root.transform, false);

		RectTransform textRect = textGo.GetComponent<RectTransform>();
		textRect.anchorMin = new Vector2(0.5f, 0.5f);
		textRect.anchorMax = new Vector2(0.5f, 0.5f);
		textRect.pivot = new Vector2(0.5f, 0.5f);
		textRect.sizeDelta = new Vector2(900f, 120f);

		TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.fontSize = 42f;
		tmp.color = new Color(0.12f, 0.1f, 0.18f, 1f);
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		TmpKoreanFontUtility.ApplyFont(tmp, koreanFont);
		TmpKoreanFontUtility.EnsureGlyphs(tmp, koreanFont, line);
		tmp.text = line;
		tmp.ForceMeshUpdate();

		return root;
	}

	static Vector2 WorldToCanvasLocal(RectTransform canvasRect, Canvas canvas, Vector3 worldPosition)
	{
		Camera worldCam = canvas != null ? canvas.worldCamera : null;
		if (worldCam == null)
			worldCam = Camera.main;

		Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCam, worldPosition);
		Camera eventCam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
			? null
			: worldCam;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, eventCam, out Vector2 local);
		return local;
	}

	static float ComputeCoverScale(RectTransform canvasRect, Vector2 originLocal, float startSize)
	{
		Vector2 half = canvasRect.rect.size * 0.5f;
		Vector2 abs = new Vector2(Mathf.Abs(originLocal.x), Mathf.Abs(originLocal.y));
		float needW = (half.x + abs.x) * 2f;
		float needH = (half.y + abs.y) * 2f;
		float need = Mathf.Max(needW, needH);
		return Mathf.Max(need / startSize, 8f);
	}

	static Sprite cachedWhiteSprite;

	static void ApplyDefaultUiSprite(Image image)
	{
		if (image == null)
			return;

		if (cachedWhiteSprite == null)
		{
			Texture2D tex = Texture2D.whiteTexture;
			cachedWhiteSprite = Sprite.Create(
				tex,
				new Rect(0f, 0f, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				100f);
		}

		image.sprite = cachedWhiteSprite;
		image.type = Image.Type.Simple;
	}
}
