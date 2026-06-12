using UnityEngine;
using System.Collections;

/// <summary>
/// 보스 몬스터: 심연의 포식자 (AbyssalPredator)
/// 부모 클래스(BossBase)를 상속받아 무적 타임과 패턴 타임을 실시간으로 스왑하고,
/// 특정 체력 이하에서 패턴이 급격히 강화되는 광폭화 시스템을 제어하는 클래스입니다.
/// </summary>
public class AbyssalPredator : BossBase
{
    [Header("기본 설정")]
    [Tooltip("현재 보스의 무적 상태 여부 (무적 상태일 때 피격 시 데미지를 체력으로 흡수)")]
    public bool isInvincible = true;
    [Tooltip("무적 상태에서 흡수하는 데미지를 체력으로 치환할 때 적용할 회복 배율")]
    public float healMultiplier = 1f;
    [Tooltip("무적 상태에서 피격되어 체력을 회복할 때 순간적으로 피드백할 점멸 색상")]
    public Color healColor = Color.green;
    [Tooltip("체력 30% 이하 광폭화 상태 돌입 시 보스 본체에 상시 적용될 색상")]
    public Color enragedColor =
        new Color(1f, 0.5f, 0.5f);

    // 원본 스프라이트의 초기 색상을 복원용으로 보관하는 변수
    private Color originalColor;

    // 현재 광폭화(Enrage) 상태가 활성화되었는지 추적하는 플래그
    private bool enraged = false;

    [Header("탄 인덱스")]
    [Tooltip("유도탄 프리팹의 오브젝트 풀 인덱스")]
    public int homingBulletIndex = 0;
    [Tooltip("부채꼴 방사탄 프리팹의 오브젝트 풀 인덱스")]
    public int spreadBulletIndex = 1;

    [Header("유도탄 패턴")]
    [Tooltip("유도탄 패턴 1회당 총 발사할 탄환 개수")]
    public int homingShotCount = 5;
    [Tooltip("탄환과 탄환 사이의 순차적 발사 시간 간격")]
    public float homingShotInterval = 0.3f;
    [Tooltip("유도탄 패턴이 완전히 종료된 후 무적/이동 상태로 복귀하기 전 대기 딜레이")]
    public float homingEndDelay = 1.5f;

    [Header("부채꼴 패턴")]
    [Tooltip("텔레포트 후 부채꼴 탄막을 방사하는 전체 사이클의 총 반복 횟수")]
    public int spreadRepeatCount = 3;
    [Tooltip("1회 텔레포트 시 제자리에서 연속으로 탄막을 뿜어내는 점사(Burst) 횟수")]
    public int spreadBurstCount = 3;
    [Tooltip("부채꼴 패턴의 중심이 되는 기준 탄환 개수")]
    public int spreadBaseBulletCount = 9;
    [Tooltip("연속 점사(Burst) 시 각 점사 간의 내부 시간 간격")]
    public float spreadInnerInterval = 0.2f;
    [Tooltip("다음 사이클(텔레포트 이동)로 넘어가기 전 제자리 대기 시간 간격")]
    public float spreadRepeatInterval = 1f;
    [Tooltip("부채꼴 패턴 시퀀스가 완전히 끝난 후 복귀하기 전 최종 대기 딜레이")]
    public float spreadEndDelay = 1.5f;
    [Tooltip("플레이어 위치를 기준으로 보스가 텔레포트하여 안착할 최소/최대 반경 거리")]
    public float teleportDistance = 4f;
    [Tooltip("부채꼴 탄막이 펼쳐질 전체 사격 각도 폭 (예: 90도 범위 내 방사)")]
    public float spreadAngle = 90f;

    // GameManager의 PoolManager에 빠르게 접근하기 위한 단축 프로퍼티
    private PoolManager pool => GameManager.instance.pool;

    protected override void Awake()
    {
        // 부모 클래스(BossBase)의 기초 컴포넌트 캐싱 및 초기화 실행
        base.Awake();

        // 부모 클래스가 가지고 있는 SpriteRenderer(spriter)의 기본 색상을 원본으로 백업
        originalColor = spriter.color;
    }

    protected override void Start()
    {
        // 부모 클래스의 스타트 로직(플레이어 타겟 설정 등) 작동
        base.Start();

        // 보스 고유의 무한 AI 패턴 루프 가동
        StartCoroutine(PatternRoutine());
    }

