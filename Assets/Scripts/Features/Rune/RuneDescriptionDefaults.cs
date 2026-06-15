/// <summary>룬 에셋에 설명이 없을 때 선택 카드에 표시할 간략 한국어 설명.</summary>
public static class RuneDescriptionDefaults
{
	public static string GetBrief(RuneType type)
	{
		return type switch
		{
			// Active — 순차 실행, n초 후 종료
			RuneType.Orbit => "생성 지점 기준 원 궤도로 공전하며 공격합니다. 지속 중 충돌해도 파괴되지 않습니다.",
			RuneType.Wave => "사인 곡선 궤도로 공격합니다. 지속 중 충돌해도 파괴되지 않습니다.",
			RuneType.Spiral => "나선형 궤도로 공격합니다. 지속 중 충돌해도 파괴되지 않습니다.",
			RuneType.Homing => "가장 가까운 적을 추적하며 공격합니다. 지속 중 충돌해도 파괴되지 않습니다.",

			// Trigger — 충돌 이벤트
			RuneType.Split => "충돌 시 여러 개로 분열합니다.",
			RuneType.Ricochet => "충돌 시 튕겨 공격합니다. 남은 횟수 동안 파괴되지 않습니다.",
			RuneType.Vampire => "충돌 데미지만큼 플레이어 스탯을 회복합니다.",
			RuneType.Freeze => "충돌 지점 주변 적을 일시 정지시킵니다.",
			RuneType.Chain => "충돌 시 주변 적에게 데미지가 전이됩니다.",
			RuneType.Explode => "충돌 시 범위 폭발로 피해를 줍니다.",

			// Final — 소멸 직전, 마지막 슬롯 전용
			RuneType.Recursion => "소멸 시 재귀 제외 동일 탄환을 한 번 생성합니다. 마지막 슬롯에만 착용할 수 있습니다.",

			// State — 병렬 실행
			RuneType.Gravity => "블랙홀 방향으로 적을 끌어당깁니다. 지속 중 충돌해도 파괴되지 않습니다.",
			RuneType.Growth => "크기와 데미지가 점점 증폭됩니다. 적중 시 현재 배수만큼 피해를 줍니다.",

			// Logic — 소멸 전까지 지속
			RuneType.Blink => "일정 간격으로 앞으로 순간 이동합니다.",
			RuneType.Boing => "화면 끝에 닿으면 반대편으로 이동합니다.",

			_ => string.Empty,
		};
	}
}
