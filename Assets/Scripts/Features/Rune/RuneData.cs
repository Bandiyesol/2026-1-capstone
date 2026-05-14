using UnityEngine;
 
[CreateAssetMenu(fileName = "Rune", menuName = "Scriptable Object/RuneData")]
public class RuneData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public string runeName;	
	public RuneCategory category;
	public RuneType runeType;
    public Sprite runeIcon;
	[TextArea] public string runeDescription;
	[TextArea] public string valueDescription;
 

	[Space(10)]
    [Header("[ 룬 세부 설정 ]")]
	[Tooltip("힘, 데미지 배율, 회전 속도 등 '강도'와 관련된 수치")] public float power;
	[Tooltip("지속 시간, 사거리, 회전 각도 등 '길이/시간'과 관련된 수치")] public float duration;
	[Tooltip("분열 개수, 도탄 횟수, 발사 수 등 '수량'과 관련된 수치")] public int count;
	[Tooltip("반지름, 폭발 범위, 감지 거리 등 '크기/범위'와 관련된 수치")] public float range;
	[Tooltip("공격 주기, 발동 간격, 지연 시간 등 '시간차'와 관련된 수치")] public float interval;
}