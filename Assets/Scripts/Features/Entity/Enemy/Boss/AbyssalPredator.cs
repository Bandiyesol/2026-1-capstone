using UnityEngine;
using System.Collections;

// 심연의 포식자 보스
public class AbyssalPredator : BossBase
{
    [Header("기본 설정")]
    public bool isInvincible = true;              // 무적 여부
    public float healMultiplier = 1f;             // 회복 배율
    public Color healColor = Color.green;         // 회복 색
    public Color enragedColor =                 // 광폭화 색
        new Color(1f, 0.5f, 0.5f);

    // 원래 색 저장
    private Color originalColor;

    // 광폭화 여부
    private bool enraged = false;

    [Header("탄 인덱스")]
    public int homingBulletIndex = 0;             // 유도탄
    public int spreadBulletIndex = 1;             // 부채꼴탄

    [Header("유도탄 패턴")]
    public int homingShotCount = 5;               // 발사 횟수
    public float homingShotInterval = 0.3f;       // 발사 간격
    public float homingEndDelay = 1.5f;           // 종료 대기

    [Header("부채꼴 패턴")]
    public int spreadRepeatCount = 3;             // 반복 횟수
    public int spreadBurstCount = 3;              // 연속 발사
    public int spreadBaseBulletCount = 9;         // 기본 탄 수
    public float spreadInnerInterval = 0.2f;      // 내부 간격
    public float spreadRepeatInterval = 1f;       // 반복 간격
    public float spreadEndDelay = 1.5f;           // 종료 대기
    public float teleportDistance = 4f;           // 텔포 거리
    public float spreadAngle = 90f;               // 부채꼴 각도

    // 풀 접근
    private PoolManager pool => GameManager.instance.pool;

    protected override void Awake()
    {
        // 부모 초기화
        base.Awake();

        // 기본 색 저장
        originalColor = spriter.color;
    }

    protected override void Start()
    {
        // 부모 시작
        base.Start();

        // 패턴 시작
        StartCoroutine(PatternRoutine());
    }

    public override void TakeDamage(float damage)
    {
        // 무적이면 회복
        if (isInvincible)
        {
            // 체력 회복
            health = Mathf.Min(
                maxHealth,
                health + damage * healMultiplier
            );

            // 광폭화 아닐 때만 회복 점멸
            if (!enraged)
                StartCoroutine(FlashEffect(healColor));

            return;
        }

        // 일반 피해
        base.TakeDamage(damage);

        // 30% 이하 광폭화
        if (!enraged &&
            health <= maxHealth * 0.3f)
        {
            Enrage();
        }
    }

    // 광폭화
    void Enrage()
    {
        // 중복 방지
        enraged = true;

        // 색 변경
        spriter.color = enragedColor;

        // 공격력 2배
        attackDamage *= 2f;

        // 이동속도 2배
        moveSpeed *= 2f;

        // 유도탄 강화
        homingShotCount *= 2;
        homingShotInterval *= 0.5f;
        homingEndDelay *= 0.5f;

        // 부채꼴 강화
        spreadRepeatCount *= 2;
        spreadBurstCount *= 2;
        spreadBaseBulletCount *= 2;
        spreadInnerInterval *= 0.5f;
        spreadRepeatInterval *= 0.5f;
        spreadEndDelay *= 0.5f;
    }

    // 회복 점멸
    IEnumerator FlashEffect(Color color)
    {
        // 현재 색 저장
        Color prevColor = spriter.color;

        // 색 변경
        spriter.color = color;

        // 잠깐 유지
        yield return new WaitForSeconds(0.15f);

        // 광폭화면 빨강 유지
        if (enraged)
            spriter.color = enragedColor;
        else
            spriter.color = prevColor;
    }

    // 패턴 루프
    IEnumerator PatternRoutine()
    {
        while (true)
        {
            // 이동 상태
            isInvincible = true;
            canMove = true;

            // 랜덤 이동
            yield return new WaitForSeconds(
                Random.Range(3f, 5f)
            );

            // 공격 준비
            isInvincible = false;
            canMove = false;

            // 랜덤 패턴
            if (Random.value > 0.5f)
                yield return StartCoroutine(
                    Pattern_HomingMissiles()
                );
            else
                yield return StartCoroutine(
                    Pattern_TeleportSpread()
                );

            // 다시 이동
            canMove = true;
            isInvincible = true;
        }
    }

    // 유도탄 패턴
    IEnumerator Pattern_HomingMissiles()
    {

        // 반복 발사
        for (int i = 0; i < homingShotCount; i++)
        {
            // 탄 생성
            GameObject bullet = pool.GetBossBullet(homingBulletIndex);

            // 생성 성공
            if (bullet != null)
            {
                // 공격 애니메이션
                anim.SetTrigger("Attack");

                // 현재 위치에서 생성
                bullet.transform.position = transform.position;

                // 플레이어 방향 계산
                Vector2 dir = (target.position - transform.position).normalized;

                // 탄 초기화 (이동 + 충돌 활성화)
                bullet.GetComponent<BossBullet>()?.Init(dir);
            }

            // 다음 발사 대기
            yield return new WaitForSeconds(
                homingShotInterval
            );
        }

        // 종료 대기
        yield return new WaitForSeconds(
            homingEndDelay
        );
    }

    // 텔포 부채꼴 패턴
    IEnumerator Pattern_TeleportSpread()
    {
        // 반복
        for (int cycle = 0; cycle < spreadRepeatCount; cycle++)
        {
            // 타겟 없으면 종료
            if (target == null)
                yield break;

            // 플레이어 주변 텔포
            transform.position =
                (Vector2)target.position +
                Random.insideUnitCircle.normalized *
                teleportDistance;

            // 공격 애니메이션
            anim.SetTrigger("Attack");

            // 연속 발사
            for (int step = 0; step < spreadBurstCount; step++)
            {
                // 탄 수 교차
                int bulletCount = step % 2 == 0 ? spreadBaseBulletCount : spreadBaseBulletCount - 1;

                // 최소 보정
                bulletCount = Mathf.Max(2, bulletCount);

                // 시작 각도
                float startAngle = -spreadAngle * 0.5f;

                // 각도 간격
                float angleStep = spreadAngle / (bulletCount - 1);

                // 탄 생성
                for (int j = 0; j < bulletCount; j++)
                {
                    // 탄 가져오기
                    GameObject bullet = pool.GetBossBullet(spreadBulletIndex);

                    // 생성 성공
                    if (bullet != null)
                    {
                        // 보스 위치에 생성
                        bullet.transform.position = transform.position;

                        // 플레이어 방향 계산
                        Vector2 targetDir = ((Vector2)target.position - (Vector2)transform.position).normalized;

                        // 플레이어를 향한 기준 각도
                        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

                        // 플레이어 방향 기준으로 부채꼴 각도 계산
                        float angle = baseAngle + startAngle + angleStep * j;

                        // 최종 발사 방향 생성
                        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                            Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

                        // 탄 초기화 (이동 + 충돌 활성화)
                        bullet.GetComponent<BossBullet>()?.Init(dir);
                    }
                }

                // 내부 대기
                yield return new WaitForSeconds(spreadInnerInterval);
            }

            // 반복 대기
            yield return new WaitForSeconds(spreadRepeatInterval);
        }

        // 종료 대기
        yield return new WaitForSeconds(spreadEndDelay);
    }
}