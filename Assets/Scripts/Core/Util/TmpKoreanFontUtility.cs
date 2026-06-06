using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// neodgm SDF 한글 글리프 적용·누락 문자 경고.
/// </summary>
public static class TmpKoreanFontUtility
{
	public const string StatusUiGlyphs =
		"설정 화면 모드 해상도 배경음악 효과음 메인 메뉴 끝내기 창 전체 테두리 없는 " +
		"내 정보 인벤토리 소지금 무기 악세서리 물약 모험가 킬 코인 HP 공격 방어 유틸 속성 화염 독 빙결 물 번개 전격 ON OFF " +
		"캐릭터 정보 성능 Offense Defense Special 공격 방어 특수 능력 재생 습득 이동 속도 범위 기록 " +
		"공격력 속도 투사체 사거리 근접 범위 치명타 확률 피해 방어력 최대 현재 감소 회피 무적 시간 회복 배율 이동 자석 쿨다운 시야 " +
		"물리 마법 배율 개수 관통 처형 임계치 발동률 강화 없음 ATK ASPD PROJ AREA CRIT DEF EVA HEAL BUFF RUNE EXEC ELEM SPD " +
		"찾을 수 없습니다 PlayerStats 오브젝트에 PlayerStats가 있는지 확인하세요";

	/// <summary>메인·엔딩 스토리 본문에 쓰인 글자 (에디터 글리프 추가용).</summary>
	public static string StoryUiGlyphs =>
		MainStoryDefaults.OpeningStory +
		EndingStoryDefaults.Title +
		EndingStoryDefaults.RuneReturnLine +
		EndingStoryDefaults.StoryBody;

	static TMP_FontAsset cachedNeoDgm;

#if UNITY_EDITOR
	public const string NeoDgmAssetPath = "Assets/Arts/UI/Fonts/neodgm SDF.asset";
#endif

	public static void EnsureGlyphs(TextMeshProUGUI tmp, TMP_FontAsset font, string text)
	{
		if (tmp == null)
			return;

		font = ResolveNeoDgmFont(font);
		if (font != null)
			tmp.font = font;

		EnsureGlyphsInFont(tmp.font, text, tmp.name);
		tmp.ForceMeshUpdate();
	}

	/// <summary>Inspector 미연결 시 neodgm SDF를 찾습니다.</summary>
	public static TMP_FontAsset ResolveNeoDgmFont(TMP_FontAsset preferred)
	{
		if (preferred != null)
			return preferred;

		if (cachedNeoDgm != null)
			return cachedNeoDgm;

#if UNITY_EDITOR
		cachedNeoDgm = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NeoDgmAssetPath);
		if (cachedNeoDgm != null)
			return cachedNeoDgm;
#endif

		MainStoryUI mainStory = Object.FindFirstObjectByType<MainStoryUI>(FindObjectsInactive.Include);
		if (mainStory != null)
		{
			TMP_FontAsset fromMain = mainStory.KoreanFont;
			if (fromMain != null)
			{
				cachedNeoDgm = fromMain;
				return cachedNeoDgm;
			}
		}

		EndingStoryUI endingStory = Object.FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);
		if (endingStory != null)
		{
			TMP_FontAsset fromEnding = endingStory.KoreanFont;
			if (fromEnding != null)
			{
				cachedNeoDgm = fromEnding;
				return cachedNeoDgm;
			}
		}

		return null;
	}

	public static void EnsureGlyphsInFont(TMP_FontAsset font, string text, string logContext = null)
	{
		if (font == null || string.IsNullOrEmpty(text))
			return;

		string missing = GetMissingCharacters(font, text);
		if (!string.IsNullOrEmpty(missing))
			TryAddMissingCharactersRuntime(font, missing);

		missing = GetMissingCharacters(font, text);
		if (!string.IsNullOrEmpty(missing))
		{
			Debug.LogWarning(
				$"[TmpKoreanFont] '{logContext ?? font.name}' 폰트에 없는 글자: {missing}\n" +
				"에디터에서 Tools > Game > Add Story UI Korean Glyphs (neodgm) 실행 후 저장하세요.");
		}
	}

	public static void ApplyFont(TMP_Text tmp, TMP_FontAsset font)
	{
		if (tmp == null)
			return;

		font = ResolveNeoDgmFont(font);
		if (font == null)
			return;

		tmp.font = font;
	}
	public static void ApplyFontToAll(TMP_FontAsset font, params TextMeshProUGUI[] labels)
	{
		font = ResolveNeoDgmFont(font);
		if (font == null || labels == null)
			return;

		foreach (var label in labels)
			ApplyFont(label, font);
	}

	public static void EnsureStatusPanelFonts(TMP_FontAsset font, TextMeshProUGUI stats, string dynamicText)
	{
		font = ResolveNeoDgmFont(font);
		ApplyFont(stats, font);
		EnsureGlyphs(stats, font, StatusUiGlyphs + (dynamicText ?? ""));
	}

	public static string GetMissingCharacters(TMP_FontAsset font, string text)
	{
		if (font == null || string.IsNullOrEmpty(text))
			return "";

		var sb = new StringBuilder();
		foreach (char c in text)
		{
			if (c == '\n' || c == '\r' || c == '\t')
				continue;

			if (!font.HasCharacter(c, true))
				sb.Append(c);
		}

		return sb.ToString();
	}

	static void TryAddMissingCharactersRuntime(TMP_FontAsset font, string missing)
	{
		if (font == null || string.IsNullOrEmpty(missing))
			return;

		font.TryAddCharacters(missing, out string stillMissing, true);
	}

#if UNITY_EDITOR
	/// <summary>에디터 전용 — Dynamic 폰트에서 TMP string 오버로드로 글리프 추가.</summary>
	public static bool TryAddStringCharacters(TMP_FontAsset font, string characters)
	{
		if (font == null || string.IsNullOrEmpty(characters))
			return true;

		return font.TryAddCharacters(characters, out string missing, true) && string.IsNullOrEmpty(missing);
	}
#endif
}
