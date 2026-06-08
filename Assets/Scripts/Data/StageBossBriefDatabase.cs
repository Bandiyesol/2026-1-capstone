using UnityEngine;

/// <summary>
/// stageIndex(0부터)와 동일한 길이의 배열로 스테이지별 보스 브리핑을 연결합니다.
/// 비어 있거나 해당 인덱스가 null이면 <see cref="BossBriefingDefaults"/>를 사용합니다.
/// </summary>
[CreateAssetMenu(menuName = "Boss/Stage Boss Brief Database")]
public class StageBossBriefDatabase : ScriptableObject
{
	[Tooltip("인덱스 = StageManager.stageIndex (0이 첫 스테이지)")]
	public BossBriefProfile[] bossesPerStage;

	public bool TryGetBriefing(int stageIndex, out BossBriefProfile profile)
	{
		profile = null;
		if (bossesPerStage == null || stageIndex < 0 || stageIndex >= bossesPerStage.Length)
			return false;

		profile = bossesPerStage[stageIndex];
		return profile != null;
	}
}
