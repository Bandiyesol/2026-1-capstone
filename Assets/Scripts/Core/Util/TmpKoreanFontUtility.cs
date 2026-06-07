using System.Collections.Generic;
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

	/// <summary>악세사리 UI 공통 문자 (등급·상점·인벤토리 라벨).</summary>
	public const string AccessoryUiCommonGlyphs =
		"일반 희귀 유니크 전설 Common Rare Unique Legendary 성물 " +
		"공격 방어 유틸 속성 화염 독 빙결 물 번개 " +
		"악세서리 무기 물약 품절 구매 완료 클릭하여 합산 개수 이름 없음 " +
		"토끼발 촛불 미네르바 지혜 전지적 설계 복리 증가 치명타 확률";

	/// <summary>악세사리에 쓰이지만 SDF 아틀라스에 자주 빠지는 neodgm 음절.</summary>
	public const string AccessoryPriorityGlyphs = "끼납낡럭꽃냉";

	/// <summary>RewardCatalog / AccessoryData 전체 displayName·description·type 문자.</summary>
	public static string CollectAccessoryUiGlyphs()
	{
		var sb = new StringBuilder(16384);
		sb.Append(AccessoryUiCommonGlyphs);
		ForEachKnownAccessory(data => AppendAccessoryText(sb, data));
		return sb.ToString();
	}

	/// <summary>카탈로그에 등록된 모든 AccessoryData를 순회합니다.</summary>
	public static void ForEachKnownAccessory(System.Action<AccessoryData> action)
	{
		if (action == null)
			return;

		RewardCatalogSettings catalog = RewardCatalogSettings.Load();
		if (catalog?.allAccessories == null)
			return;

		foreach (AccessoryData data in catalog.allAccessories)
		{
			if (data != null)
				action(data);
		}
	}

	/// <summary>neodgm 폰트에 악세사리 UI 전체 글리프를 반영합니다.</summary>
	public static void EnsureAllAccessoryGlyphs(TMP_FontAsset font, IEnumerable<AccessoryData> extra = null)
	{
		font = ResolveNeoDgmFont(font);
		if (font == null)
			return;

		if (accessoryGlyphsBootstrapped && extra == null)
			return;

		var sb = new StringBuilder(CollectAccessoryUiGlyphs());
		sb.Append(AccessoryPriorityGlyphs);
		if (extra != null)
		{
			foreach (AccessoryData data in extra)
				AppendAccessoryText(sb, data);
		}

		EnsureGlyphsInFont(font, sb.ToString(), "AccessoryUI", suppressWarning: true);
		EnsureFallbackChainReady(font);

		if (extra == null)
			accessoryGlyphsBootstrapped = true;
	}

	public static void AppendAccessoryText(StringBuilder sb, AccessoryData data)
	{
		if (data == null)
			return;

		if (!string.IsNullOrEmpty(data.displayName))
			sb.Append(data.displayName);
		if (!string.IsNullOrEmpty(data.description))
			sb.Append(data.description);
		if (!string.IsNullOrEmpty(data.accessoryType))
			sb.Append(data.accessoryType);
	}

	static TMP_FontAsset cachedNeoDgm;
	static bool accessoryGlyphsBootstrapped;
	static readonly HashSet<char> RuntimeAddedChars = new HashSet<char>();

	public static void ResetRuntimeGlyphCache()
	{
		RuntimeAddedChars.Clear();
		accessoryGlyphsBootstrapped = false;
		cachedNeoDgm = null;
	}

#if UNITY_EDITOR
	public const string NeoDgmAssetPath = "Assets/Arts/UI/Fonts/neodgm SDF.asset";
	public const string FallbackAssetPath = "Assets/Arts/UI/Fonts/neodgm Korean Fallback SDF.asset";
