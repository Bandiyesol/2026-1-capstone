/// <summary>
/// StageBossBriefDatabase 미지정 시 사용하는 기본 보스 설명 (3스테이지).
/// 스크립트: HeavenEyeBoss, UndergroundDrillerBoss, StormDragonBoss.
/// </summary>
public static class BossBriefingDefaults
{
	public struct Entry
	{
		public string displayName;
		public string traits;
		public string patterns;
		public string traitsHud;
		public string patternsHud;
	}

	public static bool TryGet(int stageIndex, out Entry e)
	{
		e = default;

		switch (stageIndex)
		{
			case 0:
				e = new Entry
				{
					displayName = "천공의 눈",
					traits = "공중에 떠 있는 거대한 눈. 원거리 탄막과 순간이동으로 거리를 벌리며 압박합니다.",
					patterns = "부채꼴 탄막, 투명화 후 순간이동·은밀 탄막. 이동 예측이 어려울 때는 범위 회피·짧은 무적·빠른 재배치 룬이 유리합니다.",
					traitsHud = "원거리 탄막·순간이동. 거리 유지에 유리한 룬.",
					patternsHud = "부채꼴 탄막 / 투명 이동. 회피·이속 룬 추천."
				};
				return true;

			case 1:
				e = new Entry
				{
					displayName = "지하 굴착사",
					traits = "땅속을 파고들었다가 지상으로 솟아오르며, 광역 낙석으로 필드를 채웁니다.",
					patterns = "지하 이동 후 등장, 낙석 다발. 넓은 안전지대 확보·낙석 타이밍 회피·생존/방어 룬이 도움이 됩니다.",
					traitsHud = "땅속 이동 후 등장. 광역 낙석 주의.",
					patternsHud = "낙석 다발. 생존·방어·넓은 회피 룬."
				};
				return true;

			case 2:
				e = new Entry
				{
					displayName = "폭풍의 해룡",
					traits = "물기둥과 번개 탄막을 번갈아 쓰는 원거리형 보스. 필드 곳곳에 지속 위협을 둡니다.",
					patterns = "물기둥 소환 후 부채꼴 번개탄. 발 밑 예고 확인, 원거리 견제·체력 회복·이속 보조 룬을 고려하세요.",
					traitsHud = "물기둥·번개 탄막. 원거리 견제 보스.",
					patternsHud = "발 밑 예고 확인. 회복·이속 룬 고려."
				};
				return true;

			default:
				return false;
		}
	}
}
