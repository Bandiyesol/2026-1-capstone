using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SplitRune", menuName = "RuneData/Trigger/Split")]
public class SplitRuneData : RuneData
{
	[FormerlySerializedAs("splitCount")]
	[Tooltip("충돌·쿨마다 한 번에 생성할 분열체 수")]
	public int spawnsPerTrigger = 3;

	[Tooltip("분열체들이 퍼지는 총 각도(도). 비행 방향을 중심으로 대칭")]
	public float spreadDegrees = 30f;
}
