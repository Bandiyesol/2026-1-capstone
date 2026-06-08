using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 동굴 렉스 보스 컨트롤러 (부하 소환 + 소환수 생존 시 무적 + 원형 탄막 부모 클래스 상속)
/// </summary>
public class CaveRex : BossBase
{
    [Header("소환 설정")]
    [SerializeField] int summonMonsterIndex;  // 풀링에서 사용할 부하 몬스터 인덱스
    [SerializeField] int firstSummonCount = 10; // 전투 시작 시 최초 소환 마리 수
    [SerializeField] int repeatSummonCount = 5; // 주기적으로 추가 소환할 마리 수
    [SerializeField] float summonInterval = 6f; // 추가 소환 반복 주기 (쿨타임)

    [Header("탄막 설정")]
    [SerializeField] int bulletPoolIndex; // 풀링에서 사용할 탄막 인덱스
    [SerializeField] int bulletCount = 20;   // 원형으로 발사할 탄막 개수

    [Header("범위")]
    [SerializeField] float summonRadius = 3f; // 보스 중심 기준 소환 최대 반경

    // 타이머 변수
    float summonTimer; // FixedUpdate에서 사용할 소환 주기 체크용 타이머

    // 메모리 제어 리스트 분리
    List<GameObject> summonedMonsters = new List<GameObject>(); // 현재 생존한 부하 몬스터 추적 리스트
    List<GameObject> bullets = new List<GameObject>();          // 필드 내 발사된 탄막 추적 리스트

    // 오브젝트 활성화 시 초기화 및 최초 소환
    protected override void OnEnable()
    {
        base.OnEnable();

        StopAllCoroutines(); // 잔존 코루틴 완전 차단

        summonTimer = 0f; // 타이머 초기화

        // 추적 리스트 메모리 청소
        summonedMonsters.Clear();
        bullets.Clear();

        SummonMonsters(firstSummonCount); // 전투 돌입 즉시 선제 부하 소환 (10마리)
    }

    // 오브젝트 비활성화 시 예외 처리
    void OnDisable()
    {
        StopAllCoroutines(); // 작동 중인 패턴 코루틴 중지

        ClearAllSpawned(); // 필드 내 생성물 일괄 반환 및 정리
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 부모 클래스의 물리/이동 가동

        // 사망 상태일 경우 주기적 소환 타이머 연산 스킵
        if (health <= 0)
            return;

        summonTimer += Time.fixedDeltaTime; // 물리 프레임 타임 누적 가산

        // 소환 주기 충족 시 타이머 초기화 후 반복 소환 실행
        if (summonTimer >= summonInterval)
        {
            summonTimer = 0f;
            SummonMonsters(repeatSummonCount); // 추가 소환 (5마리)
        }
    }

    // 메인 패턴 타이머 충족 시 호출 (부모 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        StartCoroutine(Pattern_CircleShot()); // 원형 사격 패턴 코루틴 실행
    }

    // [패턴 1] 보스 중심 360도 원형 확산 탄막 코루틴
    IEnumerator Pattern_CircleShot()
    {
        isPatternPlaying = true; // 패턴 재생 플래그 ON
        canMove = false;         // 패턴 집중을 위해 본체 이동 정지

        anim?.SetTrigger("Attack"); // 공격 애니메이션 재생 트리거

        yield return new WaitForSeconds(0.4f); // 투사체 생성 전 선딜레이 대기

        // 설정된 탄막 수만큼 360도 균등 분할하여 발사
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = (360f / bulletCount) * i; // 순차 각도 계산

            // 삼각함수를 사용하여 각도를 2D 진행 방향 벡터로 변환
            Vector2 dir =
                new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

            // 오브젝트 풀에서 탄막 인스턴스 추출
            GameObject bullet =
                GameManager.instance.pool.GetBossBullet(bulletPoolIndex);

            if (bullet == null)
                continue;

            bullet.transform.position = transform.position; // 보스 중심점 생성 위치 세팅

            bullet.GetComponent<BossBullet>()?.Init(dir); // 투사체 진행 방향 전달 및 초기화

            bullet.SetActive(true); // 활성화

            bullets.Add(bullet); // 사망 시 청소용 리스트 등록
        }

        yield return new WaitForSeconds(0.5f); // 패턴 종료 후 후딜레이 대기

        canMove = true;           // 이동 권한 복구
        isPatternPlaying = false; // 패턴 종료 처리
    }

    // 보스 주변 무작위 좌표에 부하 몬스터 대량 스폰
    void SummonMonsters(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 오브젝트 풀에서 적 몬스터 인스턴스 추출
            GameObject monster =
                GameManager.instance.pool.GetEnemy(summonMonsterIndex);

            if (monster == null)
                continue;

            // 0~360도 각도 및 최소 1m ~ 최대 설정 반경 사이의 무작위 거리 연산
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(1f, summonRadius);

            // 무작위 삼각함수 벡터에 거리를 곱해 오프셋 좌표 산출
            Vector2 offset =
                new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * distance;

            // 보스 현재 좌표에 오프셋 가산 후 스폰 위치 결정
            monster.transform.position =
                (Vector2)transform.position + offset;

            monster.SetActive(true); // 몬스터 활성화 (AI 가동)

            summonedMonsters.Add(monster); // 소환수 생존 체크용 리스트에 등록
        }
    }

    // 소환된 부하 몬스터들의 실시간 생존 여부 검사
    bool HasAliveSummons()
    {
        // 람다식을 활용해 파괴(null)되었거나 비활성화(풀링반환)된 객체를 리스트에서 실시간 일괄 제외
        summonedMonsters.RemoveAll(m => m == null || !m.activeSelf);

        // 제외 후 남은 카운트가 0보다 크면 부하가 아직 살아있음을 의미
        return summonedMonsters.Count > 0;
    }

    // 피격 이벤트 수신 오버라이드 (핵심 방어 기믹)
    public override void TakeDamage(float damage)
    {
        // 생존 중인 부하 소환수가 단 1마리라도 있다면 보스는 피해를 받지 않음 (무적 판정)
        if (HasAliveSummons())
            return;

        base.TakeDamage(damage); // 부하가 전멸했을 때만 부모의 실 데미지 및 사망 연산 실행
    }

    // 보스 사망 처리 오버라이드
    protected override void Dead()
    {
        ClearAllSpawned(); // 남아있는 모든 탄막과 부하 몬스터 강제 철거
        base.Dead();       // 부모 클래스의 사망 연출 및 보상 가동
    }

    // 현재 생성된 모든 소환수 및 탄막 안전 풀링 반환 처리
    void ClearAllSpawned()
    {
        // 1. 잔존 부하 몬스터 일괄 비활성화
        for (int i = 0; i < summonedMonsters.Count; i++)
        {
            if (summonedMonsters[i] != null &&
                summonedMonsters[i].activeSelf)
            {
                summonedMonsters[i].SetActive(false);
            }
        }

        // 2. 잔존 발사 탄막 일괄 비활성화
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i] != null &&
                bullets[i].activeSelf)
            {
                bullets[i].SetActive(false);
            }
        }

        // 3. 리스트 내부 메모리 데이터 초기화
        summonedMonsters.Clear();
        bullets.Clear();
    }
}