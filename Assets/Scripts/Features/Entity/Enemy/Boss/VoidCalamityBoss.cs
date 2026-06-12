using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전 우주의 재앙 (VoidCalamityBoss)
/// - 부모 클래스(BossBase)의 패턴 쿨타임 시스템 상속 및 활용
/// - 모든 소환수(기믹 적), 분신, 탄막을 PoolManager를 통해 오브젝트 풀링 제어
/// </summary>
public class VoidCalamityBoss : BossBase
{
    // ============================================================
    // 혼돈의 세계 - 바이옴 기믹 소환 설정
    // ============================================================
    [Header("혼돈의 세계 - 바이옴 기믹 소환")]
    [Tooltip("소환할 바이옴 기믹 적들의 PoolManager 내 프리패브 인덱스 배열")]
    [SerializeField] private int[] voidMinionIndexes = new int[0];

    [Tooltip("보스의 체력이 100%일 때 적용되는 기본 소환 주기 (초)")]
    [SerializeField] private float voidSummonBaseInterval = 8f;

    [Tooltip("보스의 체력이 바닥날 때 단축되는 한계 소환 주기 (초)")]
    [SerializeField] private float voidSummonMinInterval = 3f;

    [Tooltip("보스 위치를 기준으로 기믹이 무작위 스폰될 수 있는 최대 원형 반경")]
    [SerializeField] private float voidSummonRadius = 6f;

    // 실시간 경과 시간을 계산하여 기믹 소환 주기를 체크하는 누적 타이머
    private float voidSummonTimer = 0f;

    // ============================================================
    // 재앙의 전도사 - 분신 기믹
    // ============================================================
    [Header("재앙의 전도사 - 분신 소환")]
    [Tooltip("PoolManager.bossPrefabs 내에 등록된 분신 호위 적의 풀링 인덱스")]
    [SerializeField] private int apostlePoolIndex = 0;

    [Tooltip("보스 중심점으로부터 분신들이 삼각형 구도로 배치될 거리 오프셋")]
    [SerializeField] private float apostleSpawnOffset = 3f;

    // 현재 분신 호위 페이즈(보스 무적 상태)가 활성화되어 작동 중인지 판별하는 플래그
    private bool isApostlePatternActive = false;

    // 풀에서 꺼내온 분신 게임 오브젝트(GameObject)들을 일괄 비활성화/관리하기 위한 참조 리스트
    private readonly List<GameObject> apostleObjects = new List<GameObject>();

    // 분신 컴포넌트에 직접 접근하여 실시간 생사태(IsDead)를 감시하기 위한 스크립트 추적 리스트
    private readonly List<VoidApostleController> activeApostles = new List<VoidApostleController>();

    // ============================================================
    // 파멸 - 광역 공격 패턴
    // ============================================================
    [Header("파멸 - 광역 공격 패턴")]
    [Tooltip("파멸 패턴 발동 시 360도로 방사할 탄막의 풀링 인덱스")]
    [SerializeField] private int doomBulletIndex = 3;

    [Tooltip("폭발 탄막을 전개하기 전, 자리에 멈춰 기를 모으는 선딜레이 시간 (초)")]
    [SerializeField] private float doomChargeDuration = 4f;

    [Tooltip("파멸 차징을 강제 취소(인터럽트)시키기 위해 차징 중 플레이어가 입혀야 하는 누적 데미지 통곡의 벽")]
    [SerializeField] private float doomInterruptDamageThreshold = 150f;

    [Tooltip("파멸 차징 중 바닥에 깔아줄 범위 경고 원형 이펙트 오브젝트")]
    [SerializeField] private GameObject doomWarningCircle;

    // 현재 보스가 파멸 주문을 차징(기 모으기)하고 있는 중인지 나타내는 상태 플래그
    private bool isDoomCharging = false;

    // 파멸 차징이 시작된 순간부터 보스가 입은 최종 누적 피해량을 저장하는 계측 변수
    private float doomAccumulatedDamage = 0f;

    // 파멸 차징 타이머 코루틴을 실행 도중 안전하게 제어하고 중도 정지하기 위한 코루틴 핸들 주소
    private Coroutine doomCoroutine = null;

