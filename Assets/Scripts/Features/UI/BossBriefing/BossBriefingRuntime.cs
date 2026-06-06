using UnityEngine;

/// <summary>현재 스테이지 보스 브리핑 — 보스 알리미 패널과 HUD 툴팁이 동일 데이터를 읽습니다.</summary>
public static class BossBriefingRuntime
{
	public static string DisplayName { get; private set; }
	public static string TraitsSummary { get; private set; }
	public static string PatternsHint { get; private set; }
	public static string TraitsHudShort { get; private set; }
	public static string PatternsHudShort { get; private set; }
	public static Sprite Portrait { get; private set; }
	public static int StageIndex { get; private set; }
	public static bool HasBrief { get; private set; }

	static GameObject[] portraitPrefabOverride;

	public static void Clear()
	{
		HasBrief = false;
		StageIndex = 0;
		portraitPrefabOverride = null;
		DisplayName = string.Empty;
		TraitsSummary = string.Empty;
		PatternsHint = string.Empty;
		TraitsHudShort = string.Empty;
		PatternsHudShort = string.Empty;
		Portrait = null;
	}

	/// <summary>스테이지 인덱스(0-based)에 해당하는 보스 표시 이름. 기록 저장 시 런타임 상태와 무관하게 조회합니다.</summary>
	public static string GetBossDisplayName(int stageIndex)
	{
		GameManager game = GameManager.instance;
		if (game != null
		    && game.bossBriefDatabase != null
		    && game.bossBriefDatabase.TryGetBriefing(stageIndex, out BossBriefProfile profile)
		    && profile != null
		    && !string.IsNullOrEmpty(profile.displayName))
		{
			return profile.displayName;
		}

		if (BossBriefingDefaults.TryGet(stageIndex, out BossBriefingDefaults.Entry entry))
			return entry.displayName;

		return "—";
	}

	public static void ApplyStage(int stageIndex, StageBossBriefDatabase database, GameObject[] portraitPrefabFallback = null)
	{
		Clear();
		StageIndex = stageIndex;
		portraitPrefabOverride = portraitPrefabFallback;

		if (database != null && database.TryGetBriefing(stageIndex, out BossBriefProfile profile) && profile != null)
		{
			ApplyFromProfile(profile);
			RefreshPortrait();
			return;
		}

		if (BossBriefingDefaults.TryGet(stageIndex, out BossBriefingDefaults.Entry e))
		{
			DisplayName = e.displayName;
			TraitsSummary = e.traits;
			PatternsHint = e.patterns;
			TraitsHudShort = string.IsNullOrWhiteSpace(e.traitsHud) ? e.traits : e.traitsHud;
			PatternsHudShort = string.IsNullOrWhiteSpace(e.patternsHud) ? e.patterns : e.patternsHud;
			HasBrief = true;
			RefreshPortrait();
		}
	}

	/// <summary>UI 표시 직전 호출 — 프리팹에서 다시 찾아 누락 방지.</summary>
	public static Sprite GetPortrait()
	{
		if (Portrait == null && HasBrief)
			RefreshPortrait();

		return Portrait;
	}

	static void RefreshPortrait()
	{
		if (!HasBrief)
		{
			Portrait = null;
			return;
		}

		if (Portrait != null)
			return;

		Portrait = BossBriefPortraitResolver.Resolve(StageIndex, portraitPrefabOverride);

		if (Portrait == null)
		{
			Debug.LogWarning(
				$"[BossBriefing] 스테이지 {StageIndex} 보스 초상을 찾지 못했습니다. " +
				"GameManager → Boss Portrait Prefabs 또는 PoolManager → Boss Prefabs 에 보스 프리팹을 연결하세요.");
		}
	}

	static void ApplyFromProfile(BossBriefProfile profile)
	{
		DisplayName = profile.displayName;
		TraitsSummary = profile.traitsSummary;
		PatternsHint = profile.patternsHint;
		TraitsHudShort = string.IsNullOrWhiteSpace(profile.traitsHudShort)
			? profile.traitsSummary
			: profile.traitsHudShort;
		PatternsHudShort = string.IsNullOrWhiteSpace(profile.patternsHudShort)
			? profile.patternsHint
			: profile.patternsHudShort;
		Portrait = profile.portrait;
		HasBrief = true;
	}
}