    /// <summary>
    /// 외부 타격으로부터 데미지를 연산하는 피격 오버라이드 메서드
    /// </summary>
    public override void TakeDamage(float damage)
    {
        // [특수 기믹] 현재 보스가 무적(이동/대기 페이즈) 상태라면 모든 데미지를 흡수하여 체력을 회복
        if (isInvincible)
        {
            // 최대 체력을 초과하지 않는 선에서 (데미지 * 배율)만큼 체력 증가 연산
            health = Mathf.Min(
                maxHealth,
                health + damage * healMultiplier
            );

            // 광폭화 상태가 아닐 때만 초록색 회복 점멸 이펙트 연출 (광폭화 상시 붉은색 유지 보호)
            if (!enraged)
                StartCoroutine(FlashEffect(healColor));

            return; // 회복 처리 후 실제 피격 데미지 연산은 전면 차단
        }

        // [취약 상태] 무적이 풀린 공격 패턴 페이즈일 때는 정상적으로 부모의 데미지 피격 로직 수행
        base.TakeDamage(damage);

        // [광폭화 조건 검사] 아직 광폭화 상태가 아니면서, 현재 체력이 최대치의 30% 이하로 떨어졌는가?
        if (!enraged &&
            health <= maxHealth * 0.3f)
        {
            Enrage(); // 보스 광폭화 각성 기믹 발동
        }
    }

    /// <summary>
    /// 플레이어의 무기(Is Trigger) 충돌을 감지하여 처리하는 트리거 메서드 (방법 2 적용: 단독 실행)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 보스가 무적(체력 흡수) 상태일 때만 투사체 관통 차단 로직 작동
        if (!isInvincible)
            return;

