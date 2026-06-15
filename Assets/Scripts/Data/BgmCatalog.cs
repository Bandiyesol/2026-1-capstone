using UnityEngine;

/// <summary>
/// 메인·스테이지(바이옴) BGM 클립 목록.
/// Resources/Data/BgmCatalog — Tools/Game/Rebuild Bgm Catalog 로 갱신합니다.
/// </summary>
[CreateAssetMenu(fileName = "BgmCatalog", menuName = "Game/Bgm Catalog")]
public class BgmCatalog : ScriptableObject
{
	public const int StageCount = 7;

	[Header("타이틀·로그인·게임 시작 전")]
	public AudioClip mainMenuClip;
	[Range(0.1f, 3f)]
	[Tooltip("Rebuild Bgm Catalog 시 RMS 기준 자동 보정")]
	public float mainMenuVolumeScale = 1f;

	[Header("스테이지 1~7 (숲→동굴→바다→용암→설원→사막→보이드)")]
	public AudioClip[] stageClips = new AudioClip[StageCount];
	[Range(0.1f, 3f)]
	public float[] stageVolumeScales = new float[StageCount];
	[Range(0.5f, 1f)]
	[Tooltip("숲(스테이지 1) BGM만 추가 감쇠 — 원본 마스터가 다른 바이옴보다 큼")]
	public float stage1VolumeTrim = 0.62f;

	[Header("상점")]
	public AudioClip shopClip;
	[Range(0.1f, 3f)]
	[Tooltip("Rebuild Bgm Catalog 시 RMS 기준 자동 보정")]
	public float shopVolumeScale = 1f;

	[Header("짧은 연출 (1회 재생)")]
	public AudioClip stageClearClip;
	[Range(0.1f, 3f)]
	public float stageClearVolumeScale = 1f;
	public AudioClip deathClip;
	[Range(0.1f, 3f)]
	public float deathVolumeScale = 1f;

	static BgmCatalog cached;

	public static BgmCatalog Load()
	{
		if (cached == null)
			cached = Resources.Load<BgmCatalog>("Data/BgmCatalog");

		return cached;
	}

	public AudioClip GetStageClip(int stageIndex)
	{
		if (stageClips == null || stageIndex < 0 || stageIndex >= stageClips.Length)
			return null;

		return stageClips[stageIndex];
	}

	public float GetMainMenuVolumeScale()
	{
		return Mathf.Clamp(mainMenuVolumeScale, 0.1f, 3f);
	}

	public float GetStageVolumeScale(int stageIndex)
	{
		if (stageVolumeScales == null || stageIndex < 0 || stageIndex >= stageVolumeScales.Length)
			return 1f;

		float scale = stageVolumeScales[stageIndex];
		if (stageIndex == 0)
			scale *= stage1VolumeTrim;

		return Mathf.Clamp(scale, 0.1f, 3f);
	}

	public float GetShopVolumeScale()
	{
		return Mathf.Clamp(shopVolumeScale, 0.1f, 3f);
	}

	public float GetStageClearVolumeScale()
	{
		return Mathf.Clamp(stageClearVolumeScale, 0.1f, 3f);
	}

	public float GetDeathVolumeScale()
	{
		return Mathf.Clamp(deathVolumeScale, 0.1f, 3f);
	}

#if UNITY_EDITOR
	public static void SetCached(BgmCatalog catalog)
	{
		cached = catalog;
	}
#endif
}