#endif

	public static void EnsureGlyphs(TextMeshProUGUI tmp, TMP_FontAsset font, string text)
	{
		if (tmp == null)
			return;

		font = ResolveNeoDgmFont(font);
		if (font != null)
			tmp.font = font;

		EnsureGlyphsInFont(tmp.font, text, tmp.name, suppressWarning: true);

		if (IsFontAssetRenderable(tmp.font))
			tmp.ForceMeshUpdate();
	}

	/// <summary>neodgm + Fallback 폰트 Material·Atlas가 사용 가능한지 확인합니다.</summary>
	public static void EnsureFallbackChainReady(TMP_FontAsset primary)
	{
		primary = ResolveNeoDgmFont(primary);
		if (primary == null)
			return;

		if (!IsFontAssetRenderable(primary))
		{
			Debug.LogWarning("[TmpKoreanFont] neodgm SDF Material/Atlas가 없습니다. 에디터에서 폰트 에셋을 확인하세요.");
			return;
		}

		if (primary.fallbackFontAssetTable == null || primary.fallbackFontAssetTable.Count == 0)
		{
			Debug.LogWarning(
				"[TmpKoreanFont] neodgm 보조 Fallback이 연결되지 않았습니다.\n" +
				"Tools > Game > Repair neodgm Korean Fallback Font 를 실행하세요.");
			return;
		}

		foreach (TMP_FontAsset fallback in primary.fallbackFontAssetTable)
		{
			if (fallback == null)
				continue;

			if (!IsFontAssetRenderable(fallback))
			{
				Debug.LogWarning(
					"[TmpKoreanFont] Fallback 폰트 Material/Atlas가 없습니다.\n" +
					"Tools > Game > Repair neodgm Korean Fallback Font 를 실행하세요.");
			}
		}
	}

	static bool IsFontChainRenderable(TMP_FontAsset primary)
	{
		return IsFontAssetRenderable(primary);
	}

	static bool IsFontAssetRenderable(TMP_FontAsset font)
	{
		return font != null
		       && font.material != null
		       && font.atlasTextures != null
		       && font.atlasTextures.Length > 0
		       && font.atlasTextures[0] != null;
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

		MainStoryUI mainStory = UnityEngine.Object.FindFirstObjectByType<MainStoryUI>(FindObjectsInactive.Include);
		if (mainStory != null)
		{
			TMP_FontAsset fromMain = mainStory.KoreanFont;
			if (fromMain != null)
			{
				cachedNeoDgm = fromMain;
				return cachedNeoDgm;
			}
		}

		EndingStoryUI endingStory = UnityEngine.Object.FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);
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

	public static void EnsureGlyphsInFont(TMP_FontAsset font, string text, string logContext = null, bool suppressWarning = false)
	{
		if (font == null || string.IsNullOrEmpty(text))
			return;

		string missing = GetPrimaryMissingCharacters(font, text);
		if (!string.IsNullOrEmpty(missing))
			TryAddMissingCharactersRuntime(font, missing);

		if (suppressWarning || Application.isPlaying)
			return;

		missing = GetPrimaryMissingCharacters(font, text);
		if (!string.IsNullOrEmpty(missing))
		{
			Debug.LogWarning(
				$"[TmpKoreanFont] '{logContext ?? font.name}' SDF 아틀라스에 없는 글자: {missing}\n" +
				"에디터에서 Tools > Game > Add Accessory UI Korean Glyphs (neodgm) 실행 후 저장하세요.");
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
		return GetMissingCharacters(font, text, searchFallbacks: true);
	}

	/// <summary>neodgm SDF 아틀라스에 없는 글자 (Fallback 제외).</summary>
	public static string GetPrimaryMissingCharacters(TMP_FontAsset font, string text)
	{
		return GetMissingCharacters(font, text, searchFallbacks: false);
	}

	static string GetMissingCharacters(TMP_FontAsset font, string text, bool searchFallbacks)
	{
		if (font == null || string.IsNullOrEmpty(text))
			return "";

		var seen = new HashSet<char>();
		var sb = new StringBuilder();
		foreach (char c in text)
		{
			if (c == '\n' || c == '\r' || c == '\t')
				continue;

			if (!seen.Add(c))
				continue;

			if (!font.HasCharacter(c, searchFallbacks))
				sb.Append(c);
		}

		return sb.ToString();
	}

	static void TryAddMissingCharactersRuntime(TMP_FontAsset font, string missing)
	{
		if (font == null || string.IsNullOrEmpty(missing))
			return;

		var pending = new StringBuilder(missing.Length);
		foreach (char c in missing)
		{
			if (RuntimeAddedChars.Contains(c))
				continue;

			pending.Append(c);
		}

		if (pending.Length == 0)
			return;

		string batch = pending.ToString();
		if (!font.TryAddCharacters(batch, out string stillMissing, true))
		{
			Debug.LogWarning($"[TmpKoreanFont] 글리프 추가 실패: {batch}");
			return;
		}

		foreach (char c in batch)
		{
			if (font.HasCharacter(c, false))
				RuntimeAddedChars.Add(c);
		}

		if (!string.IsNullOrEmpty(stillMissing))
			Debug.LogWarning($"[TmpKoreanFont] neodgm SDF에 추가되지 않은 글자: {stillMissing}");

		font.ReadFontAssetDefinition();
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
