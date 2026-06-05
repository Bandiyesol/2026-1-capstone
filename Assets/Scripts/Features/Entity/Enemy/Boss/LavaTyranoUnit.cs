using System.Collections;
using UnityEngine;

/// <summary>
/// 라바 티라노 골렘 - 유닛 컨트롤러 (정밀 체력 분열 + 용암 고정 버프 + 코루틴 유도탄 패턴)
/// </summary>
public class LavaTyranoUnit : BossBase
{
    [Header("코어")]
    public LavaTyranoCore core; // 모든 분열 유닛의 생존과 포탈 생성을 관리하는 부모 코어

    [Header("분열")]
    [SerializeField] int splitUnitPoolIndex;    // 오브젝트 풀에서 꺼낼 용암 티라노 보스의 인덱스 번호
    [SerializeField] float BOSS_MAX_HEALTH;       // [기준점] 분열하기 전 태초 상태(레벨 0) 거대 보스의 최대 체력
    [SerializeField] int splitLevel = 0;        // 현재 객체의 분열 단계 (0: 최초 보스, 1~3: 하위 분열체)
    [SerializeField] int maxSplitLevel = 3;     // 최대 분열 가능 단계 한계선

    [Header("용암 유도탄")]
    [SerializeField] int homingBulletIndex;   // 오브젝트 풀에서 꺼낼 유도 탄막 프리팹 인덱스
    [SerializeField] int baseBulletCount = 8; // 최초(0단계) 보스가 발사할 기본 유도탄 개수
    [SerializeField] float homingFireDelay = 0.15f; // 순차적 탄막 발사 사이의 시간 간격

    // 상태 및 타이머 캐싱 변수
    bool isOnLava;     // 현재 용암(Lava) 타일 위에 올라와 있는지 여부
    float lavaTimer;   // 용암 위에서 1초마다 틱 힐을 계산하기 위한 타이머
    float baseAttackDamage; // 분열 단계별 배정된 순수 기본 공격력 (용암 버프 원상복구용 캐싱)

    // 프로퍼티: 현재 객체의 사망 여부
    public bool IsDead => health <= 0;

    protected override void OnEnable()
    {
        base.OnEnable();

        // 최상위 보스(0단계)일 때만 최초 maxHealth를 태초 기준점 체력으로 낙점
        if (splitLevel == 0)
            BOSS_MAX_HEALTH = maxHealth;

        // 현재 단계의 기본 공격력을 백업하고 버프 관련 타이머 상태 초기화
        baseAttackDamage = attackDamage;
        lavaTimer = 0f;
        isOnLava = false;
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead)
            return;

