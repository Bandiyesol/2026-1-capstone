using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 폭풍의 해룡 보스 컨트롤러 (BossBase 상속)
/// </summary>
public class StormDragonBoss : BossBase
{
    [Header("물기둥 패턴")]
    [SerializeField] int waterspoutIndex;           // 풀링에서 사용할 물기둥 인덱스
    [SerializeField] int waterspoutCount = 4;       // 기본 생성 물기둥 개수
    [SerializeField] float waterspoutRadius = 3f;    // 물기둥 최대 생성 반경
    [SerializeField] float waterspoutCooldown = 3f; // 물기둥 자동 생성 주기

    [Header("번개 탄막")]
    [SerializeField] int lightningBulletIndex;       // 풀링에서 사용할 번개 인덱스
    [SerializeField] int lightningSpreadCount = 5;    // 한 번에 발사할 탄막 개수
    [SerializeField] float lightningSpreadAngle = 30f;// 부채꼴 전체 범위 각도
    [SerializeField] int lightningRoundCount = 3;     // 총 발사 연사 횟수
    [SerializeField] float lightningFireDelay = 0.3f; // 연사 간격 대기 시간
    [SerializeField] float lightningSpawnDistance = 2f;// 보스 중심 기준 생성 거리

    [Header("유도 번개")]
    [SerializeField] int homingBulletIndex; // 풀링에서 사용할 유도 번개 인덱스
    [SerializeField] int homingCount = 8;       // 원형 생성 유도 탄막 개수
    [SerializeField] float homingRadius = 2f;    // 보스 중심 기준 생성 반지름
    [SerializeField] float homingDelay = 0.5f;   // 패턴 종료 후 후딜레이

    // 매니저 및 타이머 참조
    PoolManager pool;          // 게임 매니저에서 가져올 오브젝트 풀링 참조
    float waterspoutTimer;     // 물기둥 자동 생성을 위한 개별 타이머

    // 페이즈 중복 발동 방지 플래그
    bool phase75; // 체력 75% 페이즈
    bool phase50; // 체력 50% 페이즈
    bool phase25; // 체력 25% 페이즈

    // 메모리 관리
    List<GameObject> spawnedObjects = new List<GameObject>(); // 추적 및 사망 시 초기화용 리스트

    // 활성화 시 데이터 초기화
    protected override void OnEnable()
    {
        base.OnEnable();

        waterspoutTimer = 0f; // 타이머 리셋

        // 페이즈 플래그 초기화
        phase75 = false;
        phase50 = false;
        phase25 = false;
    }

    protected override void Start()
    {
        pool = GameManager.instance.pool; // 게임 매니저 인스턴스에서 풀 매니저 캐싱
    }

    protected override void Update()
    {
        float hpRate = health / maxHealth; // 현재 체력 비율 계산 (0.0 ~ 1.0)

        // [페이즈 변환 검사] 체력 구간 도달 시 물기둥 생성 개수 누적 강화 (+3개씩)
        if (!phase75 && hpRate <= 0.75f)
        {
            phase75 = true;
            waterspoutCount += 3;
        }

        if (!phase50 && hpRate <= 0.5f)
        {
            phase50 = true;
            waterspoutCount += 3;
        }

        if (!phase25 && hpRate <= 0.25f)
        {
            phase25 = true;
            waterspoutCount += 3;
        }

        // [상시 패턴] 물기둥 자동 생성 타이머 누적 및 실행
        waterspoutTimer += Time.deltaTime;

        if (waterspoutTimer >= waterspoutCooldown)
        {
            waterspoutTimer = 0f;
            SpawnWaterspout(); // 쿨타임 충족 시 물기둥 생성
        }

        // [메인 패턴 제어] 현재 특수 패턴이 재생 중이면 이동 차단 후 업데이트 중단
        if (isPatternPlaying)
        {
            canMove = false;
            patternTimer += Time.deltaTime; // 부모 클래스의 패턴 타이머는 유지
            return;
        }

        canMove = true; // 일반 상태일 때 이동 가능 처리

        patternTimer += Time.deltaTime; // 다음 무작위 패턴 타이머 누적

        // 메인 패턴 쿨타임 충족 시 무작위 패턴 실행
        if (patternTimer >= patternCooldown)
        {
            patternTimer = 0f;
            StartRandomPattern();
        }
    }

    // 보스 주변 무작위 원형 범위에 물기둥 생성
    void SpawnWaterspout()
    {
        for (int i = 0; i < waterspoutCount; i++)
        {
            // 균등 분할 각도 계산 및 ±20도 난수 적용으로 자연스러운 배치
            float angle = (360f / waterspoutCount) * i;
            angle += Random.Range(-20f, 20f);

            // 각도를 회전 방향 벡터로 변환
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;

            // 최소 반경 ~ 최대 반경 사이의 무작위 거리 산출
            float distance =
                Random.Range(waterspoutRadius * 0.5f, waterspoutRadius);

            // 최종 스폰 좌표 결정
            Vector2 spawnPos =
                (Vector2)transform.position + dir * distance;

            GameObject obj = pool.GetBossBullet(waterspoutIndex); // 풀에서 오브젝트 획득

            if (obj == null)
                continue;

            obj.transform.SetParent(pool.transform); // 부모 트리 정리
            obj.transform.position = spawnPos;       // 계산된 위치 적용
            obj.SetActive(true);                     // 활성화

            spawnedObjects.Add(obj); // 사망 시 일괄 제거를 위한 리스트 등록
        }
    }

    // 난수 기반 메인 패턴 결정 (기본 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        int pattern = Random.Range(0, 2); // 0 또는 1 무작위 결정

