using System.Collections;
using UnityEngine;

public class DesertGuardianBoss : BossBase
{
    [Header("탄 및 기믹 인덱스")]
    [SerializeField] int sandstormGimmickIndex; // 모래바람 기믹 풀 인덱스
    [SerializeField] int fanBulletIndex;        // 부채꼴 탄 풀 인덱스

    [Header("모래바람 패턴")]
    [SerializeField] float sandstormInterval = 3f; // 8방향 모래바람 소환 주기 (초)

    [Header("워프 설정")]
    [SerializeField] float warpDistance = 3f;   // 플레이어 뒤쪽으로 이동할 거리
    [SerializeField] float verticalOffset = 2f; // 워프 위치 좌우 분산 범위

    [Header("부채꼴 탄막")]
    [SerializeField] int fanBulletCount = 7;  // 발사할 탄 수
    [SerializeField] float fanAngle = 100f;   // 부채꼴 전체 각도

    [Header("야습 - 워프 후 속도 부스트")]
    [SerializeField] float warpSpeedMultiplier = 1.5f;  // 워프 후 이동속도 배율
    [SerializeField] float warpSpeedBoostDuration = 2f; // 속도 부스트 지속 시간 (초)
    [SerializeField] float afterFireWaitTime = 1f;      // 탄막 발사 후 정지 시간 (초)

    [Header("사막의 수호자 - 공격 무효화")]
    [SerializeField, Range(0f, 1f)]
    float blockChance = 0.3f; // 공격 무효화 기본 확률 (유도탄 제외)

    [Header("수호의 마음 - 분노")] // 분노 시 무효화 확률은 blockChance * 2 로 자동 계산
    [SerializeField, Range(0f, 1f)] float enrageThreshold = 0.3f;    // 분노 돌입 체력 비율 (30% 이하)
    [SerializeField] float enrageSpeedMultiplier = 2f; // 분노 시 이동속도 배율

    float sandstormTimer;          // 모래바람 소환 경과 시간
    float waitTimer;               // 탄막 발사 후 정지 경과 시간
    bool isWaiting;                // 탄막 발사 후 정지 상태
    bool isEnraged;                // 분노 상태 여부
    bool wantsToWarp;              // 피격 시 워프 예약 플래그
    bool isHitByHomingFlag;        // 이번 피격이 유도탄인지 여부

    float baseSpeed;               // 기준 이동속도 (배율 복원 기준)
    Coroutine speedBoostCoroutine; // 속도 부스트 중복 방지용 참조

    PoolManager pool;

    protected override void OnEnable()
    {
        base.OnEnable();

        sandstormTimer = 0f;
        waitTimer = 0f;
        isWaiting = false;
        isEnraged = false;
        wantsToWarp = false;
        isHitByHomingFlag = false;
        speedBoostCoroutine = null;

        pool = GameManager.instance.pool;
        baseSpeed = moveSpeed; // BossBase가 data로 moveSpeed를 설정한 직후 저장
    }

    // 외부(충돌 처리 등)에서 유도탄 여부를 주입하는 함수
    public void SetHomingHit(bool isHoming) => isHitByHomingFlag = isHoming;

    // ─── TakeDamage ───────────────────────────────────────────
    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        // 분노 여부에 따라 무효화 확률 결정 (분노 시 2배, 최대 100%)
        float currentBlockChance = isEnraged ? Mathf.Min(blockChance * 2f, 1f) : blockChance;

        // 유도탄이 아닐 때만 무효화 판정
        if (!isHitByHomingFlag && Random.value < currentBlockChance)
        {
            StartCoroutine(FlashInvincible(new Color(0.87f, 0.72f, 0.45f, 1f))); // 모래색 반짝임
            isHitByHomingFlag = false;
            return;
        }

        isHitByHomingFlag = false;
        base.TakeDamage(damage); // 실제 데미지 적용

        if (isDead) return;

        wantsToWarp = true; // 데미지를 받으면 다음 프레임에 워프 실행

