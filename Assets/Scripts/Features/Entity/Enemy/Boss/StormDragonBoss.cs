using System.Collections;
using UnityEngine;

// 폭풍의 해룡 보스
public class StormDragonBoss : BossBase
{
    [Header("물기둥 패턴")]
    // 물기둥 프리팹 번호
    [SerializeField]
    int waterspoutIndex;

    // 물기둥 소환 개수
    [SerializeField]
    public int waterspoutCount = 4;

    // 물기둥 소환 범위 (보스 주변)
    [SerializeField]
    public float waterspoutRadius = 3f;

    // 물기둥 쿨타임 (독립)
    [SerializeField]
    public float waterspoutCooldown = 3f;

    [Header("번개탄막 패턴")]
    // 번개탄막 프리팹 번호
    [SerializeField]
    int lightningBulletIndex;

    // 부채꼴 탄막 개수
    [SerializeField]
    public int lightningSpreadCount = 5;

    // 부채꼴 각도 범위
    [SerializeField]
    public float lightningSpreadAngle = 30f;

    // 발사 횟수
    [SerializeField]
    public int lightningRoundCount = 3;

    // 발사 간격
    [SerializeField]
    public float lightningFireDelay = 0.3f;

    // 탄막 발사 거리 배열 (번갈아가며)
    [SerializeField]
    public float[] lightningSpawnDistances = { 2f, 4f };

    // 탄막이 생성될 거리 (추가)
    [SerializeField]
    public float lightningSpawnDistance = 2.0f;

    // 폴 매니저
    PoolManager pool;

    // 물기둥 타이머
    float waterspoutTimer;

    protected override void Awake()
    {
        // 부모 실행
        base.Awake();
    }

    protected override void OnEnable()
    {
        // 부모 실행
        base.OnEnable();

        // 물기둥 타이머 초기화
        waterspoutTimer = 0f;
    }

    protected override void Start()
    {
        // 폴 저장
        pool = GameManager.instance.pool;
    }

    protected override void Update()
    {
        // 물기둥 타이머 증가 (항상)
        waterspoutTimer += Time.deltaTime;

        // 물기둥 쿨타임 도달 (패턴 중에도 계속 소환)
        if (waterspoutTimer >= waterspoutCooldown)
        {
            waterspoutTimer = 0f;
            SpawnWaterspout();
        }

        // 패턴 중이면 공동 쿨타임만 증가 + 추적 멈춤
        if (isPatternPlaying)
        {
            canMove = false;
            patternTimer += Time.deltaTime;
            return;
        }

        // 패턴 종료 후 추적 재개
        canMove = true;

        // 공동 쿨타임 처리
        patternTimer += Time.deltaTime;

        // 공동 쿨타임 도달
        if (patternTimer >= patternCooldown)
        {
            patternTimer = 0f;
            StartCoroutine(PatternLightningBarrage());
        }
    }

    // 물기둥 소환
    void SpawnWaterspout()
    {
        for (int i = 0; i < waterspoutCount; i++)
        {
            // 원형으로 균등 배치
            float angle = (360f / waterspoutCount) * i;
            // 각도 랜덤 보정
            angle += Random.Range(-20f, 20f);

            // 방향 계산
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

            // 거리 랜덤
            float distance = Random.Range(waterspoutRadius * 0.5f, waterspoutRadius);

            // 최종 위치
            Vector2 spawnPos = (Vector2)transform.position + dir * distance;

            // 물기둥 가져오기
            GameObject waterspout = pool.GetBossBullet(waterspoutIndex);

            if (waterspout == null)
                continue;

            // 부모를 폴 매니저로 변경
            waterspout.transform.SetParent(pool.transform);

            // 위치 설정
            waterspout.transform.position = spawnPos;

            // 활성화
            waterspout.SetActive(true);
        }
    }

    // 랜덤 패턴 (사용 안 함 - Update에서 직접 처리)
    protected override void StartRandomPattern()
    {
        // 공동 쿨타임은 Update에서 처리하므로 여기서는 사용 안 함
    }

    // 번개 탄막 발사 패턴
    IEnumerator PatternLightningBarrage()
    {
        isPatternPlaying = true;
        waterspoutTimer = 0f;

        if (target == null)
        {
            isPatternPlaying = false;
            yield break;
        }

        Vector2 myPos = transform.position;

        // 1. 탄막 사이의 각도 간격 계산 (전체 각도 / (개수 - 1))
        // 예: 30도 범구에 5발이면 간격은 7.5도
        float stepAngle = lightningSpreadAngle / (lightningSpreadCount - 1);

        for (int round = 0; round < lightningRoundCount; round++)
        {
            // 다시 계산 (보스가 이동했을 수 있으므로)
            Vector2 targetPos = target.position;
            Vector2 baseDir = (targetPos - myPos).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // 2. 틈새 공략 오프셋 계산
            // round가 0, 2, 4... 이면 0 (정방향)
            // round가 1, 3, 5... 이면 간격의 절반만큼 회전 (틈새 방향)
            float gapOffset = (round % 2 == 1) ? stepAngle * 0.5f : 0f;

            for (int i = 0; i < lightningSpreadCount; i++)
            {
                // 중앙을 기준으로 부채꼴 배치 + 틈새 오프셋 추가
                float startAngle = baseAngle - (lightningSpreadAngle * 0.5f);
                float finalAngle = startAngle + (stepAngle * i) + gapOffset;

                Vector2 bulletDir = new Vector2(
                    Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                    Mathf.Sin(finalAngle * Mathf.Deg2Rad)
                );

                GameObject bullet = pool.GetBossBullet(lightningBulletIndex);
                if (bullet != null)
                {
                    bullet.transform.SetParent(null);
                    BossBullet bossBullet = bullet.GetComponent<BossBullet>();
                    if (bossBullet != null)
                    {
                        // 거리는 기존 설정된 lightningSpawnDistance 사용
                        Vector3 spawnPos = (Vector3)myPos + (Vector3)(bulletDir * lightningSpawnDistance);
                        spawnPos.z = 0f;
                        bullet.transform.position = spawnPos;

                        bossBullet.Init(bulletDir);
                        bullet.SetActive(true);
                    }
                }
            }

            yield return new WaitForSeconds(lightningFireDelay);
        }

        yield return new WaitForSeconds(0.5f);
        isPatternPlaying = false;
    }

    // 데미지 처리 오버라이드
    public override void TakeDamage(float damage)
    {
        // 패턴 중이면 데미지 감소 (선택사항)
        if (isPatternPlaying)
        {
            damage *= 0.5f;
        }

        // 부모 클래스 데미지 처리
        base.TakeDamage(damage);
    }
}