        if (pattern == 0)
            StartCoroutine(PatternLightningBarrage()); // 부채꼴 탄막 연사
        else
            StartCoroutine(PatternHomingLightning());  // 원형 유도 번개
    }

    // [패턴 1] 플레이어 방향 부채꼴 교차 번개 연사 코루틴
    IEnumerator PatternLightningBarrage()
    {
        isPatternPlaying = true; // 패턴 시작 플래그 세팅

        // 타겟(플레이어) 부재 시 예외 처리 및 조기 종료
        if (target == null)
        {
            isPatternPlaying = false;
            yield break;
        }

        // 탄막 1개당 벌어질 간격 각도 계산
        float stepAngle =
            lightningSpreadAngle / (lightningSpreadCount - 1);

        // 연사 횟수만큼 루프 실행
        for (int round = 0; round < lightningRoundCount; round++)
        {
            Vector2 myPos = transform.position;

            // ⭐ [오류 수정 발생 구간] target.position(Vector3)을 (Vector2)로 캐스팅하여 모호성 해결
            Vector2 baseDir = ((Vector2)target.position - myPos).normalized;

            // 기본 조준 각도 계산 (라디안 -> 디그리 변환)
            float baseAngle =
                Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            // 홀수 번째 라운드마다 탄막을 절반 각도만큼 비틀어 교차 사격 연출
            float gapOffset =
                (round % 2 == 1) ? stepAngle * 0.5f : 0f;

            // 한 라운드의 탄막 개수만큼 루프 발사
            for (int i = 0; i < lightningSpreadCount; i++)
            {
                // 부채꼴 좌측 끝 시작점 설정
                float startAngle =
                    baseAngle - lightningSpreadAngle * 0.5f;

                // 순차적 간격 및 교차 오프셋을 더해 최종 투사체 각도 결정
                float finalAngle =
                    startAngle + stepAngle * i + gapOffset;

                // 디그리 각도를 다시 방향 삼각함수 벡터로 환원
                Vector2 dir =
                    new Vector2(
                        Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                        Mathf.Sin(finalAngle * Mathf.Deg2Rad));

                anim.SetTrigger("Attack"); // 공격 애니메이션 재생 트리거

                GameObject bullet =
                    pool.GetBossBullet(lightningBulletIndex); // 풀에서 투사체 가져오기

                if (bullet == null)
                    continue;

                BossBullet bb =
                    bullet.GetComponent<BossBullet>();

                if (bb == null)
                    continue;

                bullet.transform.SetParent(null); // 월드 좌표계 독립
                bullet.transform.position =
                    myPos + dir * lightningSpawnDistance; // 생성 오프셋 거리 적용

                bb.Init(dir);           // 투사체 진행 방향 초기화
                bullet.SetActive(true); // 오브젝트 활성화

                spawnedObjects.Add(bullet); // 추적 리스트 등록
            }

            yield return new WaitForSeconds(lightningFireDelay); // 다음 연사까지 대기
        }

        yield return new WaitForSeconds(0.5f); // 패턴 종료 후 최종 후딜레이

        isPatternPlaying = false; // 패턴 종료 플래그 OFF
    }

    // [패턴 2] 보스 주변 원형으로 유도 번개 배치 후 발사 코루틴
    IEnumerator PatternHomingLightning()
    {
        isPatternPlaying = true; // 패턴 시작 플래그 세팅
        canMove = false;         // 패턴 집중을 위한 본체 이동 정지

        for (int i = 0; i < homingCount; i++)
        {
            // 360도 균등 각도 분할
            float angle = (360f / homingCount) * i;
            Vector2 dir =
                Quaternion.Euler(0, 0, angle) * Vector2.right;

            // 보스 기준 원형 테두리 좌표 계산
            Vector2 spawnPos =
                (Vector2)transform.position + dir * homingRadius;

            anim.SetTrigger("Attack"); // 공격 애니메이션 작동

            GameObject bullet =
                pool.GetBossBullet(homingBulletIndex); // 유도 번개 오브젝트 풀 추출

            if (bullet == null)
                continue;

            bullet.transform.position = spawnPos; // 위치 세팅

            BossBullet bb =
                bullet.GetComponent<BossBullet>();

            if (bb != null)
                bb.Init(dir); // 초기 방향 전달 (이후 유도 로직은 투사체 내부에서 수행)

            bullet.SetActive(true); // 활성화

            spawnedObjects.Add(bullet); // 추적 리스트 등록
        }

        yield return new WaitForSeconds(homingDelay); // 패턴 후딜레이 대기

        canMove = true;           // 이동 능력 해제
        isPatternPlaying = false; // 패턴 완전히 종료
    }

    // 데미지 입력 수신 오버라이드
    public override void TakeDamage(float damage)
    {
        // 핵심 캐스팅/패턴 작동 중에는 보스에게 들어오는 피해량 50% 경감 (방어 태세)
        if (isPatternPlaying)
            damage *= 0.5f;

        base.TakeDamage(damage); // 부모 클래스의 실 데미지 및 사망 연산 수행
    }

    // 보스 사망 시 호출 처리 (오버라이드)
    protected override void Dead()
    {
        ClearSpawnedObjects(); // 필드 내 잔존 탄막/물기둥 강제 클리어
        base.Dead();           // 부모 클래스의 사망 시퀀스 작동
    }

    // 현재 생성된 모든 패턴용 하위 투사체를 풀링 반환 및 리스트 정리
    void ClearSpawnedObjects()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
                spawnedObjects[i].SetActive(false); // 오브젝트 비활성화 복귀
        }

        spawnedObjects.Clear(); // 리스트 메모리 비우기
    }
}