        // 체력이 임계값 이하면 분노 돌입 (1회만)
        if (!isEnraged && health / maxHealth <= enrageThreshold)
            EnterEnrage();
    }

    // ─── 수호의 마음 : 분노 ───────────────────────────────────
    void EnterEnrage()
    {
        isEnraged = true;
        moveSpeed = baseSpeed * enrageSpeedMultiplier; // 이동속도 2배
        Debug.Log("[DesertGuardianBoss] 수호의 마음 발동");
    }

    // ─── Update ───────────────────────────────────────────────
    protected override void Update()
    {
        if (target == null) return;

        // 모래바람은 상태와 무관하게 항상 타이머 진행
        sandstormTimer += Time.deltaTime;
        if (sandstormTimer >= sandstormInterval)
        {
            sandstormTimer = 0f;
            SpawnSandstormGimmickCircle();
        }

        // 탄막 발사 후 정지 상태 → 시간이 지나면 해제
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= afterFireWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
            }
            return;
        }

        // 워프 예약 처리 → 워프 + 속도 부스트 + 부채꼴 발사 + 정지
        if (wantsToWarp)
        {
            wantsToWarp = false;
            WarpAwayFromPlayer();

            if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
            speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine());

            FireFanBullets();
            isWaiting = true;
            return;
        }

        base.Update(); // 기본 추적 이동
    }

    // ─── 워프 후 이동속도 1.5배 일시 증가 ────────────────────
    IEnumerator SpeedBoostRoutine()
    {
        // 분노 배율을 기준으로 추가 부스트 적용
        float boosted = (isEnraged ? baseSpeed * enrageSpeedMultiplier : baseSpeed) * warpSpeedMultiplier;
        moveSpeed = boosted;

        yield return new WaitForSeconds(warpSpeedBoostDuration);

        // 부스트 종료 후 분노 여부에 맞게 복원
        moveSpeed = isEnraged ? baseSpeed * enrageSpeedMultiplier : baseSpeed;
        speedBoostCoroutine = null;
    }

    protected override void StartRandomPattern() { } // 기본 패턴 사용 안 함

    // ─── 워프 : 플레이어 반대편(진짜 뒤쪽)으로 즉시 이동 ─────
    void WarpAwayFromPlayer()
    {
        if (target == null) return;

        Vector2 toBoss = (transform.position - target.position).normalized;
        Vector2 backDir = -toBoss;                              // 보스 반대 방향 = 플레이어 뒤
        Vector2 perp = new Vector2(-backDir.y, backDir.x);  // 뒤쪽 수직 벡터 (좌우 분산용)

        float side = Random.Range(-1f, 1f);
        float vertical = Random.Range(-verticalOffset, verticalOffset);

        transform.position = (Vector2)target.position
                             + backDir * warpDistance
                             + perp * side * verticalOffset
                             + perp * vertical * 0.3f;
    }

    // ─── 8방향 모래바람 소환 ──────────────────────────────────
    void SpawnSandstormGimmickCircle()
    {
        if (pool == null) return;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f; // 45도 간격으로 8방향
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject obj = pool.GetBossBullet(sandstormGimmickIndex);
            if (obj == null) continue;

            obj.transform.position = transform.position;
            obj.GetComponent<SandstormGimmick>()?.Init(dir);
        }
    }

    // ─── 부채꼴 탄막 : 플레이어 방향 기준으로 퍼지게 발사 ────
    void FireFanBullets()
    {
        if (pool == null || target == null) return;

        Vector2 dir = (target.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - fanAngle * 0.5f;                              // 부채꼴 시작 각도
        float step = fanBulletCount > 1 ? fanAngle / (fanBulletCount - 1) : 0f; // 탄 간격

        for (int i = 0; i < fanBulletCount; i++)
        {
            float a = startAngle + step * i;
            Vector2 bulletDir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));

            GameObject b = pool.GetBossBullet(fanBulletIndex);
            if (b == null) continue;

            b.transform.position = transform.position;
            b.GetComponent<BossBullet>().Init(bulletDir);
        }
    }
}