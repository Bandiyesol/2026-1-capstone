using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidCalamityBoss : BossBase
{
    [Header("혼돈의 세계 - 바이옴 기믹 소환")]
    [Tooltip("소환할 공허 바이옴 기믹 몬스터들의 PoolManager 내 오브젝트 풀 인덱스 배열")]
    [SerializeField] private int[] voidMinionIndexes = new int[0];

    [Tooltip("보스의 체력이 100%일 때 적용되는 기본 기믹 소환 주기 (초 단위)")]
    [SerializeField] private float voidSummonBaseInterval = 8f;

    [Tooltip("보스의 체력이 0%에 가까워질 때 단축되는 최소 한계 소환 주기 (초 단위)")]
    [SerializeField] private float voidSummonMinInterval = 3f;

    [Tooltip("보스의 현재 위치를 기준으로 기믹이 무작위로 생성될 수 있는 최대 원형 반경 범위")]
    [SerializeField] private float voidSummonRadius = 6f;

    [Tooltip("보스의 체력이 100%일 때 한 번에 소환되는 기믹 몬스터의 최소 개수")]
    [SerializeField] private int voidSummonMinCount = 1;

    [Tooltip("보스의 체력이 0%에 가까워질 때 한 번에 소환되는 기믹 몬스터의 최대 개수")]
    [SerializeField] private int voidSummonMaxCount = 4;

    [Tooltip("실시간으로 흐른 시간을 누적하여 소환 주기를 체크하는 내부 타이머 변수")]
    private float voidSummonTimer = 0f;

    [Header("재앙의 전도사 - 분신 소환")]
    [Tooltip("PoolManager에 등록된 전도사(분신) 호위 몬스터 프리패브의 오브젝트 풀 인덱스")]
    [SerializeField] private int apostlePoolIndex = 0;

    [Tooltip("보스 중심점으로부터 분신들이 삼각형 구도로 사방에 배치될 거리 오프셋 수치")]
    [SerializeField] private float apostleSpawnOffset = 3f;

    [Tooltip("현재 분신 소환 패턴(보스 무적 상태 페이즈)이 발동 중인지 나타내는 상태 플래그")]
    private bool isApostlePatternActive = false;

    [Tooltip("생성된 분신 게임 오브젝트들을 참조하여 추후 일괄 비활성화/관리를 위해 담아두는 리스트")]
    private readonly List<GameObject> apostleObjects = new List<GameObject>();

    [Tooltip("분신들의 실시간 사망 및 파괴 여부를 감시하기 위해 제어 스크립트 컴포넌트를 담아두는 리스트")]
    private readonly List<VoidApostleController> activeApostles = new List<VoidApostleController>();

    [Header("파멸 - 광역 공격 패턴")]
    [Tooltip("광역 파멸 패턴 발동 시 풀에서 꺼내어 사용할 폭발 탄막/장판의 오브젝트 풀 인덱스")]
    [SerializeField] private int doomBulletIndex = 3;

    [Tooltip("폭발 탄막이 발사되기 전, 제자리에 멈춰 서서 주문을 캐스팅(차징)하는 총 대기 시간")]
    [SerializeField] private float doomChargeDuration = 4f;

    [Tooltip("파멸 차징 상태의 보스를 저지(인터럽트)하기 위해 플레이어가 단시간에 입혀야 하는 누적 데미지 요구량")]
    [SerializeField] private float doomInterruptDamageThreshold = 150f;

    [Tooltip("파멸 주문이 차징되는 동안 바닥에 활성화되어 위험 구역을 보여줄 원형 경고 범위 이펙트 오브젝트")]
    [SerializeField] private GameObject doomWarningCircle;

    [Tooltip("현재 보스가 파멸 주문을 영창하며 기를 모으고 있는 차징 상태인지 판별하는 플래그")]
    private bool isDoomCharging = false;

    [Tooltip("파멸 차징이 시작된 이후 보스가 플레이어로부터 받은 실시간 누적 데미지 양을 기록하는 변수")]
    private float doomAccumulatedDamage = 0f;

    [Tooltip("시간 경과 및 차징 취소 처리를 제어하는 코루틴의 중복 방지 및 강제 정지용 참조 핸들")]
    private Coroutine doomCoroutine = null;

    [Tooltip("보스가 시전한 공격 탄막 및 기믹 투사체 오브젝트들의 실시간 추적 및 메모리 회수용 리스트")]
    private readonly List<GameObject> spawnedBullets = new List<GameObject>();

    [Tooltip("필드에 상시 주기적으로 스폰되는 일반 공허 기믹 몬스터들의 추적 및 일괄 제거용 리스트")]
    private readonly List<GameObject> spawnedVoidGimmicks = new List<GameObject>();

    protected override void Start()
    {
        base.Start(); // 부모 클래스인 BossBase의 기본 초기화 세팅(타깃 추적 등) 수행

        if (rigid == null) rigid = GetComponent<Rigidbody2D>(); // 리지드바디가 누락된 경우 컴포넌트 안전하게 자동 할당

        if (data != null) // 보스 능력치 데이터 시트가 존재하는지 검사
        {
            maxHealth = data.maxHealth; // 데이터 시트에 저장된 최대 체력을 할당
            health = maxHealth; // 시작 시점에 현재 체력을 최대 체력 수치로 전체 충전
            defense = data.damageReduction; // 피해 감소율(0~1) 스탯을 방어력(defense) 필드에 대입
        }

        voidSummonTimer = 0f; // 첫 기믹 소환 쿨타임을 위해 누적 스폰 타이머를 0으로 초기화
    }

    protected override void Update()
    {
        base.Update(); // 부모 클래스의 프레임별 기본 타이머 및 내부 쿨타임 연산 작동

        if (GameManager.instance == null || !GameManager.instance.isLive) return; // 게임 매니저가 없거나 일시정지 상태면 정지
        if (isDead) return; // 보스가 이미 사망한 상태라면 하위 루프 연산을 전부 스킵

        if (isApostlePatternActive) // 만약 현재 전도사(분신) 패턴 페이즈가 작동 중이라면
        {
            CheckApostlesStatus(); // 실시간으로 소환된 분신들의 생존 상태를 정밀 체크하러 이동
            return; // 분신 페이즈 중에는 보스가 무적이므로 상시 기믹 소환 타이머 처리를 차단
        }

        UpdateVoidSummon(); // 평시 상태일 때만 주기적으로 소환수를 스폰하는 가변 타이머 로직 업데이트
    }

    protected override void StartRandomPattern()
    {
        ExecuteRandomPattern(); // 부모의 기본 무작위 패턴 호출 시, 커스텀 무작위 패턴 실행 함수로 연결
    }

    public void ExecuteRandomPattern()
    {
        if (Random.Range(0, 2) == 0) TriggerApostlePattern(); // 50% 확률로 0이 나오면 전도사 분신 소환 페이즈 시전
        else TriggerDoomPattern();                            // 50% 확률로 1이 나오면 파멸 광역기 차징 패턴 시전
    }

    private void UpdateVoidSummon()
    {
        voidSummonTimer += Time.deltaTime; // 전 프레임 대비 경과 시간을 타이머에 누적

        float healthRatio = Mathf.Clamp01(health / maxHealth); // 현재 체력 비율을 0.0 ~ 1.0 범위로 제한하여 계산
        // 체력 비율에 맞춰 소환 주기를 선형 보간 (체력이 줄어들수록 간격이 최소 수치에 가깝게 짧아짐)
        float currentInterval = Mathf.Lerp(voidSummonMinInterval, voidSummonBaseInterval, healthRatio);

        if (voidSummonTimer >= currentInterval) // 누적 시간이 계산된 동적 소환 주기를 초과했는지 체크
        {
            voidSummonTimer = 0f; // 소환 주기를 정상 만족했으므로 카운트 타이머 리셋
            SpawnVoidGimmick();   // 실제 공허 기믹 몬스터 스폰 프로세스 구동
        }
    }

    private void SpawnVoidGimmick()
    {
        // 배열이 비어있거나 풀 매니저 싱글톤 인스턴스가 존재하지 않는 비정상 예외 상황 가드 코드
        if (voidMinionIndexes == null || voidMinionIndexes.Length == 0 || PoolManager.Instance == null) return;

        float healthRatio = Mathf.Clamp01(health / maxHealth); // 체력 비율 연산
        // 체력이 낮을수록 난이도 상승을 위해 한 번에 소환될 기믹 몬스터 수를 최대 수치에 가깝게 보간 및 반올림
        int spawnCount = Mathf.RoundToInt(Mathf.Lerp(voidSummonMaxCount, voidSummonMinCount, healthRatio));

        for (int i = 0; i < spawnCount; i++) // 결정된 소환 개수만큼 루프 반복 수행
        {
            int randomIndex = voidMinionIndexes[Random.Range(0, voidMinionIndexes.Length)]; // 인덱스 배열에서 임의의 속성 인덱스 추첨
            GameObject gimmick = PoolManager.Instance.GetGimmick(randomIndex); // 오브젝트 풀 시스템에서 알맞은 기믹 프리패브 인출

            if (gimmick != null) // 풀에서 정상적으로 오브젝트가 반환되었는지 체크
            {
                // 보스의 현재 좌표를 기반으로 무작위 원형 범위(Inside Unit Circle) 내에 스폰 좌표 설정 및 오프셋 부여
                gimmick.transform.position = (Vector2)transform.position + Random.insideUnitCircle * voidSummonRadius;
                gimmick.SetActive(true); // 비활성화되어 있던 풀링 오브젝트를 활성화하여 필드에 등장시킴
                spawnedVoidGimmicks.Add(gimmick); // 보스 사망 또는 페이즈 전환 시 일괄 정리를 위해 추적 리스트에 삽입
            }
        }
    }

    private void TriggerApostlePattern()
    {
        anim.SetTrigger("Summon"); // 소환 동작 전용 애니메이션 상태 트리거 구동

        isApostlePatternActive = true; // 무적 및 특수 페이즈 전환 플래그 셋업
        isPatternPlaying = true;       // 부모 클래스의 기본 자동 패턴 쿨타임 타이머 흐름을 일시 정지
        canMove = false;               // 보스 자체의 인공지능 네비게이션 및 자율 이동 차단
        if (rigid != null) rigid.linearVelocity = Vector2.zero; // 이동 정지 시 물리적 관성이 남아 미끄러지는 현상 제거

        // 소환할 분신의 3가지 유니크 속성(씨앗, 파동, 강림) 타입을 배열로 순차 구성
        VoidApostleController.ApostleType[] types =
        {
            VoidApostleController.ApostleType.Seed,
            VoidApostleController.ApostleType.Wave,
            VoidApostleController.ApostleType.Advent
        };

        // 피셔-예이츠(Fisher-Yates) 셔플 알고리즘을 사용해 3가지 속성의 배치 순서를 무작위로 혼합
        for (int s = types.Length - 1; s > 0; s--)
        {
            int r = Random.Range(0, s + 1); // 현재 인덱스 이하의 무작위 위치 선택
            (types[s], types[r]) = (types[r], types[s]); // 튜플 구조 분해 스왑을 이용해 두 원소의 위치를 서로 맞바꿈
        }

        float angleStep = 120f; // 360도를 3등분하여 정삼각형 구도를 형성하기 위한 배치 각도 단계 수치
        for (int i = 0; i < 3; i++) // 3마리의 분신을 원형 배치하기 위한 생성 루프
        {
            float angle = i * angleStep * Mathf.Deg2Rad; // 각도를 삼각함수 연산용 호도법(라디안) 수치로 정밀 변환
            // 코사인과 사인을 활용하여 보스 중심점에서 삼각 배율 거리 오프셋이 적용된 로컬 좌표 벡터 연산
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * apostleSpawnOffset;

            GameObject apostleObj = PoolManager.Instance.GetBoss(apostlePoolIndex); // 보스 소환수 전용 풀에서 분신 인출
            if (apostleObj == null) continue; // 풀에 잔여 오브젝트가 없어 인출에 실패했다면 예외 스킵

            apostleObj.transform.position = spawnPos; // 연산된 월드 좌표계 위치로 분신의 스폰 위치 조정
            apostleObj.SetActive(true); // 분신 오브젝트 실시간 활성화

            VoidApostleController apostle = apostleObj.GetComponent<VoidApostleController>(); // 분신 제어 컴포넌트 획득
            if (apostle != null) // 컴포넌트 유효성 검사
            {
                // 셔플된 무작위 속성 타입, 원본 보스 스탯, 추적할 타깃 데이터, 생성된 탄막 리스트를 일괄 전달하며 초기화
                apostle.Init(types[i], data, target, spawnedBullets);
                activeApostles.Add(apostle); // 실시간 생존 연산 및 생사 확인을 위한 감시 리스트에 바인딩
            }
            apostleObjects.Add(apostleObj); // 페이즈 강제 종료 시 메모리 회수 처리를 위한 추적 목록에 추가
        }
    }

    private void CheckApostlesStatus()
    {
        // 리스트 원소 삭제 시 인덱스가 앞으로 밀려 연산이 누락되는 현상을 원천 방지하기 위해 역순 루프 탐색 실행
        for (int i = activeApostles.Count - 1; i >= 0; i--)
        {
            // 분신 스크립트가 파괴되었거나 내부 상태가 사망(IsDead == true) 상태인지 체크
            if (activeApostles[i] == null || activeApostles[i].IsDead)
                activeApostles.RemoveAt(i); // 조건을 만족해 사망한 분신은 실시간 실효성 감시 리스트에서 즉각 배제
        }

        if (activeApostles.Count == 0) // 감시 리스트 안의 모든 분신이 제거되어 카운트가 0이 되었는지 판단
            EndApostlePattern(interrupted: false); // 모든 분신이 완전 소멸했다면 플레이어의 기믹 성공이므로 패턴 정상 클리어 종료
    }

    private void EndApostlePattern(bool interrupted)
    {
        isApostlePatternActive = false; // 보스의 페이즈 무적 상태를 원상 복구하여 해제
        isPatternPlaying = false;       // 정지되어 있던 부모 패턴 제어용 내부 타이머 재개 활성화
        canMove = true;                 // 고정되어 있던 보스의 자율 이동 및 AI 행동 권한 전면 복구

        foreach (GameObject obj in apostleObjects) // 필드에 등록되었던 모든 분신 오브젝트를 순회
        {
            if (obj != null) obj.SetActive(false); // 생존해 있거나 비활성화되지 않은 분신들을 전부 오브젝트 풀로 일괄 회수
        }
        apostleObjects.Clear(); // 캐싱용 오브젝트 리스트 메모리 청소
        activeApostles.Clear(); // 감시용 컴포넌트 리스트 메모리 청소

        if (!interrupted) // 만약 타임아웃 등의 강제 중단이 아닌 플레이어의 기믹 파훼로 정상 종료되었다면
        {
            health -= maxHealth * 0.05f; // [기믹 성공 보상] 보스의 최대 체력 기준 5%의 절대 고정 자해 대미지 페널티 부여
            if (health <= 0) Dead();     // 자해 대미지로 인해 체력이 0 이하로 떨어졌을 경우 즉시 사망 처리 프로세스로 이관
        }
    }

    private void TriggerDoomPattern()
    {
        isDoomCharging = true;           // 파멸 주문 영창 시전 플래그 활성화
        isPatternPlaying = true;         // 부모 클래스의 기본 무작위 자동 패턴 주기 카운트 타이머 중지
        canMove = false;                 // 강력한 광역 스펠 시전을 위해 시전 중 보스의 이동 상태를 강제 고정
        doomAccumulatedDamage = 0f;      // 이전 차징 시 누적되었던 피격 데미지 기록 카운터를 0으로 초기화

        doomCoroutine = StartCoroutine(DoomChargeRoutine()); // 일정 시간 차징 및 취소 처리를 관장하는 코루틴 스트림 시전
    }

    private IEnumerator DoomChargeRoutine()
    {
        if (doomWarningCircle != null) doomWarningCircle.SetActive(true); // 시각적으로 폭발 범위를 표시할 장판 경고 데칼 활성화

        yield return new WaitForSeconds(doomChargeDuration); // 설정된 주문 영창 시간(선딜레이) 동안 제자리에서 차징 대기

        if (isDoomCharging) // 영창 시간이 흐르는 동안 플레이어의 방해로 차징이 깨지지 않고 유지되었는지 검사
        {
            FireDoomBlast(); // 끊기지 않았다면 파멸 광역 폭발 공격 최종 투사체 발사 프로세스 가동
        }

        doomCoroutine = null; // 사용이 종료된 코루틴 참조 핸들 리셋

        yield return new WaitForSeconds(1.0f); // 폭발 발사 이후 보스가 취하는 잠깐의 후딜레이 모션 시간 추가 대기

        isPatternPlaying = false; // 보스 액션 종결에 따라 부모 클래스 패턴 타이머 흐름 정상 재개
        canMove = true;           // 후딜레이가 끝났으므로 보스의 자유 이동 권한 다시 오픈
    }

    private void FireDoomBlast()
    {
        isDoomCharging = false; // 주문 캐스팅 상태 종료 처리

        if (doomWarningCircle != null) doomWarningCircle.SetActive(false); // 화면에 표시 중이던 경고 장판 데칼 비활성화 소거
        if (PoolManager.Instance == null) return; // 싱글톤 인스턴스 예외 가드

        anim.SetTrigger("Attack"); // 보스의 강력한 광역 타격 공격 애니메이션 모션 트리거 시전

        GameObject doom = PoolManager.Instance.GetBossBullet(doomBulletIndex); // 오브젝트 풀 매니저에서 광역 파멸 탄환 객체 인출
        if (doom == null) return; // 풀 인출 예외 발생 시 하위 처리 차단

        doom.transform.position = transform.position; // 광역 장판 탄환의 원점 중심 좌표를 보스의 현재 월드 위치로 설정
        spawnedBullets.Add(doom); // 보스 사망 또는 룸 클리어 시 강제 정리를 위해 공격 탄환 추적 목록에 바인딩
    }

    public override void TakeDamage(float damageAmount)
    {
        if (isApostlePatternActive) // 만약 현재 전도사(분신) 페이즈 패턴이 돌아가는 상태라면 (보스 무적)
        {
            StartCoroutine(FlashInvincible(new Color(0.4f, 0.15f, 0.6f))); // 데미지를 흡수하고 공허 특유의 보라색 무적 반짝임 이펙트만 송출 후 무시
            return; // 타격 데미지 연산을 원천 차단하고 탈출
        }

        float finalDamage = damageAmount * (1f - defense); // 플레이어 원본 데미지에 보스의 방어력(피해 감소율) 비율을 반영한 실 최종 데미지 산출
        health -= finalDamage; // 산출된 실 데미지를 보스의 현재 체력에서 차감

        if (isDoomCharging) // 보스가 무적 상태는 아니지만 파멸 주문을 영창하고 기를 모으던 중이었는지 확인
        {
            doomAccumulatedDamage += finalDamage; // 영창 중 받은 최종 실 데미지 수치를 인터럽트 카운터 누적치에 적립

            if (doomAccumulatedDamage >= doomInterruptDamageThreshold) // 누적된 피격 데미지가 주문 파괴 임계값을 초과했는지 체크
            {
                if (doomCoroutine != null) // 구동되고 있던 영창 대기 코루틴이 유효하게 살아있는지 검사
                {
                    StopCoroutine(doomCoroutine); // 대기 중이던 차징 영창 코루틴 강제 정지
                    doomCoroutine = null;
                }

                isDoomCharging = false;   // 차징 모드 강제 취소 및 경직 탈출(이동 회복)
                isPatternPlaying = false; // BossBase 쿨타임 타이머 재개
                canMove = true;

                if (doomWarningCircle != null) doomWarningCircle.SetActive(false); // 바닥 경고 서클 즉시 소거
            }
        }

        if (health <= 0) Dead(); // 피격 후 체력이 0 이하면 사망 프로세스 호출
    }

    protected override void Dead()
    {
        if (isDead) return; // 중복 사망 연산 가드
        isDead = true;

        if (doomCoroutine != null) StopCoroutine(doomCoroutine); // 사망 시 구동 중인 파멸 코루틴 안전 강제 종료
        if (doomWarningCircle != null) doomWarningCircle.SetActive(false);

        // [오브젝트 풀링 관리] 필드 상에 열려 있던 모든 분신 오브젝트 일괄 비활성화 회수
        foreach (GameObject obj in apostleObjects) if (obj != null) obj.SetActive(false);

        // 잔존하던 공격 장판 오브젝트 일괄 비활성화 회수 및 리스트 비우기
        foreach (GameObject bullet in spawnedBullets) if (bullet != null) bullet.SetActive(false);
        spawnedBullets.Clear();

        // 필드 상에 살아있던 일반 공허 소환 몬스터 오브젝트 일괄 비활성화 회수 및 리스트 비우기
        foreach (GameObject gimmick in spawnedVoidGimmicks) if (gimmick != null) gimmick.SetActive(false);
        spawnedVoidGimmicks.Clear();

        gameObject.SetActive(false); // 모든 처리가 끝난 최종 보스 본체 오브젝트를 풀에 비활성화 반환
    }
}