using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지하 굴착사 보스 컨트롤러 (땅속 잠행 이동 + 지상 등장 및 낙석 부모 클래스 상속)
/// </summary>
public class UndergroundDrillerBoss : BossBase
{
    // 패턴 종류 정의 (직관성을 위한 열거형)
    enum Pattern
    {
        UndergroundMove,
        RiseAndRockFall
    }

    [Header("연기 오브젝트")]
    [SerializeField] GameObject undergroundSmoke; // 땅속 이동 시 활성화할 먼지/연기 이펙트

    [Header("낙석 패턴")]
    [SerializeField] int fallingRockIndex;        // 풀링에서 사용할 낙석 오브젝트 인덱스
    [SerializeField] int rockCount = 6;           // 한 번에 생성할 낙석 개수
    [SerializeField] float rockSpawnRadius = 5f;  // 낙석 최대 생성 반경

    [Header("패턴 시간")]
    [SerializeField] float undergroundDuration = 3f; // 땅속 잠행 유지 시간
    [SerializeField] float groundStayTime = 3f;      // 지상 등장 후 머무르는 시간

    // 매니저 및 상태 변수
    PoolManager pool;
    int originLayer;       // 지상 상태의 원래 레이어 백업용
    bool isUnderground;    // 현재 땅속에 머물러 있는지 여부
    Pattern lastPattern;   // 직전에 실행했던 패턴 기록 (중복 방지용)

    // 🔥 메모리 관리용 낙석 추적 리스트
    List<GameObject> rocks = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();

