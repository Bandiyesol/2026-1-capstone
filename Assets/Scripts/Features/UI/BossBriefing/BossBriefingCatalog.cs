using System.Collections.Generic;

using UnityEngine;



/// <summary>보스 프리팹별 브리핑 — 스테이지당 랜덤 보스에 맞춰 조회합니다.</summary>

public static class BossBriefingCatalog

{

	public struct Entry

	{

		public string displayName;

		public string traits;

		public string patterns;

		public string traitsHud;

		public string patternsHud;

	}



	static readonly Dictionary<string, Entry> ByPrefabName = BuildCatalog();



	public static bool TryGet(GameObject prefab, out Entry entry)

	{

		entry = default;

		if (prefab == null)

			return false;



		string name = NormalizePrefabName(prefab.name);

		if (ByPrefabName.TryGetValue(name, out entry))

			return true;



		if (name.EndsWith(" Core") && ByPrefabName.TryGetValue(name.Substring(0, name.Length - 6), out entry))

			return true;



		return false;

	}



	public static bool TryGetByPoolIndex(int poolBossIndex, GameObject[] bossPrefabs, out Entry entry)

	{

		entry = default;

		if (bossPrefabs == null || poolBossIndex < 0 || poolBossIndex >= bossPrefabs.Length)

			return false;



		return TryGet(bossPrefabs[poolBossIndex], out entry);

	}



	static string NormalizePrefabName(string name)

	{

		if (string.IsNullOrEmpty(name))

			return string.Empty;



		return name.Replace("(Clone)", string.Empty).Trim();

	}



	static Dictionary<string, Entry> BuildCatalog()

