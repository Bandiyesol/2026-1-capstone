using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심해 괴수 보스 컨트롤러 (거리별 조류 흡입 + 외부 기절 탄막 조준 + 부하 링크 무적 페이즈 부모 클래스 상속)
/// </summary>
public class DeepSeaMutant : BossBase
{
    [Header("조류")]
    public GameObject currentObject;
    [SerializeField] float currentForce = 7f; // 조류가 플레이어를 당기는 최대 힘
    [SerializeField] float currentRange = 6f; // 조류가 영향을 미치는 최대 반경 거리

    [Header("기절 탄막")]
    [SerializeField] int stunBulletIndex;      // 풀링에서 사용할 기절 탄막 인덱스
    [SerializeField] int bulletCount = 5;       // 패턴 1회당 발사할 탄막 개수
    [SerializeField] float spawnRadius = 4f;    // 플레이어 주변 탄막 스폰 오차 반경
    [SerializeField] float bulletDelay = 0.25f; // 탄막 간 순차 생성 지연 시간

    [Header("부하 소환")]
    [SerializeField] int summonEnemyIndex;     // 풀링에서 사용할 부하 몬스터 인덱스
    [SerializeField] int summonCount = 4;       // 1회당 소환할 부하 수
    [SerializeField] float summonInterval = 4f; // 무적 페이즈 중 부하 소환 반복 주기

    // 페이즈 제어 플래그
    bool invinciblePhase; // 현재 부하 링크 무적 상태인지 여부
    bool phaseTriggered;  // 50% HP 페이즈가 최초 1회 발동되었는지 체크

    // 타이머 변수
    float summonTimer; // 무적 페이즈 중 부하 추가 스폰 주기를 계산할 타이머

    // ==========================================
    // 🔥 메모리 관리 리스트 분리 (몬스터 vs 탄막)
    // ==========================================
    List<GameObject> summons = new List<GameObject>(); // 소환된 부하 몬스터 추적 리스트
    List<GameObject> bullets = new List<GameObject>(); // 필드 내 가동 중인 기절 탄막 추적 리스트

    // 오브젝트 활성화 시 페이즈 및 데이터 리셋
    protected override void OnEnable()
    {
        base.OnEnable();

        invinciblePhase = false;
        phaseTriggered = false;
        summonTimer = 0f;

        // 리스트 완전 초기화
        summons.Clear();
        bullets.Clear();
    }
    private void OnDisable()
    {
        // 리스트 완전 초기화 (보스의 사망과 동시에 몹 소환 방지를 위해)
        summons.Clear();
        bullets.Clear();
    }

    protected override void Update()
    {
        base.Update(); // 부모 클래스의 기본 컴포넌트 및 타이머 업데이트

        if (target == null)
            return;

        ApplyCurrent(); // 상시 기믹: 플레이어를 중심으로 당기는 상시 조류 가동

        // [페이즈 특수 기믹] 무적 상태일 때만 주기적으로 부하 몬스터 추가 소환
        if (invinciblePhase)
        {
            summonTimer += Time.deltaTime;

            if (summonTimer >= summonInterval)
            {
                summonTimer = 0f;
                SummonMinions(); // 주기적 부하 증식
            }
        }
    }

    // ==========================================
    // 상시 조류 흡입 로직
    // ==========================================
    void ApplyCurrent()
    {
        // 타겟에서 컴포넌트 추출 및 기절 상태일 경우 중력 가산 스킵 예외 처리
        Player player = target.GetComponent<Player>();

        if (player == null || player.isStunned)
        {
            currentObject.SetActive(false);
            return;
        }

        currentObject.SetActive(true);

        // 보스와 플레이어 간의 거리 및 방향 벡터 연산
        Vector2 dir = target.position - transform.position;
        float dist = dir.magnitude;

        // 조류 영향권 범위를 벗어나면 연산 제외
        if (dist > currentRange)
            return;

        dir.Normalize(); // 방향 정규화

        // [핵심 공식] 보스에 가까울수록 조류의 힘이 선형적으로 강해짐
        float force = currentForce * (1f - dist / currentRange);

        // 플레이어의 외부 물리 벡터 속성에 누적 가산 (보스 방향으로 당겨짐)
        player.externalVelocity += dir * force;
    }