        // 플레이어 무기(Motion) 컴포넌트 체크
        Motion weaponMotion = collision.GetComponent<Motion>();
        if (weaponMotion != null && weaponMotion.instance != null && weaponMotion.instance.info != null)
        {
            string weaponType = weaponMotion.instance.info.type;

            // 보스가 무적이어도 파괴(소멸)되지 않고 유지되는 예외 무기 리스트
            bool isExempt = weaponType == "Sword" || // 검
                            weaponType == "Hammer" || // 망치
                            weaponType == "Scythe" || // 낫
                            weaponType == "Orb" || // 오브 (장판 유지)
                            weaponType == "Grimoire";  // 마도서

            // 예외 무기가 아니라면(일반 화살, 총알 등 원거리 투사체)
            // 보스 무적 방벽에 부딪히는 순간 관통하지 못하도록 즉시 투사체를 소멸(비활성화)시킵니다.
            if (!isExempt)
            {
                collision.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 보스의 체력이 30% 이하일 때 단 1회 발동되어 모든 스펙 및 탄막 밀도를 2배 수준으로 영구 강화하는 메서드
    /// </summary>
    void Enrage()
    {
        enraged = true; // 중복 실행 방지 플래그 마킹

        // 보스의 외형 색상을 경고성 붉은빛(enragedColor)으로 고정 전환
        spriter.color = enragedColor;

        // 보스 자체 스펙 증가 (공격력 및 이동 속도 2배)
        attackDamage *= 2f;
        moveSpeed *= 2f;

        // [유도탄 패턴 성능 강화] 발사 수량 2배 증가 / 발사 및 후속 딜레이는 절반으로 단축
        homingShotCount *= 2;
        homingShotInterval *= 0.5f;
        homingEndDelay *= 0.5f;

        // [부채꼴 패턴 성능 강화] 반복/연속 발사수 및 탄막 조밀도 2배 증가 / 모든 대기 시간 및 간격은 절반으로 단축
        spreadRepeatCount *= 2;
        spreadBurstCount *= 2;
        spreadBaseBulletCount *= 2;
        spreadInnerInterval *= 0.5f;
        spreadRepeatInterval *= 0.5f;
        spreadEndDelay *= 0.5f;
    }

    /// <summary>
    /// 피격 및 회복 시 보스 몸체를 순간적으로 지정 색상으로 반짝이게 만들어 시각 피드백을 주는 코루틴
    /// </summary>
    IEnumerator FlashEffect(Color color)
    {
        // 변화를 주기 직전의 현재 스프라이트 색상 보관
        Color prevColor = spriter.color;

        // 타겟 색상(초록색 등)으로 변경
        spriter.color = color;

        // 0.15초 동안 유지하며 연출
        yield return new WaitForSeconds(0.15f);

        // 연출 종료 시점에 광폭화 상태라면 붉은색을 강제 유지하고, 일반 상태라면 직전 색상으로 안전 복구
        if (enraged)
            spriter.color = enragedColor;
        else
            spriter.color = prevColor;
    }

    /// <summary>
    /// [메인 AI 루프] 보스의 생존 기간 동안 무한히 순환하며 이동과 무작위 패턴 공격을 번갈아 제어하는 메인 코루틴
    /// </summary>
    IEnumerator PatternRoutine()
    {
        while (true)
        {
            // ─── [페이즈 1: 랜덤 추적 및 충전 이동] ───
            isInvincible = true; // 이동 중에는 무적 상태 활성화 (유저가 공격 시 보스 체력 회복 기믹)
            canMove = true;      // 부모 AI 추적 이동 로직 가동

            // 3초에서 5초 사이의 무작위 시간 동안 필드를 배회하며 유저를 압박
            yield return new WaitForSeconds(
                Random.Range(3f, 5f)
            );

            // ─── [페이즈 2: 약점 노출 및 공격 시동] ───
            isInvincible = false; // 공격 패턴 돌입 시 무적 전면 해제 (이 타이밍이 유저의 실질적인 딜타임)
            canMove = false;      // 공격 집중을 위해 자리에 고정 (이동 중지)

            // 50%의 무작위 확률 분기를 통해 두 가지 공격 패턴 중 하나를 선택 실행
            if (Random.value > 0.5f)
                yield return StartCoroutine(
                    Pattern_HomingMissiles() // 1번 패턴: 유도탄 순차 점사 기동
                );
            else
                yield return StartCoroutine(
                    Pattern_TeleportSpread() // 2번 패턴: 플레이어 기습 텔레포트 및 부채꼴 폭사 기동
                );

            // ─── [페이즈 3: 패턴 종료 후 리셋] ───
            // 루프의 처음으로 돌아가기 전 상태 플래그를 복구하여 안정적인 사이클 유도
            canMove = true;
            isInvincible = true;
        }
    }

    /// <summary>
    /// [공격 패턴 1] 유저를 향해 지속적으로 각도를 갱신하며 날아가는 유도성 투사체를 순차 발사하는 패턴
    /// </summary>
    IEnumerator Pattern_HomingMissiles()
    {
        // 설정된 총 발사 횟수만큼 탄환을 한 발씩 텀을 두고 소환
        for (int i = 0; i < homingShotCount; i++)
        {
            // 오브젝트 풀에서 유도탄 인스턴스 인출
            GameObject bullet = pool.GetBossBullet(homingBulletIndex);

            if (bullet != null)
            {
                // 보스 전용 공격 애니메이션 트리거 가동
                anim.SetTrigger("Attack");

                // 소환된 탄환의 위치를 보스 본체의 현재 월드 위치로 동기화
                bullet.transform.position = transform.position;

                // 실시간으로 변하는 유저(target)의 위치를 추적하여 발사 기준 방향 벡터 연산 (.normalized로 크기 1 유지)
                Vector2 dir = (target.position - transform.position).normalized;

                // 탄환 스크립트(BossBullet)의 Init 메서드를 호출하여 방향 주입 및 이동 알고리즘 활성화
                bullet.GetComponent<BossBullet>()?.Init(dir);
            }

            // 탄환 간의 발사 주기 간격(기본 0.3초 / 광폭화 시 0.15초)만큼 텀을 대기
            yield return new WaitForSeconds(
                homingShotInterval
            );
        }

        // 모든 사격이 종료된 후 후딜레이 대기 (유저가 후속 딜을 넣을 수 있는 안전장치 타임)
        yield return new WaitForSeconds(
            homingEndDelay
        );
    }

    /// <summary>
    /// [공격 패턴 2] 플레이어 주변으로 순간이동한 후, 중심축을 기준으로 부채꼴 형태의 사선 탄막을 사방으로 펼치는 기습 패턴
    /// </summary>
    IEnumerator Pattern_TeleportSpread()
    {
        // 설정된 사이클 횟수(기본 3회 / 광폭화 시 6회)만큼 텔레포트 공격 연쇄 반복
        for (int cycle = 0; cycle < spreadRepeatCount; cycle++)
        {
            // 플레이어가 중도 사망하여 타겟이 소멸했을 경우 예외 차단 및 코루틴 즉시 탈출
            if (target == null)
                yield break;

            // [기습 텔레포트 매커니즘] 플레이어의 현재 위치 좌표에 임의의 원형 단위 벡터를 연산하고 
            // 정해진 오프셋 거리(4f)를 곱해 플레이어 주변 무작위 원형 궤도 상의 좌표로 순간이동 실행
            transform.position =
                (Vector2)target.position +
                Random.insideUnitCircle.normalized *
                teleportDistance;

            // 안착 직후 사격 애니메이션 트리거 발동
            anim.SetTrigger("Attack");

            // 지정된 연속 버스트 사격 횟수만큼 자리에 멈춰 서서 탄막을 투사
            for (int step = 0; step < spreadBurstCount; step++)
            {
                // [시각적 교차 효과 적용] 현재 점사 스텝(step)의 홀짝 여부에 따라 탄환 개수를 N개와 N-1개로 번갈아 설정하여
                // 탄막의 각도가 엇갈리도록 배치 (사이에 공간이 교차하여 배치되는 시각 이펙트 유도)
                int bulletCount = step % 2 == 0 ? spreadBaseBulletCount : spreadBaseBulletCount - 1;

                // 계산의 왜곡 및 분모가 0이 되는 현상을 막기 위해 최소 탄 수량을 2개로 보정 강제 제한
                bulletCount = Mathf.Max(2, bulletCount);

                // 부채꼴 좌측 끝단에서 시작할 상대 각도 연산 (전체 각도의 절반만큼 마이너스 방향으로 후퇴)
                float startAngle = -spreadAngle * 0.5f;

                // 탄환과 탄환 사이를 균등 분할 채우기 위한 세부 각도 한 스텝 단위 연산 공식
                float angleStep = spreadAngle / (bulletCount - 1);

                // 이번 점사 스텝에 배정된 탄 수량만큼 루프를 돌며 방사형 삼각함수 연산 시작
                for (int j = 0; j < bulletCount; j++)
                {
                    // 오브젝트 풀에서 방사형 부채꼴 탄환 인스턴스 인출
                    GameObject bullet = pool.GetBossBullet(spreadBulletIndex);

                    if (bullet != null)
                    {
                        // 텔레포트하여 안착한 보스 현재 좌표에 탄환 스폰 배치
                        bullet.transform.position = transform.position;

                        // 보스 본체 위치에서 플레이어 위치를 바라보는 메인 조준선 벡터 계산
                        var currentPos = (Vector2)transform.position;
                        Vector2 targetDir = ((Vector2)target.position - currentPos).normalized;

                        // 아크탄젠트(Atan2) 공식을 사용해 2D 벡터를 절대 각도(도 단위, RadDeg) 값으로 정밀 역산
                        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

                        // [최종 각도 공식 생성] 플레이어를 바라보는 메인 기준각 + 좌측 끝 시작 오프셋 각도 + 순차 스텝각(j) 적용
                        float angle = baseAngle + startAngle + angleStep * j;

                        // 연산된 최종 도(Degree) 단위를 다시 호도법(Radian)으로 감싸 삼각함수(Cos, Sin)에 대입하여 최종 발사 2D 방향 벡터 추출
                        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                            Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

                        // 추출된 사선 방향 벡터를 탄환 스크립트에 밀어 넣어 이동 물리 엔진 가동
                        bullet.GetComponent<BossBullet>()?.Init(dir);
                    }
                }

                // 점사와 점사 사이의 아주 짧은 내부 숨고르기 대기 시간 적용
                yield return new WaitForSeconds(spreadInnerInterval);
            }

            // 다음번 기습 텔레포트 구역으로 넘어가기 전의 정비 대기 타임 적용
            yield return new WaitForSeconds(spreadRepeatInterval);
        }

        // 전체 텔레포트 연쇄 사이클이 완료된 후 페이즈 복귀 직전의 최종 마무리 딜레이 대기
        yield return new WaitForSeconds(spreadEndDelay);
    }
}