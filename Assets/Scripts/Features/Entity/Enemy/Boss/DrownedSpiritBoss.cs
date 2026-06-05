using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 익사체 영혼 보스 컨트롤러 (BossBase 상속)
/// </summary>
public class DrownedSpiritBoss : BossBase
{
    [Header("패턴 프리랩 인덱스")]
    [SerializeField] int waterDropIndex = 5; // 풀링에서 사용할 물방울 인덱스
    [SerializeField] int diveTrapIndex = 6;  // 풀링에서 사용할 잠수 함정 인덱스

    [Header("물방울 패턴 설정")]
    [SerializeField] int waterDropCount = 6;      // 기본 생성할 물방울 개수
    [SerializeField] float waterDropDistance = 4f; // 플레이어 기준 생성 반지름

    [Header("잠수 패턴 설정")]
    [SerializeField] float emergeDistance = 4f; // 플레이어 주변 재등장 거리
    [SerializeField] float maxDiveTime = 8f;    // 잠수 최대 지속 시간 (타임아웃)

    // 컴포넌트 캐싱
    SpriteRenderer sr;

    // 패턴 제어 변수
    DiveTrap currentTrap;   // 현재 생성된 잠수 함정 참조
    bool diveCaptured;      // 플레이어 포획 성공 여부