    // ============================================================
    // 공통 시스템
    // ============================================================
    // 보스 및 분신들이 필드에 생성한 모든 탄막 오브젝트들을 유실 없이 추적하여 사망 시 소거하기 위한 리스트
    private readonly List<GameObject> spawnedBullets = new List<GameObject>();

    // 상시 기믹으로 소환된 바이옴 일반 적(몬스터) 오브젝트들을 보스 사망 시 함께 퇴장시키기 위한 추적 리스트
    private readonly List<GameObject> spawnedVoidGimmicks = new List<GameObject>();

    protected override void Start()
    {
        // 부모의 Start 로직(기본 컴포넌트 할당 등)이 있다면 먼저 수행하도록 베이스 호출 포함 가능
        // 리지드바디2D 구성 요소가 비어있다면 실시간 내부 룩업으로 자동 안전 컴포넌트 바인딩
        if (rigid == null) rigid = GetComponent<Rigidbody2D>();

        // 할당받은 보스 스크립터블 오브젝트(data)가 존재한다면 런타임 기초 능력치 정보 동기화
        if (data != null)
        {
            maxHealth = data.maxHealth;
            health = maxHealth;
            defense = data.damageReduction; // 부모 스탯 구조의 피해 감소율을 방어력으로 치환 반영
        }

        // 게임 시작 시 기믹 스폰용 누적 타이머 초기화
        voidSummonTimer = 0f;
    }

    protected override void Update()
    {
        // 게임 구조상 매니저가 유실되었거나, 일시정지 상태 혹은 플레이 중이 아니라면 프레임 연산 완전 차단
        if (GameManager.instance == null || !GameManager.instance.isLive) return;
        // 보스가 이미 생명을 다해 쓰러진 상태라면 불필요한 매 프레임 업데이트 연산 즉시 패스
        if (isDead) return;

        // [기믹 최우선 규칙]: 현재 분신 소환 패턴이 활성화된 특수 페이즈라면
        if (isApostlePatternActive)
        {
            // 상시 기믹 타이머 흐름을 멈추고 오직 호위 분신들의 생존 현황만 실시간 하드웨어 감시
            CheckApostlesStatus();
            return;
        }

        // 분신 페이즈가 아닐 때만 평시 상태로 간주하여 상시 바이옴 기믹 소환 루틴 가동
        UpdateVoidSummon();
    }

    /// <summary>
    /// 외부 타임라인 제어기 혹은 부모의 패턴 다이스 시스템에서 무작위 스킬을 발동시킬 때 호출하는 공용 매개 진입점
    /// </summary>
    public void ExecuteRandomPattern()
    {
        // 반반(50%) 확률 연산을 통해 분신 소환 수호 패턴 또는 파멸 광역 방사 패턴 중 하나를 무작위 시전
        if (Random.Range(0, 2) == 0) TriggerApostlePattern();
        else TriggerDoomPattern();
    }

    // ============================================================
    // [패턴 1] 혼돈의 세계 : 현재 체력량에 반비례하여 가속되는 실시간 적 소환
    // ============================================================
    private void UpdateVoidSummon()
    {
        // 매 프레임의 델타 타임을 누적 시켜 타이머 갱신
        voidSummonTimer += Time.deltaTime;

        // 보스의 현재 체력 비율을 0.0(사망) ~ 1.0(만개) 사이의 안전 규격 스케일로 정규화
        float healthRatio = Mathf.Clamp01(health / maxHealth);

        // 선형 보간(Lerp) 연산을 적용하여 보스의 체력이 떨어질수록(0에 수렴할수록) 스폰 대기 주기가 min값까지 극단적으로 짧아짐
        float currentInterval = Mathf.Lerp(voidSummonMinInterval, voidSummonBaseInterval, healthRatio);

        // 유동적으로 변화하는 타깃 주기에 도달했을 때 기믹을 사방에 소환하고 타이머 리셋
        if (voidSummonTimer >= currentInterval)
        {
            voidSummonTimer = 0f;
            SpawnVoidGimmick();
        }
    }

