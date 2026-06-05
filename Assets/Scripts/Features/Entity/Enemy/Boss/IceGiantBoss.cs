using System.Collections;
using UnityEngine;

public class IceGiantBoss : BossBase
{
    [Header("탄 설정")]
    [SerializeField] int slamBulletIndex;     // 내려찍기 장판
    [SerializeField] int shardBulletIndex;    // 얼음 파편

    [Header("경고원")]
    [SerializeField] GameObject warningCircle; // 공격 전 표시

    [Header("내려찍기")]
    [SerializeField] float slamCooldown = 5f; // 쿨타임
    [SerializeField] float slamRange = 4f;    // 발동 거리
    [SerializeField] float warningTime = 1.2f; // 경고 지속 시간

    [Header("파편")]
    [SerializeField] float shardChance = 0.015f; // 이동 중 발사 확률
    [SerializeField] int shardCount = 8;         // 발사 개수

    [Header("광폭")]
    [SerializeField] float enragedSpeedMultiplier = 1.5f; // 속도 배율

    float slamTimer;         // 내려찍기 타이머
    bool isCasting;          // 패턴 시전 여부
    float originMoveSpeed;   // 기본 이동속도 저장

    PoolManager pool;        // 오브젝트 풀
    Collider2D bossCollider; // 보스 충돌체

    protected override void OnEnable()
    {
        base.OnEnable();

        // 상태 초기화
        slamTimer = 0f;
        isCasting = false;

        // 참조 캐싱
        pool = GameManager.instance.pool;
        originMoveSpeed = moveSpeed;
        bossCollider = GetComponent<Collider2D>();

        // 경고원 숨김
        if (warningCircle != null)
            warningCircle.SetActive(false);
    }

    protected override void Update()
    {
        // 타겟 없으면 중단
        if (target == null) return;

        // 쿨타임 누적
        slamTimer += Time.deltaTime;

        // 체력 절반 이하 광폭화
        moveSpeed = health <= maxHealth * 0.5f
            ? originMoveSpeed * enragedSpeedMultiplier
            : originMoveSpeed;

        // 패턴 중이면 행동 정지
        if (isCasting) return;

        // 플레이어 거리 계산
        float dist = Vector2.Distance(transform.position, target.position);

        // 플레이어 추적 이동
        if (canMove && moveSpeed > 0)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }

        // 내려찍기 조건
        if (dist <= slamRange && slamTimer >= slamCooldown)
        {
            StartCoroutine(SlamPattern());
            return;
        }

        // 이동 중 랜덤 파편 발사
        if (Random.value <= shardChance)
            FireShardCircle();
    }

    // 부모 랜덤패턴 비활성화
    protected override void StartRandomPattern() { }

    IEnumerator SlamPattern()
    {
        // 패턴 시작
        isCasting = true;
        isPatternPlaying = true;
        canMove = false;

        // 현재 속도 저장 후 정지
        float prevSpeed = moveSpeed;
        moveSpeed = 0f;

        // 이동 애니메이션 정지
        if (anim != null)
        {
            anim.SetInteger("Moving", 0);
            anim.ResetTrigger("Attack");
        }

        // 경고 표시
        if (warningCircle != null)
            warningCircle.SetActive(true);

        // 경고 대기
        yield return new WaitForSeconds(warningTime);

        // 경고 제거
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 공격 애니메이션
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }

        // 보스 중심 기준
        Vector3 spawnOrigin = transform.position;

        // 내려찍기 생성
        GameObject slam = pool.GetBossBullet(slamBulletIndex);
        slam.transform.position = spawnOrigin;

        // 적과 충돌 무시
        IgnoreEnemyCollision(slam);

        // 파편 동시 발사
        FireShardCircleFromPoint(spawnOrigin);

        // 쿨타임 초기화
        slamTimer = 0f;

        // 후딜
        yield return new WaitForSeconds(0.7f);

        // 이동 복구
        moveSpeed = prevSpeed;
        canMove = true;
        isPatternPlaying = false;
        isCasting = false;
    }

    void FireShardCircleFromPoint(Vector3 centerOrigin)
    {
        if (pool == null) return;

        for (int i = 0; i < shardCount; i++)
        {
            // 각도 계산
            float angle = i * (360f / shardCount) * Mathf.Deg2Rad;

            // 방향 벡터
            Vector2 dir = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );

            // 탄 생성
            GameObject shard = pool.GetBossBullet(shardBulletIndex);

            // 보스 중심 생성
            shard.transform.position = centerOrigin;

            // 이동 방향 설정
            shard.GetComponent<BossBullet>().Init(dir);

            // 스프라이트 회전
            float degree = (i * (360f / shardCount)) + 180f;
            shard.transform.rotation =
                Quaternion.Euler(0, 0, degree);

            // 적 관통
            IgnoreEnemyCollision(shard);
        }
    }

    // 현재 위치에서 원형 발사
    void FireShardCircle()
    {
        FireShardCircleFromPoint(transform.position);
    }

    // 적/보스 충돌 무시
    void IgnoreEnemyCollision(GameObject bullet)
    {
        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (bulletCol == null) return;

        // 보스와 충돌 무시
        if (bossCollider != null)
            Physics2D.IgnoreCollision(bulletCol, bossCollider);

        // 모든 적과 충돌 무시
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            Collider2D enemyCol = enemy.GetComponent<Collider2D>();

            if (enemyCol != null)
                Physics2D.IgnoreCollision(bulletCol, enemyCol);
        }
    }
}