	{

		var map = new Dictionary<string, Entry>();



		void Add(string prefabName, string displayName, string traits, string patterns, string traitsHud = "", string patternsHud = "")

		{

			map[prefabName] = new Entry

			{

				displayName = displayName,

				traits = traits,

				patterns = patterns,

				traitsHud = traitsHud,

				patternsHud = patternsHud,

			};

		}



		Add("PumpkinKing", "펌킨킹",

			"바이옴: 숲\n보스가 피해를 입으면 근처에 플레이어를 방해하는 부하를 소환합니다.",

			"· 호박 파티: 보스가 데미지를 입을 때마다 근처에 부하 소환\n" +

			"· 끈질긴 씨앗: 일정 시간 동안 플레이어를 추적하는 탄막 발사\n" +

			"추천 룬) 연쇄 · 폭발",

			"바이옴: 숲 / 호박 부하 소환",

			"추적 씨앗 / 연쇄·폭발");



		Add("HeavenEyeBoss", "천공의 눈",

			"바이옴: 숲\n투명화 후 플레이어 주변을 여러 번 순간이동하는 공중형 보스입니다.",

			"· 순간이동: 투명화 상태로 변환한 뒤 플레이어 근처를 여러 번 순간이동\n" +

			"· 다발 눈빛: 잠시 추적을 멈추고 플레이어를 향해 빠른 탄막 발사\n" +

			"추천 룬) 유도 · 증폭",

			"바이옴: 숲 / 투명·텔레포트",

			"다발 눈빛 / 유도·증폭");



		Add("UndergroundDrillerBoss", "지하 굴착사",

			"바이옴: 동굴\n땅굴을 파며 이동해 플레이어 공격을 무효화하는 두더지형 보스입니다.",

			"· 땅굴: 이동 중 땅굴을 파며 이동, 플레이어 공격 무효화\n" +

			"· 땅굴 낙석: 땅굴 파는 중 잠시 지상으로 나올 때 낙석이 함께 떨어짐\n" +

			"추천 룬) 유도 · 빙결",

			"바이옴: 동굴 / 지하 무적",

			"땅굴 낙석 / 유도·빙결");



		Add("CaveRex", "동굴 티라노",

			"바이옴: 동굴\n부하와 함께 등장하며, 필드에 부하가 남아 있으면 보스 공격이 무효화됩니다.",

			"· 떼사냥: 등장 시 10마리 부하와 함께 등장, 일정 시간마다 부하 5마리씩 추가 소환. 부하가 있으면 보스 피격 무효\n" +

			"· 원거리 지원: 일정 시간마다 원형 탄막 발사\n" +

			"추천 룬) 유도·연쇄 · 폭발·중력",

			"바이옴: 동굴 / 부하 무적",

			"떼사냥·탄막 / 유도·연쇄");



		Add("DeepSeaMutant", "심해 괴수",

			"바이옴: 바다\n강한 조류로 접근을 막고, 저체력에서 무적·부하 소환 패턴을 사용합니다.",

			"· 괴물 조류: 상시 강한 조류 발산. 플레이어 기절 시 중단\n" +

			"· 알 수 없는 바다의 공포: 플레이어 근처에 순차 탄막. 피격 시 일정 확률 기절\n" +

			"· 비겁한 대장: HP 50% 이하 무적 + 부하 소환. 부하 1마리 처치마다 보스 HP 1% 감소\n" +

			"추천 룬) 유도 · 증폭 · 흡수",

			"바이옴: 바다 / 조류·기절",

			"무적·부하 / 유도·증폭·흡수");



		Add("DrownedSpiritBoss", "익사체 영혼",

			"바이옴: 바다\n물방울 디버프와 잠수 패턴으로 플레이어를 압박합니다.",

			"· 저주받은 물방울: 주기적으로 물방울 소환. 닿으면 DOT·투사체 차단. HP 30% 이하 시 2배\n" +

			"· 물귀신: 대기 후 잠수(무적·이동). 경고원 범위 진입 시 플레이어도 잠수·DOT. 추적 속도 1.5배\n" +

			"추천 룬) 연쇄·증폭·블랙홀 · 유도·증폭·빙결",

			"바이옴: 바다 / 물방울·잠수",

			"물귀신 / 연쇄·블랙홀");



		Add("StormDragonBoss", "폭풍의 해룡",

			"바이옴: 바다\n물기둥과 번개 탄막으로 필드를 압박하는 대형 보스입니다.",

			"· 물기둥 발산: 플레이어를 끌어당기는 물기둥 다수 소환. HP↓일수록 개수 증가\n" +

			"· 번개 탄막 발사: 부채꼴 번개 탄막 연속 발사\n" +

			"· 유도 번개 탄막: 원형 발사 후 플레이어 추적\n" +

			"추천 룬) 유도·증폭·분열 · 점멸·증폭·분열",

			"바이옴: 바다 / 물기둥",

			"번개·유도탄 / 유도·증폭");



		Add("LavaTyrano", "라바 티라노 골렘",

			"바이옴: 용암\n체력 구간마다 분열하며 작아지고 빨라지는 용암 골렘 보스입니다.",

			"· 분열: HP 75/50/25%마다 더 작은 개체로 분열. 공격력↓, 이속·쿨↓(빨라짐)\n" +

			"· 복구: 용암 타일 위에서 공격력 1.5배·체력 회복\n" +

			"· 용암 유도탄: 플레이어 추적. 개체가 작을수록 탄 수 감소. 피격 시 화상 확률\n" +

			"추천 룬) 연쇄·폭발·블랙홀 · 연쇄·빙결·흡수",

			"바이옴: 용암 / 분열·회복",

			"유도탄 / 연쇄·폭발");



		Add("LavaTyrano Core", "라바 티라노 골렘",

			"바이옴: 용암\n체력 구간마다 분열하며 작아지고 빨라지는 용암 골렘 보스입니다.",

			"· 분열: HP 75/50/25%마다 더 작은 개체로 분열. 공격력↓, 이속·쿨↓(빨라짐)\n" +

			"· 복구: 용암 타일 위에서 공격력 1.5배·체력 회복\n" +

			"· 용암 유도탄: 플레이어 추적. 개체가 작을수록 탄 수 감소. 피격 시 화상 확률\n" +

			"추천 룬) 연쇄·폭발·블랙홀 · 연쇄·빙결·흡수",

			"바이옴: 용암 / 분열·회복",

			"유도탄 / 연쇄·폭발");



		Add("VolcanoPumpkin Core", "화산 호박 소대",

			"바이옴: 용암\n3마리가 일렬로 이동하며 대장 교체·무적 규칙을 쓰는 소대형 보스입니다.",

			"· 일렬종대: 3마리 종대 이동. 맨 앞이 대장·피격 가능, 뒤는 무적. 1마리만 남으면 스텟 2배+\n" +

			"· 사격 개시: 정지 후 유도탄 발사. 화상 확률\n" +

			"· 소대 지원: 대장 정지 시 일반 몬스터 소환\n" +

			"추천 룬) 유도·연쇄·블랙홀 · 증폭·재귀·흡수",

			"바이옴: 용암 / 종대·대장",

			"사격·소환 / 유도·연쇄");



		Add("LavaEarthDragon", "용암 지룡",

			"바이옴: 용암\n이동 시 부하 소환, 용암 장판·나선 탄막으로 압박합니다.",

			"· 왕의 귀환: 이동 시 일정 확률로 일반 몬스터 소환. HP 75/50/25%마다 소환 수 2배\n" +

			"· 용암 나선: 보스 중심 나선 탄막. 피격 시 화상\n" +

			"· 용암의 기적: 주변 용암 장판. HP↓일수록 장판 확대\n" +

			"추천 룬) 연쇄·증폭·블랙홀 · 유도·증폭·빙결",

			"바이옴: 용암 / 부하·장판",

			"나선·장판 / 연쇄·블랙홀");



		Add("FrostWolfBoss Core", "서리 군주 늑대",

			"바이옴: 설원\n세 마리가 하나의 보스로 동작하며, 1마리만 남으면 강화됩니다.",

			"· 서리 늑대 삼형제: 3마리 모두 처치해야 사망. 1마리만 남으면 HP 회복·스텟 2배·3패턴 사용\n" +

			"· 돌진: 플레이어 방향 돌진\n" +

			"· 빙결 탄막: 빙결 탄막. 1마리만 남으면 부채꼴 다발\n" +

			"· 군주의 부름: 일반 몬스터 소환. 1마리만 남으면 2배\n" +

			"추천 룬) 연쇄 · 폭발",

			"바이옴: 설원 / 삼형제",

			"돌진·빙결 / 연쇄·폭발");



		Add("IceGiant", "빙하 거인",

			"바이옴: 설원\n이동·근접 내려찍기와 저체력 광폭화를 쓰는 설원 거인입니다.",

			"· 거인의 걸음: 이동마다 얼음 파편 원형 발사. 빙결. 공격·폭발 룬으로 파괴 가능\n" +

			"· 빙하 파쇄: 근접 시 내려찍기. 높은 데미지·빙결\n" +

			"· 광폭화: HP 50% 이하 이동속도 1.5배\n" +

			"추천 룬) 폭발 · 빙결",

			"바이옴: 설원 / 내려찍기",

			"얼음 파편 / 폭발·빙결");



		Add("DesertGuardianBoss", "사막의 수호자",

			"바이옴: 사막\n플레이어 시선 밖 기습 텔레포트와 모래바람으로 압박합니다.",

			"· 야습: 플레이어가 쳐다볼 때마다 뒤로 텔레포트. 이후 확률적 부채꼴 탄막\n" +

			"· 사막의 수호자: 상시 8방향 모래바람 소환\n" +

			"추천 룬) 유도 · 증폭",

			"바이옴: 사막 / 기습",

			"모래바람 / 유도·증폭");



		Add("ImmortalUndeadBoss", "불길의 언데드",

			"바이옴: 사막\n상시 무적이며, 주변 수호대를 모두 처치해야만 피격 가능합니다.",

			"· 불멸: 상시 무적. 필드 일반 몬스터 전멸 시 잠시 정지·피격 가능\n" +

			"· 언데드 수호대: 8방향 수호 몬스터. 1마리 처치마다 보스 HP 1% 감소. 30초 후 부활\n" +

			"· 언데드 공격대: 추적 몬스터 주기 소환. HP 50% 이하 2배\n" +

			"추천 룬) 연쇄 · 폭발",

			"바이옴: 사막 / 무적·수호대",

			"수호대·부활 / 연쇄·폭발");



		Add("AbyssalPredator", "심연의 포식자",

			"바이옴: 보이드\n상시 무적·흡혈 보스. 패턴 시전 중에만 피격 가능합니다.",

			"· 모든 걸 집어삼키는 자: 상시 무적. 피격 시 공격력 비례 회복. 패턴 중 잠깐 무적 해제\n" +

			"· 추적자: 플레이어 유도탄 다발\n" +

			"· 광란의 춤: 주변 연속 텔레포트 + 부채꼴 탄막\n" +

			"· 공허의 폭주: HP 30% 이하 모든 스텟 2배\n" +

			"추천 룬) 유도 · 증폭 · 흡수",

			"바이옴: 보이드 / 무적·흡혈",

			"유도·텔레포트 / 유도·증폭");



		Add("VoidCalamityBoss", "전 우주의 재앙",

			"바이옴: 보이드\n모든 바이옴 기믹과 분신·광역 패턴을 쓰는 최종 보스입니다.",

			"· 혼돈의 세계: 상시 전 맵 바이옴 기믹. HP↓일수록 간격 단축\n" +

			"· 재앙의 전도사: 1분마다 분신 3마리. 분신 처치마다 보스 HP 5% 감소\n" +

			"· 파멸: 30초 준비 후 전 맵 광역. 일정 피해로 중단 가능\n" +

			"추천 룬) 유도 · 증폭 · 분열",

			"바이옴: 보이드 / 기믹·분신",

			"파멸·분신 / 유도·증폭");



		return map;

	}

}

