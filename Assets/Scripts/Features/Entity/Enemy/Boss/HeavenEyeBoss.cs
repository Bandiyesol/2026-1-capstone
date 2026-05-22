using System.Collections;
using UnityEngine;

public class HeavenEyeBoss : BossBase
{
    enum Pattern
    {
        SpreadShot,
        InvisibleShot
    }

    [Header("탄막")]
    // 탄막 프리팹 번호
    [SerializeField]
    int bulletIndex;
    // 발사 개수
    [SerializeField]
    int spreadCount = 3;
    // 탄막 각도 간격
    [SerializeField]
    float angleGap = 20f;
    // 발사 간격
    [SerializeField]
    float shotDelay = 0.15f;
    // 탄막 소환 거리
    [SerializeField]
    float spawnDistance = 1.2f;

    [Header("투명 순간이동")]
    // 투명 시간
    [SerializeField]
    float invisibleTime = 3f;
    // 순간이동 간격
    [SerializeField]
    float teleportDelay = 0.6f;
    // 플레이어와 유지할 최소 거리
    [SerializeField]
    float teleportDistance = 4f;
    // 순간이동 위치 랜덤 범위
    [SerializeField]
    float teleportRandomOffset = 1.5f;

    // 폴 매니저
    PoolManager pool;

    protected override void Awake()
    {
        // 부모 실행
        base.Awake();
    }

    protected override void OnEnable()
    {
        // 부모 실행
        base.OnEnable();

        // 투명도 복구
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;
    }

    protected override void Start()
    {
        // 폴 저장
        pool = GameManager.instance.pool;
    }

    protected override void StartRandomPattern()
    {
        // 패턴 랜덤
        int rand = Random.Range(0, 2);

        switch ((Pattern)rand)
        {
            case Pattern.SpreadShot:
                StartCoroutine(PatternSpreadShot());
                break;

            case Pattern.InvisibleShot:
                StartCoroutine(PatternInvisibleShot());
                break;
        }
    }

    // 3갈래 탄막 패턴
    IEnumerator PatternSpreadShot()
    {
        if (isPatternPlaying) yield break;

        isPatternPlaying = true;
        canMove = false;

        yield return new WaitForSeconds(0.3f);

        if (target == null)
        {
            canMove = true;
            isPatternPlaying = false;
            yield break;
        }

        // 현재 위치 저장
        Vector2 myPos = transform.position;

        // 타겟 위치 저장
        Vector2 targetPos = target.position;

        // 방향 계산
        Vector2 baseDir = (targetPos - myPos).normalized;

        // 각도 계산
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < spreadCount; i++)
        {
            // 퍼지는 각도 계산
            float angleOffset = (-angleGap * (spreadCount - 1) / 2f) + (angleGap * i);

            // 최종 각도
            float finalAngle = baseAngle + angleOffset;

            // 방향 변환
            Vector2 shotDir = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            );

            GameObject bullet = pool.GetBossBullet(bulletIndex);

            if (bullet != null)
            {
                // 부모 해제
                bullet.transform.SetParent(null);

                BossBullet bossBullet = bullet.GetComponent<BossBullet>();

                if (bossBullet != null)
                {
                    // 생성 위치
                    Vector3 spawnPos = myPos + (shotDir * spawnDistance);
                    spawnPos.z = 0f;

                    bullet.transform.position = spawnPos;

                    // 방향 초기화
                    bossBullet.Init(shotDir);

                    bullet.SetActive(true);
                }
            }

            yield return new WaitForSeconds(shotDelay);
        }

        yield return new WaitForSeconds(1f);

        canMove = true;
        isPatternPlaying = false;
    }

    // 투명 패턴
    IEnumerator PatternInvisibleShot()
    {
        if (isPatternPlaying) yield break;

        // 패턴 시작
        isPatternPlaying = true;

        // 반투명
        Color c = spriter.color;
        c.a = 0.01f;
        spriter.color = c;

        float timer = 0f;

        // 투명 상태 유지
        while (timer < invisibleTime)
        {
            // 순간이동 실행
            TeleportAroundTarget();

            // 대기
            yield return new WaitForSeconds(teleportDelay);

            timer += teleportDelay;
        }

        // 원상복구
        c.a = 1f;
        spriter.color = c;

        // 패턴 종료
        isPatternPlaying = false;
    }

    // 플레이어 주변 순간이동
    void TeleportAroundTarget()
    {
        // 타겟 없으면 종료
        if (target == null) return;

        // 랜덤 방향
        Vector2 dir = Random.insideUnitCircle.normalized;

        // 거리 적용
        Vector2 offset = dir * teleportDistance;

        // 추가 랜덤값
        offset += Random.insideUnitCircle * teleportRandomOffset;

        // 최종 위치
        Vector3 nextPos = target.position + (Vector3)offset;

        // Z축 고정
        nextPos.z = 0f;

        // 순간이동
        transform.position = nextPos;
    }
}