    // 메인 패턴 타이머 충족 시 호출 (부모 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        StartCoroutine(Pattern_StunShot()); // 기절 탄막 포위 사격 가동
    }

    // ==========================================
    // [패턴 1] 플레이어 주변 포위 기절 탄막 코루틴
    // ==========================================
    IEnumerator Pattern_StunShot()
    {
        isPatternPlaying = true;
        canMove = false; // 패턴 수행 중 보스 이동 차단

        anim.SetTrigger("Attack"); // 공격 애니메이션 가동

        yield return new WaitForSeconds(0.4f); // 선딜레이 대기

        for (int i = 0; i < bulletCount; i++)
        {
            // ⭐ [독특한 기믹] 탄막이 보스가 아닌 '플레이어 주변 외곽(원형 반경)'에서 생성됨
            Vector2 spawnPos =
                (Vector2)target.position +
                Random.insideUnitCircle * spawnRadius;

            // 생성된 외곽 좌표에서 플레이어 중심을 향하는 정규화 방향 연산
            Vector2 dir =
                ((Vector2)target.position - spawnPos).normalized;

            GameObject bullet =
                GameManager.instance.pool.GetBossBullet(stunBulletIndex);

            if (bullet == null)
                continue;

            bullet.transform.position = spawnPos; // 계산된 외곽 좌표에 탄막 배치

            bullet.GetComponent<BossBullet>()?.Init(dir); // 플레이어 중심 방향으로 날아가도록 초기화

            bullet.SetActive(true); // 활성화

            bullets.Add(bullet); // 사망 시 청소용 리스트 등록

            yield return new WaitForSeconds(bulletDelay); // 순차 발사 지연 대기
        }

        yield return new WaitForSeconds(0.4f); // 패턴 종료 후 후딜레이

        canMove = true;
        isPatternPlaying = false;
    }

    // ==========================================
    // 부하 몬스터 소환 로직
    // ==========================================
    void SummonMinions()
    {
        for (int i = 0; i < summonCount; i++)
        {
            GameObject enemy =
                GameManager.instance.pool.GetEnemy(summonEnemyIndex);

            if (enemy == null)
                continue;

            // 보스 중심 원형 범위 내에 오프셋 배치
            Vector2 offset = Random.insideUnitCircle * 2f;

            enemy.transform.position =
                (Vector2)transform.position + offset;

            enemy.SetActive(true);

            summons.Add(enemy); // 실시간 생존 체크 및 정리를 위해 리스트 등록
        }
    }

    // ==========================================
    // 피격 이벤트 오버라이드 (페이즈 트리거)
    // ==========================================
    public override void TakeDamage(float damage)
    {
        // 부하 링크 무적 페이즈 작동 중에는 본체 피해 완전 면역
        if (invinciblePhase)
            return;

        base.TakeDamage(damage); // 일반 상태일 때 실 데미지 연산

        // 체력이 50% 이하로 떨어지면 1회 한정 무적/소환 페이즈 돌입
        if (!phaseTriggered &&
            health <= maxHealth * 0.5f)
        {
            phaseTriggered = true;
            invinciblePhase = true; // 무적 상태 ON
            summonTimer = 0f;       // 증식 타이머 리셋

            SummonMinions(); // 최초 1차 부하 군단 즉시 소환
        }
    }

    // ==========================================
    // 소환수 실시간 생존 연동 기믹
    // ==========================================
    /// <summary>
    /// 소환된 부하가 사망할 때 부하 스크립트 측에서 호출해 줄 역방향 연동 메서드
    /// </summary>
    public void OnSummonDead()
    {
        // 무적 페이즈가 아닐 때 들어온 신호는 예외 차단
        if (!invinciblePhase)
            return;

        // [핵심 기믹] 부하가 죽을 때마다 보스의 체력이 최대 체력의 1%씩 강제로 깎임 (자해)
        health -= maxHealth * 0.01f;

        // 자해로 인해 체력이 0 이하가 되면 즉시 사망 처리 후 탈출
        if (health <= 0f)
        {
            Dead();
            return;
        }

        // 맵에 살아있는 부하가 단 한 마리도 없다면 보스의 무적 상태 강제 해제
        if (!HasAliveSummons())
        {
            invinciblePhase = false;
        }
    }

    // 소환수 리스트 정밀 검사
    bool HasAliveSummons()
    {
        summons.RemoveAll(x => x == null); // 파괴된 객체 1차 정리

        // 남아있는 객체 중 계층 구조상 활성화(activeSelf)되어 살아있는 몹이 있는지 루프 검사
        foreach (var obj in summons)
        {
            if (obj != null && obj.activeSelf)
                return true; // 1마리라도 살아있다면 true 반환
        }

        return false; // 전멸 상태 시 false 반환
    }

    // ==========================================
    // 사망 처리 오버라이드
    // ==========================================
    protected override void Dead()
    {
        ClearAll();  // 필드 내 잔존 탄막 및 남아있는 모든 부하 강제 증발
        base.Dead(); // 부모 클래스의 사망 시퀀스(보상 및 비활성화) 작동
    }

    // 필드 내 모든 생성 오브젝트 안전 풀링 반환 처리
    void ClearAll()
    {
        // 1. 잔존 부하 일괄 처리
        for (int i = 0; i < summons.Count; i++)
        {
            if (summons[i] != null)
                summons[i].SetActive(false);
        }

        // 2. 잔존 외곽 포위 탄막 일괄 처리
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i] != null)
                bullets[i].SetActive(false);
        }

        // 3. 리스트 데이터 초기화
        summons.Clear();
        bullets.Clear();
    }
}