    private void SpawnVoidGimmick()
    {
        // 인덱스 풀링 데이터 예외 가드 및 싱글톤 풀 매니저 널 체크
        if (voidMinionIndexes == null || voidMinionIndexes.Length == 0 || PoolManager.Instance == null) return;

        // 소환 가능한 바이옴 적 인덱스 중 하나를 무작위로 추첨
        int randomIndex = voidMinionIndexes[Random.Range(0, voidMinionIndexes.Length)];

        // 일반 에너미 풀(GetEnemy)에서 재사용 대기 중인 게임 오브젝트 참조 인출
        GameObject gimmick = PoolManager.Instance.GetEnemy(randomIndex);
        if (gimmick != null)
        {
            // 보스의 현재 중심 좌표를 기준으로 설정된 소환 반경 내 임의의 원형 무작위 2D 좌표 연산 후 배치
            gimmick.transform.position = (Vector2)transform.position + Random.insideUnitCircle * voidSummonRadius;
            gimmick.SetActive(true); // 필드 상에 실시간 기능 활성화 시전
            spawnedVoidGimmicks.Add(gimmick); // 보스 급사 혹은 클리어 시 일괄 회수를 위해 관리 리스트에 적재
        }
    }

    // ============================================================
    // [패턴 2] 재앙의 전도사 : 3대 유니크 분신 소환 및 보스 절대 무적화
    // ============================================================
    private void TriggerApostlePattern()
    {
        anim.SetTrigger("Summon");

        isApostlePatternActive = true; // 무적 상태 돌입용 플래그 마킹 (이 상태 동안 TakeDamage 대미지 완전 면역)
        canMove = false;               // 기믹 연출 수행 및 자리를 지키기 위해 보스의 자체 AI 이동 정지

        // 보스가 이동 관성에 의해 미끄러지는 현상을 방지하고자 물리 선형 속도를 즉각 제로(0)로 중립화
        if (rigid != null) rigid.linearVelocity = Vector2.zero;

        // 3마리의 분신이 완전히 고유하고 서로 다른 ApostleType을 가지도록 열거형 배열 기본 세팅
        VoidApostleController.ApostleType[] types =
        {
            VoidApostleController.ApostleType.Seed,
            VoidApostleController.ApostleType.Wave,
            VoidApostleController.ApostleType.Advent
        };

        // 피셔-예이츠 셔플(Fisher-Yates Shuffle) 알고리즘을 구동하여 중복 없는 유니크 타입 배열 무작위 무작위 난수 셔플
        for (int s = types.Length - 1; s > 0; s--)
        {
            int r = Random.Range(0, s + 1);
            (types[s], types[r]) = (types[r], types[s]); // 튜플을 이용한 원소 스왑 연산
        }

        // 360도 평면 공간을 완벽히 3등분한 120도 단일 배치 각 오프셋 지정
        float angleStep = 120f;
        for (int i = 0; i < 3; i++)
        {
            // 루프 순번 각도에 삼각함수 호도법 변환(Deg2Rad)을 투과하여 코사인(Cos), 사인(Sin) 기반 원형 좌표 벡터 연산
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * apostleSpawnOffset;

            // 보스/분신 전용 풀 카테고리(GetBoss)에서 재사용 대기 중인 분신 오브젝트 참조 인출
            GameObject apostleObj = PoolManager.Instance.GetBoss(apostlePoolIndex);
            if (apostleObj == null) continue;

            // 미리 연산해 둔 삼각 편대 꼭짓점 좌표 좌표로 분신 이동 후 필드에 개방
            apostleObj.transform.position = spawnPos;
            apostleObj.SetActive(true);

            // 해당 오브젝트의 제어 전담 컨트롤러 컴포넌트를 정밀 추출
            VoidApostleController apostle = apostleObj.GetComponent<VoidApostleController>();
            if (apostle != null)
            {
                // 완전히 셔플되어 중복이 차단된 고유 속성 타입(types[i]) 정보와 공유 투사체 목록 주소 바인딩 초기화 하달
                apostle.Init(types[i], data, target, spawnedBullets);
                activeApostles.Add(apostle); // 프레임 레벨 생사 추적 리스트에 연결
            }
            apostleObjects.Add(apostleObj); // 페이즈 해제 시 풀 일괄 반환을 위한 컨테이너 적재
        }
    }