        col = GetComponent<Collider2D>(); // 본체 콜라이더 캐싱
        originLayer = gameObject.layer;   // 기본 레이어 저장
    }

    protected override void Start()
    {
        pool = GameManager.instance.pool; // 풀 매니저 싱글톤 캐싱
    }

    // 오브젝트 활성화 시 초기화
    protected override void OnEnable()
    {
        base.OnEnable();

        EnterUnderground(); // 시작하자마자 땅속 잠행 상태로 돌입

        rocks.Clear(); // 낙석 추적 리스트 초기화
    }

    protected override void FixedUpdate()
    {
        // 1. 지상 상태일 때는 이동을 멈추고 대기 애니메이션 처리
        if (!isUnderground)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetInteger("Moving", 0);
            return;
        }

        // 2. 땅속 상태일 때는 플레이어를 추적하여 이동
        if (target == null)
            return;

        // 플레이어 정방향 벡터 계산
        Vector2 dir =
            ((Vector2)target.position - rigid.position).normalized;

        // 리지드바디를 통한 물리 이동 가동
        rigid.MovePosition(
            rigid.position + dir * moveSpeed * Time.fixedDeltaTime
        );

        anim.SetInteger("Moving", 1); // 잠행 이동 애니메이션 재생

        // 플레이어 위치에 따라 스프라이트 좌우 반전(X축) 조절
        spriter.flipX = target.position.x < transform.position.x;
    }

    // 메인 패턴 타이머 충족 시 호출 (부모 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        if (isPatternPlaying)
            return;

        Pattern next;

        // [중복 방지 루프] 이전 패턴과 다른 패턴이 나올 때까지 난수 반복 추출
        do
        {
            next = (Pattern)Random.Range(0, 2);
        }
        while (next == lastPattern);

        lastPattern = next; // 최근 실행 패턴 갱신

        // 결정된 패턴 코루틴 가동
        if (next == Pattern.UndergroundMove)
            StartCoroutine(PatternUndergroundMove());
        else
            StartCoroutine(PatternRiseAndRockFall());
    }

    // ==========================================
    // [패턴 1] 단순 땅속 이동 코루틴
    // ==========================================
    IEnumerator PatternUndergroundMove()
    {
        isPatternPlaying = true;

        EnterUnderground(); // 땅속 상태 진입

        yield return new WaitForSeconds(undergroundDuration); // 잠행 시간만큼 이동 대기

        isPatternPlaying = false; // 패턴 종료 (다음 무작위 패턴 준비)
    }

    // ==========================================
    // [패턴 2] 지상 등장 + 원형 범위 낙석 코루틴
    // ==========================================
    IEnumerator PatternRiseAndRockFall()
    {
        isPatternPlaying = true;

        EnterUnderground(); // 안전하게 땅속에서 시작

        yield return new WaitForSeconds(undergroundDuration); // 등장을 위한 잠행 이동 시간

        rigid.linearVelocity = Vector2.zero; // 등장 직전 속도 물리적 초기화

        ExitUnderground(); // 지상으로 돌출 등장 (콜라이더 및 무적 해제)

        // [핵심 기믹] 보스 주변 원형 범위를 계산하여 낙석 생성
        for (int i = 0; i < rockCount; i++)
        {
            // 360도 균등 배분 각도에 ±20도 랜덤 오차를 더해 자연스러운 배치 유도
            float angle = (360f / rockCount) * i;
            angle += Random.Range(-20f, 20f);

            // 각도를 방향 벡터로 변환
            Vector2 dir =
                Quaternion.Euler(0, 0, angle) * Vector2.right;

            // 최소 반경(50%) ~ 최대 반경(100%) 사이의 무작위 거리 산출
            float distance =
                Random.Range(rockSpawnRadius * 0.5f, rockSpawnRadius);

            // 최종 낙석 스폰 좌표 결정
            Vector2 pos =
                (Vector2)transform.position + dir * distance;

            GameObject rock =
                pool.GetBossBullet(fallingRockIndex); // 풀에서 낙석 추출

            if (rock == null)
                continue;

            rock.transform.SetParent(pool.transform); // 부모 트리 정리
            rock.transform.position = pos;           // 위치 할당
            rock.SetActive(true);                     // 가동 (경고 구역 표기 및 낙하 연출 시작)

            rocks.Add(rock); // 사망 시 청소용 리스트에 등록
        }

        yield return new WaitForSeconds(groundStayTime); // 지상 공격 노출 시간만큼 대기

        EnterUnderground(); // 패턴 주기가 끝났으므로 다시 땅속 복귀 (안전 태세)

        yield return new WaitForSeconds(patternCooldown); // 다음 무작위 턴 가동 전 부모 쿨타임 대기

        isPatternPlaying = false; // 패턴 완전히 종료
    }

    // ==========================================
    // 땅속 진입 처리 (방어/잠행 태세)
    // ==========================================
    void EnterUnderground()
    {
        if (isUnderground)
            return;

        // 1. 보스 본체 그래픽 투명화
        Color c = spriter.color;
        c.a = 0f;
        spriter.color = c;

        // 2. 바닥 연기/먼지 이펙트 가동
        if (undergroundSmoke != null)
            undergroundSmoke.SetActive(true);

        // 3. 레이어 변경 및 콜라이더 차단 (플레이어 충돌 및 피격 무력화)
        gameObject.layer = 25;

        if (col != null)
            col.enabled = false;

        isUnderground = true; // 땅속 플래그 ON
    }

    // ==========================================
    // 지상 복귀 처리 (공격/노출 태세)
    // ==========================================
    void ExitUnderground()
    {
        if (!isUnderground)
            return;

        // 1. 보스 그래픽 완전 불투명화 (출현 원상복구)
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;

        // 2. 잠행 연기 이펙트 OFF
        if (undergroundSmoke != null)
            undergroundSmoke.SetActive(false);

        // 3. 원래 오리지널 레이어 복구 및 콜라이더 재생성 (피격 가능 상태화)
        gameObject.layer = originLayer;

        if (col != null)
            col.enabled = true;

        isUnderground = false; // 땅속 플래그 OFF
    }

    // ==========================================
    // 사망 처리 오버라이드
    // ==========================================
    protected override void Dead()
    {
        ClearRocks(); // 화면에 떨어지고 있는 잔존 낙석 일괄 제거
        base.Dead();   // 부모 클래스의 사망 연출 시퀀스 가동
    }

    // 현재 맵에 가동 중인 모든 낙석 안전 풀링 반환 처리
    void ClearRocks()
    {
        for (int i = 0; i < rocks.Count; i++)
        {
            if (rocks[i] != null &&
                rocks[i].activeSelf)
            {
                rocks[i].SetActive(false); // 풀 비활성화 복귀
            }
        }

        rocks.Clear(); // 리스트 메모리 데이터 초기화
    }
}