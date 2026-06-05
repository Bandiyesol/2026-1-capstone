using System.Collections;
using UnityEngine;

public class LavaEarthDragon : BossBase
{
    [Header("몬스터 소환 설정")]
    // PoolManager enemyPrefabs 인덱스
    [SerializeField] int summonMonsterIndex;
    // 기본 소환 개수
    [SerializeField] int summonCount = 2;
    // 이동 중 소환 확률
    [SerializeField] float summonChance = 0.02f;

    [Header("나선 탄막 설정")]
    // PoolManager bossBulletPrefabs 인덱스
    [SerializeField] int bulletPoolIndex;
    // 한 바퀴당 발사 탄 수
    [SerializeField] int spiralBulletCount = 24;
    // 반복 발사 횟수
    [SerializeField] int spiralRepeatCount = 10;
    // 발사 간격
    [SerializeField] float spiralShotInterval = 0.12f;
    // 반복마다 회전하는 각도
    [SerializeField] float spiralRotateOffset = 18f;
    // 패턴 종료 후 대기
    [SerializeField] float spiralEndDelay = 1f;

    [Header("용암 타일")]
    // 0 = 기본
    // 1 = 75%
    // 2 = 50%
    // 3 = 25%
    [SerializeField] GameObject[] lavaTiles;

    // 현재 용암 단계
    int currentLavaPhase = 0;

    // 소환 배율
    int summonMultiplier = 1;

    // 체력 구간 체크
    bool phase75Triggered;
    bool phase50Triggered;
    bool phase25Triggered;

    protected override void OnEnable()
    {
        // 부모 초기화
        base.OnEnable();

        // 혹시 남아있는 코루틴 제거
        StopAllCoroutines();

        // 이동 복구
        canMove = true;

        // 패턴 종료 상태
        isPatternPlaying = false;

        // 용암 단계 초기화
        currentLavaPhase = 0;

        // 소환 배율 초기화
        summonMultiplier = 1;

        // 체력 단계 초기화
        phase75Triggered = false;
        phase50Triggered = false;
        phase25Triggered = false;

        // 기본 용암 타일 활성화
        ActivateLavaTile(0);

        // 애니메이터 상태 초기화
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    void OnDisable()
    {
        // 풀 반환 시 코루틴 정리
        StopAllCoroutines();

        // 상태 복구
        canMove = true;
        isPatternPlaying = false;
    }

    protected override void FixedUpdate()
    {
        // 부모 이동 처리
        base.FixedUpdate();

        // 이동 중 소환 체크
        TrySummon();
    }

    protected override void StartRandomPattern()
    {
        StartCoroutine(Pattern_SpiralBurst());
    }

    IEnumerator Pattern_SpiralBurst()
    {
        // 패턴 시작
        isPatternPlaying = true;

        // 이동 정지
        canMove = false;

        // 공격 애니메이션
        anim.SetTrigger("Attack");

        // 예고 시간
        yield return new WaitForSeconds(0.5f);

        // 플레이어 방향
        Vector2 targetDir =
            ((Vector2)target.position -
            (Vector2)transform.position).normalized;

        // 플레이어 기준 각도
        float baseAngle =
            Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 반복 발사
        for (int repeat = 0; repeat < spiralRepeatCount; repeat++)
        {
            // 회전 오프셋
            float rotateOffset =
                repeat * spiralRotateOffset;

            // 원형 탄막 생성
            for (int i = 0; i < spiralBulletCount; i++)
            {
                float angle =
                    baseAngle +
                    rotateOffset +
                    (360f / spiralBulletCount) * i;

                Vector2 dir =
                    new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad)
                    ).normalized;

                // PoolManager에서 보스 탄 가져오기
                GameObject bullet =
                    GameManager.instance.pool.GetBossBullet(
                        bulletPoolIndex
                    );

                if (bullet != null)
                {
                    bullet.transform.position =
                        transform.position;

                    bullet.GetComponent<BossBullet>()
                        ?.Init(dir);
                }
            }

            // 반복마다 공격 모션
            anim.SetTrigger("Attack");

            yield return new WaitForSeconds(
                spiralShotInterval
            );
        }

        // 패턴 종료 대기
        yield return new WaitForSeconds(
            spiralEndDelay
        );

        // 이동 재개
        canMove = true;

        // 패턴 종료
        isPatternPlaying = false;
    }

    void TrySummon()
    {
        // 이동 중만 소환
        if (!canMove)
            return;

        // 확률 체크
        if (Random.value > summonChance)
            return;

        // 현재 소환 개수
        int currentSummonCount =
            summonCount * summonMultiplier;

        for (int i = 0; i < currentSummonCount; i++)
        {
            // PoolManager에서 적 가져오기
            GameObject monster =
                GameManager.instance.pool.GetEnemy(
                    summonMonsterIndex
                );

            if (monster != null)
            {
                // 보스 주변 랜덤 위치
                Vector2 offset =
                    Random.insideUnitCircle * 2f;

                monster.transform.position =
                    (Vector2)transform.position + offset;
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        // 부모 데미지 처리
        base.TakeDamage(damage);

        float hpPercent = health / maxHealth;

        // 25%
        if (!phase25Triggered &&
            hpPercent <= 0.25f)
        {
            ActivateLavaTile(3);

            summonMultiplier = 8;

            phase25Triggered = true;
        }

        // 50%
        else if (!phase50Triggered &&
            hpPercent <= 0.5f)
        {
            ActivateLavaTile(2);

            summonMultiplier = 4;

            phase50Triggered = true;
        }

        // 75%
        else if (!phase75Triggered &&
            hpPercent <= 0.75f)
        {
            ActivateLavaTile(1);

            summonMultiplier = 2;

            phase75Triggered = true;
        }
    }

    void ActivateLavaTile(int index)
    {
        // 전부 비활성화
        for (int i = 0; i < lavaTiles.Length; i++)
        {
            if (lavaTiles[i] != null)
                lavaTiles[i].SetActive(false);
        }

        // 해당 단계 활성화
        if (index < lavaTiles.Length &&
            lavaTiles[index] != null)
        {
            lavaTiles[index].SetActive(true);

            currentLavaPhase = index;
        }
    }
}