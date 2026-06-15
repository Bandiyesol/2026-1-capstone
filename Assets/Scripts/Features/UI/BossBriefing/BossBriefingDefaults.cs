/// <summary>

/// StageBossBriefDatabase 미지정 시 사용하는 7스테이지 보스 브리핑 폴백.

/// 랜덤 보스가 정해지기 전·카탈로그 미등록 보스용 기본값입니다.

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

					"펌킨킹", "숲",

					"바이옴: 숲\n보스가 피해를 입으면 근처에 부하를 소환합니다.",

					"· 호박 파티: 데미지 시 부하 소환\n· 끈질긴 씨앗: 추적 탄막\n추천 룬) 연쇄 · 폭발",

					"바이옴: 숲 / 호박 부하",

					"추적 씨앗 / 연쇄·폭발");

				return true;



			case 1:

				e = Build(

					"지하 굴착사", "동굴",

					"바이옴: 동굴\n땅굴을 파며 이동해 플레이어 공격을 무효화합니다.",

					"· 땅굴: 지하 이동·피격 무효\n· 땅굴 낙석: 등장 시 낙석\n추천 룬) 유도 · 빙결",

					"바이옴: 동굴 / 지하 무적",

					"땅굴 낙석 / 유도·빙결");

				return true;



			case 2:

				e = Build(

					"심해 괴수", "바다",

					"바이옴: 바다\n강한 조류와 저체력 무적·부하 패턴을 사용합니다.",

					"· 괴물 조류: 접근 압박·기절 시 중단\n· 비겁한 대장: HP 50% 이하 무적·부하\n추천 룬) 유도 · 증폭 · 흡수",

					"바이옴: 바다 / 조류·무적",

					"부하·기절 / 유도·증폭");

				return true;



			case 3:

				e = Build(

					"라바 티라노 골렘", "용암",

					"바이옴: 용암\n체력 구간마다 분열하며 빨라지는 용암 골렘입니다.",

					"· 분열: HP↓마다 개체 분열\n· 복구: 용암 타일 위 회복\n추천 룬) 연쇄 · 폭발 · 블랙홀",

					"바이옴: 용암 / 분열",

					"유도탄 / 연쇄·폭발");

				return true;



			case 4:

				e = Build(

					"서리 군주 늑대", "설원",

					"바이옴: 설원\n세 마리가 하나의 보스로 동작합니다.",

					"· 서리 늑대 삼형제: 3마리 모두 처치 필요\n· 돌진·빙결 탄막\n추천 룬) 연쇄 · 폭발",

					"바이옴: 설원 / 삼형제",

					"돌진·빙결 / 연쇄·폭발");

				return true;



			case 5:

				e = Build(

					"사막의 수호자", "사막",

					"바이옴: 사막\n기습 텔레포트와 모래바람으로 압박합니다.",

					"· 야습: 시선 시 뒤 텔레포트\n· 모래바람: 8방향\n추천 룬) 유도 · 증폭",

					"바이옴: 사막 / 기습",

					"모래바람 / 유도·증폭");

				return true;



			case 6:

				e = Build(

					"심연의 포식자", "보이드",

					"바이옴: 보이드\n상시 무적·흡혈. 패턴 중에만 피격 가능합니다.",

					"· 무적·흡혈: 패턴 중 해제\n· 추적자·광란의 춤\n추천 룬) 유도 · 증폭 · 흡수",

					"바이옴: 보이드 / 무적·흡혈",

					"유도·텔레포트 / 유도·증폭");

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