    // 메모리 관리
    List<GameObject> spawnedObjects = new List<GameObject>(); // 현재 패턴으로 생성된 오브젝트 추적 리스트

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>(); // 렌더러 컴포넌트 캐싱
    }

    // 난수 기반 패턴 시작 (기본 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        int pattern = Random.Range(0, 2); // 0 또는 1 무작위 결정

        if (pattern == 0)
            StartCoroutine(WaterDropPattern()); // 물방울 사격 패턴
        else
            StartCoroutine(DivePattern());      // 잠수 추격 패턴
    }

    // [패턴 1] 물방울 생성 코루틴
    IEnumerator WaterDropPattern()
    {
        isPatternPlaying = true; // 패턴 진행 중 플래그 ON
        canMove = false;         // 패턴 중 자체 이동 고정

        int spawnCount = waterDropCount;

        // 체력이 30% 이하일 경우 광폭화: 물방울 개수 2배 증가
        if (health <= maxHealth * 0.3f)
            spawnCount *= 2;

        anim.SetTrigger("Attack"); // 공격 애니메이션 재생

        SpawnWaterDrops(spawnCount); // 원형 물방울 생성 실행

        yield return new WaitForSeconds(0.5f); // 공격 후딜레이 대기

        canMove = true;           // 이동 능력 복구
        isPatternPlaying = false; // 패턴 종료 플래그 OFF
    }

    // 플레이어 주변 원형으로 물방울 배치
    void SpawnWaterDrops(int count)
    {
        // 예외 처리: 풀매니저나 타겟(플레이어)이 없으면 중단
        if (PoolManager.Instance == null || target == null)
            return;

        Vector2 center = target.position; // 플레이어 위치를 중심점으로 설정

        for (int i = 0; i < count; i++)
        {
            // 균등 분할 각도 계산 후 ±10도 무작위 난수 부여 (자연스러운 배치)
            float angle = (360f / count) * i;
            angle += Random.Range(-10f, 10f);

            // 각도를 방향 벡터로 변환 후 거리 곱하기
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            Vector2 spawnPos = center + dir * waterDropDistance;

            // 오브젝트 풀에서 물방울 가져오기
            GameObject obj = PoolManager.Instance.GetBossBullet(waterDropIndex);

            if (obj == null)
                continue;

            obj.transform.position = spawnPos; // 위치 지정
            obj.SetActive(true);               // 오브젝트 활성화

            spawnedObjects.Add(obj); // 페이즈 초기화용 리스트에 등록
        }
    }

    // [패턴 2] 잠수 및 함정 추격 코루틴
    IEnumerator DivePattern()
    {
        isPatternPlaying = true; // 패턴 진행 중 플래그 ON
        canMove = false;         // 보스 본체 이동 중지

        diveCaptured = false; // 포획 플래그 초기화

        SetDiveState(true); // 잠수 상태 돌입 (본체 숨김 및 충돌 비활성화)
        SpawnDiveTrap();    // 플레이어를 쫓아갈 함정 오브젝트 생성

        float diveTimer = 0f; // 타임아웃 체크용 타이머

        // 포획되지 않은 동안 루프 진행
        while (!diveCaptured)
        {
            diveTimer += Time.deltaTime;

            // 제한 시간 초과 시 루프 강제 탈출 (패턴 실패 처리)
            if (diveTimer >= maxDiveTime)
                break;

            // 함정이 플레이어를 향해 가속 이동 (보스 기본 속도의 1.5배)
            if (currentTrap != null && target != null)
            {
                currentTrap.transform.position =
                    Vector2.MoveTowards(
                        currentTrap.transform.position,
                        target.position,
                        moveSpeed * 1.5f * Time.deltaTime
                    );
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 제한 시간 초과 등으로 포획에 실패하고 루프가 끝난 경우의 예외 처리
        if (!diveCaptured)
        {
            if (currentTrap != null)
            {
                currentTrap.gameObject.SetActive(false); // 함정 비활성화
                currentTrap = null;
            }

            SetDiveState(false); // 보스 상태 복구

            canMove = true;           // 이동 플래그 복구
            isPatternPlaying = false; // 패턴 종료
        }
    }

    // 잠수 중 플레이어를 추적할 함정 생성
    void SpawnDiveTrap()
    {
        if (PoolManager.Instance == null)
            return;

        // 오브젝트 풀에서 함정 인스턴스 획득
        GameObject obj = PoolManager.Instance.GetBossBullet(diveTrapIndex);

        if (obj == null)
            return;

        obj.transform.position = transform.position; // 보스 현재 위치에서 발사

        currentTrap = obj.GetComponent<DiveTrap>(); // 스크립트 컴포넌트 추출

        if (currentTrap != null)
            currentTrap.SetOwner(this); // 함정에 보스 본체 인스턴스 전달 (콜백용)

        spawnedObjects.Add(obj); // 사망 시 정리할 리스트에 추가
    }

    // 플레이어 포획 시 함정(DiveTrap) 측에서 호출하는 콜백 메서드
    public void OnDiveCapture(Player player)
    {
        diveCaptured = true; // 루프 탈출을 위한 플래그 세팅
    }

    // 포획 연출 연타 등으로 인해 잠수 패턴이 최종 종료될 때 호출되는 메서드
    public void OnDiveEnd(Player player)
    {
        SpriteRenderer psr = player.GetComponent<SpriteRenderer>();

        // 숨겨졌던 플레이어 이미지 다시 표시
        if (psr != null)
            psr.enabled = true;

        // 플레이어 주변 무작위 원형 좌표 계산 후 보스 기습 재등장 위치 세팅
        Vector2 offset = Random.insideUnitCircle.normalized * emergeDistance;
        transform.position = player.transform.position + (Vector3)offset;

        SetDiveState(false); // 보스 가시화 및 충돌 활성화

        canMove = true;           // 보스 자유 이동 권한 복구
        isPatternPlaying = false; // 패턴 완전 종료 처리

        currentTrap = null; // 참조 초기화
    }

    // 잠수 상태에 따른 본체 ON/OFF 제어
    void SetDiveState(bool diving)
    {
        if (sr != null)
            sr.enabled = !diving; // 참이면 하이드, 거짓이면 쇼

        if (col != null)
            col.enabled = !diving; // 잠수 중에는 플레이어 공격 및 충돌 면역
    }

    // 보스 사망 시 호출 (오버라이드)
    protected override void Dead()
    {
        ClearSpawnedObjects(); // 필드에 남아있는 물방울/함정 강제 제거

        base.Dead(); // 부모 클래스의 사망 로직(연출, 보상 등) 실행
    }

    // 생성된 모든 하위 오브젝트 풀링 반환 및 메모리 정리
    void ClearSpawnedObjects()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
                spawnedObjects[i].SetActive(false); // 풀링 비활성화 복귀
        }

        spawnedObjects.Clear(); // 리스트 비우기
        currentTrap = null;     // 참조 제거
    }
}