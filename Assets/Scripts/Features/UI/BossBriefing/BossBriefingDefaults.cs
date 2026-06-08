/// <summary>
/// StageBossBriefDatabase 미지정 시 사용하는 7스테이지 보스 브리핑.
/// 스탯 수치는 BossData에서 런타임에 채웁니다.
/// </summary>
public static class BossBriefingDefaults
{
	public struct Entry
	{
		public string displayName;
		public string biome;
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
				e = Build(
					"천공의 눈", "숲",
					"투명화하며 플레이어 주변으로 순간이동해 거리를 벌리는 공중형 보스.",
					"· 투명화: 투명 상태에서 플레이어 주변으로 여러 번 텔레포트\n· 탄막: 이동·대기 후 플레이어 방향으로 연속 탄막\n유리한 룬: 유도",
					string.Empty,
					"투명·텔레포트 / 연속 탄막 · 유도 룬");
				return true;

			case 1:
				e = Build(
					"지하 굴착사", "동굴",
					"땅속을 파고다니며 등장할 때만 공격 가능한 두더지형 보스.",
					"· 두더지: 상시 지하 이동, 지하 중 플레이어 공격 무효\n· 등장: 지상으로 올라오며 암석 다발 소환\n유리한 룬: 동결",
					string.Empty,
					"지하 무적 / 등장·낙석 · 동결 룬");
				return true;

			case 2:
				e = Build(
					"폭풍의 해룡", "바다",
					"물기둥과 부채꼴 탄막으로 필드를 압박하는 원거리 보스.",
					"· 용솟음: 일정 시간 물기둥 다수 소환\n· 브레스: 플레이어 방향 부채꼴 탄막\n유리한 룬: 유도 → 그로우",
					string.Empty,
					"물기둥 / 부채꼴 브레스 · 유도→그로우");
				return true;

			case 3:
				e = Build(
					"용암 지룡", "용암",
					"발 아래 용암 장판과 부하 소환, 나선 탄막을 쓰는 보스.",
					"· 장판: 보스 아래 용암 장판, 체력↓일수록 범위 확대\n· 부하: 이동 시 확률적 적 소환(체력↓ 시 2배 증가)\n· 나선 탄막: 정지 후 원형 회전 탄막\n유리한 룬: 딜레이 → 유도 → 그로우",
					string.Empty,
					"용암 장판 / 부하·나선탄 · 딜레이→유도→그로우");
				return true;

			case 4:
				e = Build(
					"빙하 거인", "설원",
					"근접 내려찍기와 이동 중 얼음 파편, 저체력 광폭화 보스.",
					"· 내려찍기: 근접 시 경고 후 광역 내려찍기\n· 얼음 파편: 이동 시 확률적 원형 파편(공격·폭발 룬으로 파괴 가능)\n· 광폭화: HP 50% 이하 이속 2배\n유리한 룬: 딜레이 → 그로우 → 폭발",
					string.Empty,
					"내려찍기 / 얼음 파편 · 딜레이→그로우→폭발");
				return true;

			case 5:
				e = Build(
					"사막의 수호자", "사막",
					"고속 이동과 기습 텔레포트, 모래바람으로 압박하는 보스.",
					"· 모래바람: 주기적으로 8방향 모래바람 소환\n· 기습: 플레이어 시선 시 뒤로 텔레포트, 확률적 부채꼴 탄막\n유리한 룬: 유도 → 점멸 → 그로우",
					string.Empty,
					"모래바람 / 기습 텔레포트 · 유도→점멸→그로우");
				return true;

			case 6:
				e = Build(
					"심연의 포식자", "공허",
					"상시 무적·흡혈, 잠깐 풀릴 때 탄막·텔레포트를 쓰는 최종 보스.",
					"· 무적: 무적 중 피격 시 피해 1.5배 회복\n· 유도탄: 무적 해제 시 추적 탄막 연속\n· 텔레포트: 주변 텔레포트 후 부채꼴 탄막\n· 광폭화: HP 30% 이하 스탯 2배·쿨 1/2\n유리한 룬: 딜레이 → 그로우",
					string.Empty,
					"무적·흡혈 / 유도·텔레포트 · 딜레이→그로우");
				return true;

			default:
				return false;
		}
	}

	static Entry Build(
		string name, string biome, string traits, string patterns, string traitsHud, string patternsHud)
	{
		return new Entry
		{
			displayName = name,
			biome = biome,
			traits = traits,
			patterns = patterns,
			traitsHud = traitsHud,
			patternsHud = patternsHud,
		};
	}
}
