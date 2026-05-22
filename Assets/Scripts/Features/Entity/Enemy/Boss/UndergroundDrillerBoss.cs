using System.Collections;
using UnityEngine;

// 지하 굴착사 보스
public class UndergroundDrillerBoss : BossBase
{
    enum Pattern
    {
        // 땅속 이동
        UndergroundMove,

        // 등장 + 낙석
        RiseAndRockFall
    }

    [Header("연기 오브젝트")]
    // 땅속 연기
    [SerializeField] GameObject undergroundSmoke;

    [Header("낙석 패턴")]
    // FallingRock 프리팹 번호
    [SerializeField] int fallingRockIndex;
    // 낙석 개수
    [SerializeField] int rockCount = 6;
    // 낙석 생성 범위
    [SerializeField] float rockSpawnRadius = 5f;

    [Header("패턴 시간")]
    // 땅속 이동 시간
    [SerializeField] float undergroundDuration = 3f;
    // 지상 유지 시간
    [SerializeField] float groundStayTime = 3f;

    // 폴 매니저
    PoolManager pool;
    // 콜라이더
    Collider2D col;

    // 원래 레이어 저장
    int originLayer;
    // 현재 땅속 상태
    bool isUnderground;
    // 이전 패턴 저장
    Pattern lastPattern;

    protected override void Awake()
    {
        // 부모 초기화
        base.Awake();

        // 컴포넌트 저장
        col = GetComponent<Collider2D>();

        // 원래 레이어 저장
        originLayer = gameObject.layer;
    }

    protected override void Start()
    {
        // 폴 저장
        pool = GameManager.instance.pool;
    }

    protected override void OnEnable()
    {
        // 부모 초기화
        base.OnEnable();

        // 시작 시 땅속 상태
        EnterUnderground();
    }

    protected override void FixedUpdate()
    {
        // 땅속 상태일 때만 이동
        if (!isUnderground)
        {
            // 이동 정지
            rigid.linearVelocity = Vector2.zero;

            // 이동 애니메이션 OFF
            anim.SetInteger("Moving", 0);

            return;
        }

        // 플레이어 없음
        if (target == null)
            return;

        // 플레이어 방향
        Vector2 dir =
            ((Vector2)target.position -
            rigid.position).normalized;

        // 이동
        rigid.MovePosition(
            rigid.position +
            dir * moveSpeed * Time.fixedDeltaTime
        );

        // 이동 애니메이션 ON
        anim.SetInteger("Moving", 1);

        // 좌우 반전
        spriter.flipX =
            target.position.x < transform.position.x;
    }

    // 랜덤 패턴 시작
    protected override void StartRandomPattern()
    {
        // 이미 패턴 중이면 종료
        if (isPatternPlaying)
            return;

        // 다음 패턴
        Pattern nextPattern;

        // 같은 패턴 연속 방지
        do
        {
            nextPattern =
                (Pattern)Random.Range(0, 2);
        }
        while (nextPattern == lastPattern);

        // 현재 패턴 저장
        lastPattern = nextPattern;

        switch (nextPattern)
        {
            // 땅속 이동
            case Pattern.UndergroundMove:
                StartCoroutine(PatternUndergroundMove());
                break;

            // 등장 + 낙석
            case Pattern.RiseAndRockFall:
                StartCoroutine(PatternRiseAndRockFall());
                break;
        }
    }

    // 땅속 이동 패턴
    IEnumerator PatternUndergroundMove()
    {
        // 패턴 시작
        isPatternPlaying = true;

        // 땅속 진입
        EnterUnderground();

        // 일정 시간 이동
        yield return new WaitForSeconds(undergroundDuration);

        // 패턴 종료
        isPatternPlaying = false;
    }

    // 등장 + 낙석 패턴
    IEnumerator PatternRiseAndRockFall()
    {
        // 패턴 시작
        isPatternPlaying = true;

        // 땅속 상태 유지
        EnterUnderground();

        // 땅속 이동 시간
        yield return new WaitForSeconds(undergroundDuration);

        // 이동 정지
        rigid.linearVelocity = Vector2.zero;

        // 지상 등장
        ExitUnderground();

        // 낙석 생성
        for (int i = 0; i < rockCount; i++)
        {
            // 랜덤 위치
            // 원형으로 균등 배치
            float angle = (360f / rockCount) * i;
            // 각도 랜덤 보정
            angle += Random.Range(-20f, 20f);
            // 방향 계산
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            // 거리 랜덤
            float distance = Random.Range(rockSpawnRadius * 0.5f, rockSpawnRadius);
            // 최종 위치
            Vector2 randomPos = (Vector2)transform.position + dir * distance;

            // 낙석 가져오기
            GameObject rock =
                pool.GetBossBullet(fallingRockIndex);

            // 낙석 없음
            if (rock == null)
                continue;

            // 부모를 폴 매니저로 변경
            rock.transform.SetParent(pool.transform);

            // 위치 설정
            rock.transform.position = randomPos;

            // 활성화
            rock.SetActive(true);
        }

        // 지상 유지
        yield return new WaitForSeconds(groundStayTime);

        // 다시 땅속 진입
        EnterUnderground();

        // 패턴 쿨타임
        yield return new WaitForSeconds(patternCooldown);

        // 패턴 종료
        isPatternPlaying = false;
    }

    // 땅속 진입
    void EnterUnderground()
    {
        // 이미 땅속이면 무시
        if (isUnderground)
            return;

        // 투명화
        Color c = spriter.color;
        c.a = 0f;
        spriter.color = c;

        // 연기 활성화
        if (undergroundSmoke != null)
            undergroundSmoke.SetActive(true);

        // Hide 레이어 변경
        gameObject.layer = 25;

        // 콜라이더 끄기
        if (col != null)
            col.enabled = false;

        // 땅속 상태
        isUnderground = true;
    }

    // 지상 등장
    void ExitUnderground()
    {
        // 이미 지상이면 무시
        if (!isUnderground)
            return;

        // 다시 보이기
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;

        // 연기 비활성화
        if (undergroundSmoke != null)
            undergroundSmoke.SetActive(false);

        // 원래 레이어 복구
        gameObject.layer = originLayer;

        // 콜라이더 켜기
        if (col != null)
            col.enabled = true;

        // 지상 상태
        isUnderground = false;
    }
}