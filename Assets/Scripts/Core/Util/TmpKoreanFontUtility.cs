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

#if UNITY_EDITOR
	public const string NeoDgmAssetPath = "Assets/Arts/UI/Fonts/neodgm SDF.asset";
#endif

	public static void ApplyFont(TMP_Text tmp, TMP_FontAsset font)
	{
		if (tmp == null || font == null)
			return;

		tmp.font = font;
	}

	public static void ApplyFontToAll(TMP_FontAsset font, params TextMeshProUGUI[] labels)
	{
		if (font == null || labels == null)
			return;

		foreach (var label in labels)
			ApplyFont(label, font);
	}

	/// <summary>런타임: 폰트 적용 + 누락 글자 경고만 (TryAddCharacters 호출 없음).</summary>
	public static void EnsureGlyphs(TextMeshProUGUI tmp, TMP_FontAsset font, string text)
	{
		if (tmp == null)
			return;

		if (font != null)
			tmp.font = font;

		if (tmp.font == null || string.IsNullOrEmpty(text))
			return;

		string missing = GetMissingCharacters(tmp.font, text);
		if (!string.IsNullOrEmpty(missing))
		{
			TryAddMissingCharactersRuntime(tmp.font, missing);
			missing = GetMissingCharacters(tmp.font, text);
		}

		if (!string.IsNullOrEmpty(missing))
		{
			Debug.LogWarning(
				$"[TmpKoreanFont] '{tmp.name}' 폰트에 없는 글자: {missing}\n" +
				"에디터에서 Tools > Game > Add Status UI Korean Glyphs (neodgm) 실행 후 저장하세요.",
				tmp);
		}

		tmp.ForceMeshUpdate();
	}

	public static void EnsureStatusPanelFonts(TMP_FontAsset font, TextMeshProUGUI stats, string dynamicText)
	{
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
