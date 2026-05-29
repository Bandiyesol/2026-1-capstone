using System.Collections;
using UnityEngine;

public class LavaEarthDragon : BossBase
{
    [Header("몬스터 소환 설정")]
    // PoolManager에서 가져올 몬스터 인덱스
    [SerializeField] int summonMonsterIndex;
    // 기본 소환 개수
    [SerializeField] int summonCount = 2;
    // 이동 중 소환 확률
    [SerializeField] float summonChance = 0.02f;

    [Header("나선 탄막 설정")]
    // PoolManager 탄막 인덱스
    [SerializeField] int bulletPoolIndex;
    // 원형 1회 발사당 탄 개수
    [SerializeField] int spiralBulletCount = 24;
    // 연속 발사 횟수
    [SerializeField] int spiralRepeatCount = 10;
    // 발사 간 간격
    [SerializeField] float spiralShotInterval = 0.12f;
    // 반복 발사마다 회전 각도
    [SerializeField] float spiralRotateOffset = 18f;
    // 패턴 종료 후 딜레이
    [SerializeField] float spiralEndDelay = 1f;

    [Header("용암 타일")]
    // 0 = 기본
    // 1 = 75%
    // 2 = 50%
    // 3 = 25%
    [SerializeField] GameObject[] lavaTiles;
    // 현재 용암 단계
    int currentLavaPhase = 0;
    // 현재 소환 배율
    int summonMultiplier = 1;

    protected override void OnEnable()
    {
        // 부모 초기화
        base.OnEnable();

        // 첫 번째 용암 타일 활성
        ActivateLavaTile(0);

        // 소환 배율 초기화
        summonMultiplier = 1;
    }

    protected override void FixedUpdate()
    {
        // 부모 이동 처리
        base.FixedUpdate();

        // 이동 중 몬스터 소환 시도
        TrySummon();
    }

    protected override void StartRandomPattern()
    {
        // 나선 탄막 실행
        StartCoroutine(Pattern_SpiralBurst());
    }

    IEnumerator Pattern_SpiralBurst()
    {
        // 패턴 시작
        isPatternPlaying = true;

        // 이동 정지
        canMove = false;

        // 공격 애니메이션 실행
        anim.SetTrigger("Attack");

        // 잠깐 멈춰 예고
        yield return new WaitForSeconds(0.5f);

        // 플레이어 방향 계산
        Vector2 targetDir = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // 플레이어를 향한 기준 각도
        float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 연속 발사
        for (int repeat = 0; repeat < spiralRepeatCount; repeat++)
        {
            // 현재 회전 오프셋
            float rotateOffset = repeat * spiralRotateOffset;

            // 원형 탄막 생성
            for (int i = 0; i < spiralBulletCount; i++)
            {
                // 현재 탄 각도 계산
                float angle = baseAngle + rotateOffset + (360f / spiralBulletCount) * i;

                // 방향 벡터 생성
                Vector2 dir =
                    new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad)
                    ).normalized;

                // 풀에서 탄 가져오기
                GameObject bullet = GameManager.instance.pool.GetBossBullet(bulletPoolIndex);

                if (bullet != null)
                {
                    // 보스 위치에서 생성
                    bullet.transform.position = transform.position;

                    // 방향 초기화
                    bullet.GetComponent<BossBullet>()?.Init(dir);
                }
            }

            // 발사마다 공격 애니메이션
            anim.SetTrigger("Attack");

            // 다음 발사까지 대기
            yield return new WaitForSeconds(spiralShotInterval);
        }

        // 종료 후 딜레이
        yield return new WaitForSeconds(spiralEndDelay);

        // 이동 재개
        canMove = true;

        // 패턴 종료
        isPatternPlaying = false;
    }

    void TrySummon()
    {
        // 이동 중만 가능
        if (!canMove)
            return;

        // 확률 체크
        if (Random.value > summonChance)
            return;

        // 현재 실제 소환 개수
        int currentSummonCount = summonCount * summonMultiplier;

        // 설정 개수만큼 소환
        for (int i = 0; i < currentSummonCount; i++)
        {
            // 풀에서 몬스터 가져오기
            GameObject monster = GameManager.instance.pool.GetEnemy(summonMonsterIndex);

            if (monster != null)
            {
                // 보스 주변 랜덤 위치
                Vector2 offset = Random.insideUnitCircle * 2f;

                monster.transform.position = (Vector2)transform.position + offset;
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        // 부모 데미지 처리
        base.TakeDamage(damage);

        // 현재 체력 비율
        float hpPercent = health / maxHealth;

        // 25% 이하
        if (hpPercent <= 0.25f && currentLavaPhase < 3)
        {
            ActivateLavaTile(3);

            // 소환 x8
            summonMultiplier = 8;
        }

        // 50% 이하
        else if (hpPercent <= 0.5f && currentLavaPhase < 2)
        {
            ActivateLavaTile(2);

            // 소환 x4
            summonMultiplier = 4;
        }

        // 75% 이하
        else if (hpPercent <= 0.75f && currentLavaPhase < 1)
        {
            ActivateLavaTile(1);

            // 소환 x2
            summonMultiplier = 2;
        }
    }

    void ActivateLavaTile(int index)
    {
        // 모든 타일 비활성화
        for (int i = 0; i < lavaTiles.Length; i++)
        {
            if (lavaTiles[i] != null)
                lavaTiles[i].SetActive(false);
        }

        // 해당 타일 활성화
        if (index < lavaTiles.Length && lavaTiles[index] != null)
        {
            lavaTiles[index].SetActive(true);

            // 현재 단계 저장
            currentLavaPhase = index;
        }
    }
}