using UnityEngine;

[CreateAssetMenu(fileName = "ActiveRune", menuName = "RuneData/Active")]
public class ActiveRuneData : RuneData
{
	[Header("── 액티브 설정 ──")]
	[Tooltip("지속 시간 (n초)")] public float duration;
	[Tooltip("속도 배율 또는 회전 각도 수치 (m의 속도)")] public float speedMultiplier;
	[Tooltip("영향 범위 또는 반지름 (range)")] public float affectedRange;
}
