using UnityEngine;

/// <summary>보스 알리미·HUD 툴팁에 쓰는 짧은 설명 (스테이지별로 ScriptableObject 할당).</summary>
[CreateAssetMenu(menuName = "Boss/Boss Brief Profile")]
public class BossBriefProfile : ScriptableObject
{
	[Header("표시")]
	public string displayName = "보스";

	[TextArea(2, 6)]
	public string traitsSummary = "특징을 한두 문장으로 적습니다.";

	[TextArea(3, 10)]
	public string patternsHint = "주요 패턴과 대처 힌트를 적습니다.";

	[Header("HUD 툴팁 (비우면 아래 간략 기본값·전면 문구 요약)")]
	[TextArea(1, 3)]
	public string traitsHudShort;
	[TextArea(1, 3)]
	public string patternsHudShort;

	[Header("선택")]
	public Sprite portrait;
}
