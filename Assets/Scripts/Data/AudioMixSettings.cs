using UnityEngine;

/// <summary>
/// BGM·SFX 전체 믹스 배율 — Unity Inspector / Resources/Data/AudioMixSettings 에서 조절.
/// 플레이어 설정(배경음·효과음 슬라이더) 위에 곱해집니다.
/// </summary>
[CreateAssetMenu(fileName = "AudioMixSettings", menuName = "Game/Audio Mix Settings")]
public class AudioMixSettings : ScriptableObject
{
	[Header("전체 믹스 (슬라이더 위 추가 배율)")]
	[Range(0.05f, 2f)]
	[Tooltip("배경음·스테이지 BGM·상점 BGM에 적용")]
	public float bgmMasterMixScale = 0.42f;

	[Range(0.25f, 3f)]
	[Tooltip("효과음·클리어/사망 연출음에 적용")]
	public float sfxMasterMixScale = 1.75f;

	[Header("피격 효과음 쿨다운 (초)")]
	[Range(0.05f, 2f)]
	public float playerHitSfxCooldown = 0.5f;

	[Range(0.02f, 0.5f)]
	public float enemyHitSfxCooldown = 0.05f;

	const string ResourcePath = "Data/AudioMixSettings";

	static AudioMixSettings cached;

	public static AudioMixSettings Load()
	{
		if (cached == null)
			cached = Resources.Load<AudioMixSettings>(ResourcePath);

		if (cached == null)
		{
			cached = CreateInstance<AudioMixSettings>();
			Debug.LogWarning(
				"[AudioMixSettings] Resources/Data/AudioMixSettings.asset 이 없습니다. " +
				"Tools → Game → Create Audio Mix Settings 실행, 또는 GameAudio 오브젝트에 할당하세요.");
		}

		return cached;
	}

#if UNITY_EDITOR
	public static void SetCached(AudioMixSettings settings)
	{
		cached = settings;
	}

	void OnValidate()
	{
		if (Application.isPlaying && GameAudioSettings.Instance != null)
			GameAudioSettings.Instance.ApplyVolumes();
	}
#endif
}