    private void CheckApostlesStatus()
    {
        // 가비지 컬렉션 부하 유발을 차단하고 탐색 도중 원소 탈락 순서 꼬임을 예방하고자 역순(Count-1부터 하향) 루프 가동
        for (int i = activeApostles.Count - 1; i >= 0; i--)
        {
            // 예상치 못하게 참조 주소가 소실되었거나, 분신 스크립트 내부 규칙 상 격파 판정(IsDead)이 완료된 객체 검출
            if (activeApostles[i] == null || activeApostles[i].IsDead)
                activeApostles.RemoveAt(i); // 실시간 생존 연산 목록에서 안전하게 제외 분리
        }

        // 실시간 연산 도중 필드 상의 호위 분신이 완벽히 0마리가 되었다면 기믹 파훼 성공으로 간주, 패턴 정상 종료 시퀀스 돌입
        if (activeApostles.Count == 0)
            EndApostlePattern(interrupted: false);
    }

    private void EndApostlePattern(bool interrupted)
    {
        // 무적 플래그 해제 및 보스의 자율 이동 컴포넌트 제어권 복구
        isApostlePatternActive = false;
        canMove = true;

        // 필드에 생존해 있거나 깔려 있던 모든 분신 원형 오브젝트들을 풀매니저 규칙에 입각해 비활성화 회수
        foreach (GameObject obj in apostleObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
        // 다음 정밀 페이즈 기동에 영향을 주지 않도록 잔여 참조 포인터 리스트 일괄 초기화
        apostleObjects.Clear();
        activeApostles.Clear();

        // 보스가 사망하여 강제 중단(interrupted == true)된 특수 케이스가 아닌, 플레이어가 기믹을 완수해 종료된 상황인 경우
        if (!interrupted)
        {
            // [파훼 보상 기믹]: 보스의 최대 체력에 직결되는 고정 피해 5%를 페널티로 직접 감산 타격
            health -= maxHealth * 0.05f;

            // 페널티 강제 타격 연산으로 체력이 소진되었다면 사망 프로세스 가동
            if (health <= 0) Dead();
        }
    }

    // ============================================================
    // [패턴 3] 파멸 : 캐스팅 인터럽트 시스템 및 360도 전방위 탄막 방사
    // ============================================================
    private void TriggerDoomPattern()
    {
        isDoomCharging = true;           // 파멸 피해량 누적 카운팅 활성화 플래그 온
        canMove = false;                 // 대주문 주문 영창 연출을 위해 보스 기동 정지
        doomAccumulatedDamage = 0f;      // 신규 패턴 시작에 따른 누적 피해량 미터기 리셋

        // 특정 프레임 타임라인을 안전하게 제어하고 중도 취소하기 위해 비동기 코루틴 가동 후 핸들 주소 박싱 저장
        doomCoroutine = StartCoroutine(DoomChargeRoutine());
    }

    private IEnumerator DoomChargeRoutine()
    {
        // 주문 차징 영창과 동시에 바닥 영역 시각적 예고 경고 범위 서클 오브젝트 상영 개시
        if (doomWarningCircle != null) doomWarningCircle.SetActive(true);

        // 기를 모으도록 설계된 설정 임계 시간(4초) 동안 이 코루틴 흐름을 양보 대기
        yield return new WaitForSeconds(doomChargeDuration);

        // 지정된 영창 시간 동안 플레이어가 딜 컷을 넘기지 못해 차징 상태가 온전히 유지되었다면 최종 주문 파멸 폭발 실행
        if (isDoomCharging)
            FireDoomBlast();

        // 코루틴 사이클이 완전히 종료되었으므로 연동 핸들 참조 주소 초기화
        doomCoroutine = null;
        canMove = true;         // 보스 다시 자유 이동 상태 복구
    }

    private void FireDoomBlast()
    {
        isDoomCharging = false;
        canMove = true;

        if (doomWarningCircle != null) doomWarningCircle.SetActive(false);

        if (PoolManager.Instance == null) return;

        anim.SetTrigger("Attack");

        // 보스 위치에 장판 오브젝트 소환 (IceGiantBoss SlamPattern과 동일한 방식)
        GameObject doom = PoolManager.Instance.GetBossBullet(doomBulletIndex);
        if (doom == null) return;

        doom.transform.position = transform.position;
        spawnedBullets.Add(doom);
    }

    // ============================================================
    // 피격 연산 및 플레이어의 기믹 인터럽트(방해) 처리
    // ============================================================
    public override void TakeDamage(float damageAmount)
    {
        // [분신 페이즈 절대 규칙]: 수호 분신이 단 1마리라도 필드에 배치되어 있다면 완벽한 절대 무적이므로 피해량 연산을 수행하지 않고 즉시 스킵
        if (isApostlePatternActive) return;

        // 보스의 방어 스탯 수치(피해량 경감율)를 대입 연산하여 필터링된 최종 최종 대미지 산출
        float finalDamage = damageAmount * (1f - defense);
        health -= finalDamage; // 최종 차감 대미지를 실시간 보스 체력에서 차감

        // [파멸 인터럽트 감지 규칙]: 현재 보스가 파멸 캐스팅 기 모으기 상태에 놓여 있다면
        if (isDoomCharging)
        {
            // 플레이어가 입힌 최종 실제 피해 수치를 누적 감지 계측기에 고스란히 축적 스택
            doomAccumulatedDamage += finalDamage;

            // 실시간 적립된 누적 딜량이 패턴 파쇄 임계값(150f)을 돌파했는지 조건 검사
            if (doomAccumulatedDamage >= doomInterruptDamageThreshold)
            {
                // 영창 시간 지연을 처리하고 있던 파멸 코루틴 타임라인이 존재한다면 엔진 레벨에서 중도 파괴(Stop) 지시
                if (doomCoroutine != null)
                {
                    StopCoroutine(doomCoroutine);
                    doomCoroutine = null; // 메모리 주소 초기화
                }

                // 탄막 방사(FireDoomBlast) 시퀀스로 흐름이 이탈하는 것을 전면 무산시키고자 차징 플래그를 강제 오프하고 경직 상태(이동 허용)로 비상 탈출
                isDoomCharging = false;
                canMove = true;

                // 패턴 취소 처리에 따른 바닥 경고 원형 오브젝트 즉시 비활성화 은닉
                if (doomWarningCircle != null) doomWarningCircle.SetActive(false);
            }
        }

        // 피해 반영 최종 연산 결과 보스의 생명력이 0 이하 사망 임계선 밑으로 가라앉았다면 소멸 시퀀스 트리거
        if (health <= 0) Dead();
    }

    protected override void Dead()
    {
        // 중복 사망 연출이 프레임 중복으로 다중 연산되는 버그를 예방하기 위한 전역 사망 락 가드
        if (isDead) return;
        isDead = true;

        // 사망 시점에 혹여나 잔존하여 돌아가고 있을지 모르는 파멸 주문 차징 코루틴 프로세스를 완벽히 강제 종료
        if (doomCoroutine != null) StopCoroutine(doomCoroutine);

        // 보스 소멸에 맞춰 바닥에 깔려 있던 파멸 경고 범위를 깔끔하게 비활성화
        if (doomWarningCircle != null) doomWarningCircle.SetActive(false);

        // 풀링 최적화 청소: 필드 상에 무결하게 깔아두어 관리 중이던 호위 분신 오브젝트 전량을 유실 없이 풀 비활성화 회수
        foreach (GameObject obj in apostleObjects) if (obj != null) obj.SetActive(false);
        // 사방에 날아가고 있던 잔여 탄막 투사체 오브젝트들도 플레이어 억까 방지를 위해 전량 비활성화 소거 회수
        foreach (GameObject bullet in spawnedBullets) if (bullet != null) bullet.SetActive(false);
        spawnedBullets.Clear(); // 메모리 참조 누수 방지용 클리어

        // 혼돈의 세계 패턴으로 소환되어 필드에 흩어져 잔존하던 모든 바이옴 일반 적(몬스터)들까지 일괄 비활성화 영면 퇴장 처리
        foreach (GameObject gimmick in spawnedVoidGimmicks) if (gimmick != null) gimmick.SetActive(false);
        spawnedVoidGimmicks.Clear(); // 댕글링 포인터 방지용 클리어

        // 보스 본인의 게임 오브젝트 활성 상태를 오브젝트 풀 반환 규격에 맞춰 꺼줌으로써 최종 사망 처리 완료
        gameObject.SetActive(false);
    }
}