        CheckSplit();    // 실시간 HP 비율 기준 분열 조건 체크
        CheckLavaBuff(); // 용암 타일 위 지속 힐 체크
    }

    // ==========================================
    // 용암 구역 진입 (공격력 1.5배 고정 버프 최초 1회 적용)
    // ==========================================
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Lava") && !isOnLava)
        {
            isOnLava = true;
            attackDamage = baseAttackDamage * 1.5f; // 현재 단계 기본 공격력의 1.5배로 확실히 고정 (중첩 원천 차단)
        }
    }

    // ==========================================
    // 용암 구역 이탈 (증폭된 공격력 원상복구)
    // ==========================================
    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Lava") && isOnLava)
        {
            isOnLava = false;
            attackDamage = baseAttackDamage; // 용암에서 탈출하면 배정받았던 본래 공격력으로 청정 원상복구
        }
    }

    // ==========================================
    // 실시간 체력 비율별 분열 조건 검사
    // ==========================================
    void CheckSplit()
    {
        // 🎯 [정밀 공식 적용] 태초 보스 최대 체력을 기준으로 각 단계(75%, 50%, 25%) 절대 임계점을 선형 연산
        float splitThreshold = BOSS_MAX_HEALTH * (1f - ((splitLevel + 1) * 0.25f));

        // 계산된 임계점 이하로 체력이 떨어지면 즉시 다음 세대로 세포 분열 가동
        if (health <= splitThreshold)
        {
            Split();
        }
    }

    // ==========================================
    // 핵심 세포 분열 및 스탯 역비례 가속 연산
    // ==========================================
    void Split()
    {
        if (splitLevel >= maxSplitLevel)
            return;

        int nextLevel = splitLevel + 1; // 자식들에게 물려줄 다음 분열 단계 값

        // 1) 자식 최대 체력: 현재 나의 최대 체력에서 태초 보스 체력의 25%씩을 차감해 스케일 축소
        float childMaxHealth = maxHealth - (BOSS_MAX_HEALTH * 0.25f);

        // 2) 자식 공격력: 작아질수록 기본 데미지가 절반(50%)씩 감소
        float childAttackDamage = attackDamage * 0.5f;

        // 3) 자식 이동속도: 체구가 가벼워질수록 이동 속도가 기존 대비 1.5배 상시 빨라짐
        float childMoveSpeed = moveSpeed * 1.5f;

        // 4) 자식 패턴 쿨타임: 공격 주기가 절반으로 가속단축 (최소 한계선 0.3초 제한)
        float childPatternCooldown = Mathf.Max(0.3f, patternCooldown * 0.5f);

        // 5) 자식 크기 외형: 현재 부모 스케일 대비 80% 크기로 미니어처화
        Vector3 childScale = transform.localScale * 0.8f;

        // 1타 2피: 하나의 개체가 파괴되며 동일 형태의 자식 2마리 순차 복제 생성
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = GameManager.instance.pool.GetBoss(splitUnitPoolIndex);

            if (obj == null)
                continue;

            LavaTyranoUnit unit = obj.GetComponent<LavaTyranoUnit>();

            if (unit == null)
                continue;

            // 스폰 직후 겹침으로 인한 물리 충돌 에러 방지용 무작위 외곽 반경 오프셋 배치
            Vector2 offset = Random.insideUnitCircle * 1.5f;
            obj.transform.position = (Vector2)transform.position + offset;

            // 명확히 정제된 스탯 인자를 자식 유닛 초기화 함수(`InitSplitUnit`)로 깔끔하게 전송 및 이관
            unit.InitSplitUnit(
                core,
                nextLevel,
                BOSS_MAX_HEALTH,
                childMaxHealth,
                childAttackDamage,
                childMoveSpeed,
                childPatternCooldown,
                childScale);
        }

        DieUnit(); // 자식 세대를 안전하게 출범시킨 현재 세대 유닛은 무대 뒤로 소멸
    }

    // 패턴 주기가 충족되면 부모 시스템 시퀀스에 의해 트리거 연동 호출
    protected override void StartRandomPattern()
    {
        StartCoroutine(FireHomingRoutine()); // 유도 탄막 연사 코루틴 작동
    }

    // ==========================================
    // 용암 지대 지속 효과 (공격력 제외, 초당 틱 힐만 상시 가동)
    // ==========================================
    void CheckLavaBuff()
    {
        if (!isOnLava)
            return;

        lavaTimer += Time.deltaTime;

        if (lavaTimer >= 1f)
        {
            lavaTimer = 0f;

            // 용암 위에 서 있는 동안 1초 주기로 현재 최대 체력의 3%씩 지속 자가 치유 치유
            health = Mathf.Min(maxHealth, health + maxHealth * 0.03f);
        }
    }

    // ==========================================
    // 플레이어 조준 유도 탄막 사격 코루틴 (딜레이 연사 구조)
    // ==========================================
    IEnumerator FireHomingRoutine()
    {
        isPatternPlaying = true;
        canMove = false; // 탄막을 뿌리는 말뚝 딜레이 타이밍 동안 보스 이동 통제

        if (target == null)
        {
            canMove = true;
            isPatternPlaying = false;
            yield break;
        }

        // 🎯 [기획 공식] 거듭제곱 분모 연산(`Mathf.Pow(2, splitLevel)`)을 적용하여 단계별로 탄막 총 개수가 정확히 2배씩 차감됨
        int bulletCount = Mathf.Max(1, Mathf.RoundToInt(baseBulletCount / Mathf.Pow(2, splitLevel)));

        // 애니메이터 공격 트라이거 발동
        anim?.SetTrigger("Attack");

        // 정해진 탄수만큼 루프를 돌며 순차적 지연 발사 가동
        for (int i = 0; i < bulletCount; i++)
        {
            // 발사하는 프레임 순간의 실시간 플레이어 위치를 향한 방향 벡터 역재조준
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;

            GameObject bullet = GameManager.instance.pool.GetBossBullet(homingBulletIndex);

            if (bullet != null)
            {
                bullet.transform.position = transform.position; // 본체 중심 좌표 스폰
                bullet.GetComponent<BossBullet>()?.Init(dir);   // 타겟 방향 벡터 주입
                bullet.SetActive(true);                         // 사격 개시
                core?.RegisterBullet(bullet);                   // 코어에 있는 탄막 리스트에 생성된 탄막 저장
            }

            // 기획된 연사 간격 매개변수(`homingFireDelay`)만큼 대기 후 다음 탄 루프 진행
            yield return new WaitForSeconds(homingFireDelay);
        }

        // 모든 탄막 발사가 끝난 후 부드러운 모션 처리를 위한 최종 후딜레이 대기
        yield return new WaitForSeconds(0.3f);

        // 행동 봉인 해제 및 패턴 종료 플래그 원복
        canMove = true;
        isPatternPlaying = false;
    }

    // ==========================================
    // 자식 분열체 전용 스탯 셋업 및 초기화 공정
    // ==========================================
    public void InitSplitUnit(
        LavaTyranoCore parentCore,
        int level,
        float bossMaxHealth,
        float newMaxHealth,
        float newAttackDamage,
        float newMoveSpeed,
        float newPatternCooldown,
        Vector3 newScale)
    {
        core = parentCore; // 부모 코어 매니저 상속 연동
        splitLevel = level; // 새 분열 단계 넘버링 주입

        BOSS_MAX_HEALTH = bossMaxHealth; // 태초 보스 최대 체력 기준점 유지 데이터 이관

        // 최대 체력 및 현재 스폰 체력을 계산된 축소 스탯으로 대입
        maxHealth = newMaxHealth;
        health = newMaxHealth;

        // 현재 공격력과 용암 버프 해제용 기본 데미지를 동시에 정밀 동기화 세팅
        attackDamage = newAttackDamage;
        baseAttackDamage = newAttackDamage;

        // 가속화된 기동성 및 패턴 쿨타임 데이터 오버라이드
        moveSpeed = newMoveSpeed;
        patternCooldown = newPatternCooldown;

        // 축소된 외형 크기를 트랜스폼에 즉시 투영
        transform.localScale = newScale;

        // 모든 조율이 끝난 직후 부모 코어 리스트에 실시간 멤버로 등록
        core?.RegisterUnit(this);
    }

    // 외부 피격 이벤트 수신 리스너
    public override void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        base.TakeDamage(damage);

        // 데미지 연산 결과 체력이 완전히 방전되면 소멸 시퀀스 트리거
        if (health <= 0)
            DieUnit();
    }

    // 유닛 완전 소멸 프로세스
    void DieUnit()
    {
        core?.UnregisterUnit(this);  // 소멸하기 직전 부모 코어에 사망 도장을 찍고 명단에서 제외
        gameObject.SetActive(false); // 오브젝트 풀 비활성화 반환
    }
}