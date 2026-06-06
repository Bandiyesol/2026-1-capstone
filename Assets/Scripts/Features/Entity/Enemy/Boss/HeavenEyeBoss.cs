using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 천공의 눈 보스 컨트롤러 (부채꼴 사격 + 투명 순간이동 부모 클래스 상속)
/// </summary>
public class HeavenEyeBoss : BossBase
{
    // 패턴 종류 정의 (직관성을 위한 열거형)
    enum Pattern
    {
        SpreadShot,
        InvisibleShot
    }

    [Header("탄막")]
    [SerializeField] int bulletIndex;         // 풀링에서 사용할 탄막 인덱스
    [SerializeField] int spreadCount = 3;     // 발사할 탄막 개수 (갈래 수)
    [SerializeField] float angleGap = 20f;     // 탄막 사이의 벌어질 각도 간격
    [SerializeField] float shotDelay = 0.15f;  // 탄막 간의 순차 발사 지연 시간
    [SerializeField] float spawnDistance = 1.2f;// 보스 중심 기준 생성 거리

    [Header("투명 순간이동")]
    [SerializeField] float invisibleTime = 3f;      // 투명 상태 총 유지 시간
    [SerializeField] float teleportDelay = 0.6f;     // 순간이동 주기 간격
    [SerializeField] float teleportDistance = 4f;    // 플레이어 중심 타겟팅 거리
    [SerializeField] float teleportRandomOffset = 1.5f;// 도착지 주변 랜덤 오차 반경

    // 매니저 및 메모리 관리
    PoolManager pool;
    List<GameObject> bullets = new List<GameObject>(); // 사망 시 정리를 위한 탄막 추적 리스트

    // 오브젝트 활성화 시 초기화
    protected override void OnEnable()
    {
        base.OnEnable();

        // 이전 패턴에서 투명해진 보스 투명도(알파값) 완전 복구
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;

        bullets.Clear(); // 추적 리스트 초기화
    }

    protected override void Start()
    {
        pool = GameManager.instance.pool; // 풀 매니저 싱글톤 참조 캐싱
    }

    // 메인 패턴 타이머 충족 시 호출 (부모 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        int rand = Random.Range(0, 2); // 0 또는 1 난수 추출

        if (rand == 0)
            StartCoroutine(PatternSpreadShot()); // 부채꼴 순차 확산 사격
        else
            StartCoroutine(PatternInvisibleShot()); // 투명 난무 순간이동
    }

    // [패턴 1] 플레이어 방향 부채꼴 순차 사격 코루틴
    IEnumerator PatternSpreadShot()
    {
        if (isPatternPlaying) yield break; // 중복 발동 예외 차단

        isPatternPlaying = true; // 패턴 플래그 ON
        canMove = false;         // 패턴 집중을 위해 자체 이동 고정

        yield return new WaitForSeconds(0.3f); // 공격 전 선딜레이 대기

        // 플레이어 부재 시 패턴 강제 종료 및 복구
        if (target == null)
        {
            isPatternPlaying = false;
            canMove = true;
            yield break;
        }

        Vector2 myPos = transform.position;
        Vector2 baseDir = (target.position - (Vector3)myPos).normalized; // 타겟 정방향 벡터

        // 기본 조준 각도 계산 (라디안 -> 디그리 단위 변환)
        float baseAngle =
            Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // 설정된 갈래 수만큼 루프를 돌며 순차 발사
        for (int i = 0; i < spreadCount; i++)
        {
            // 중앙 조준선을 기준으로 좌우 균등 분할 오프셋 각도 계산
            float angleOffset =
                (-angleGap * (spreadCount - 1) / 2f) + (angleGap * i);

            float finalAngle = baseAngle + angleOffset; // 오프셋이 적용된 최종 탄막 진행 각도

            // 삼각함수를 이용하여 각도를 진행 방향 벡터로 환원
            Vector2 shotDir =
                new Vector2(
                    Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                    Mathf.Sin(finalAngle * Mathf.Deg2Rad));

            GameObject bullet =
                pool.GetBossBullet(bulletIndex); // 풀에서 투사체 추출

            if (bullet == null)
                continue;

            bullet.transform.SetParent(null); // 월드 좌표계 독립

            // 보스 중심에서 발사 거리만큼 진행 방향으로 오프셋 연산 후 배치
            Vector3 spawnPos =
                myPos + (shotDir * spawnDistance);

            spawnPos.z = 0f; // 2D z축 고정

            bullet.transform.position = spawnPos;

            bullet.GetComponent<BossBullet>()?.Init(shotDir); // 투사체 속도 및 방향 초기화

            bullet.SetActive(true); // 활성화

            bullets.Add(bullet); // 사망 시 청소용 리스트에 등록

            yield return new WaitForSeconds(shotDelay); // 한 발 쏠 때마다 설정된 지연 대기
        }

        yield return new WaitForSeconds(1f); // 패턴 완료 후 후딜레이

        canMove = true;           // 이동 권한 복구
        isPatternPlaying = false; // 패턴 종료 처리
    }

    // [패턴 2] 투명화 상태 돌입 후 플레이어 주변 고속 텔레포트 코루틴
    IEnumerator PatternInvisibleShot()
    {
        if (isPatternPlaying) yield break; // 중복 발동 제한

        isPatternPlaying = true; // 패턴 플래그 ON

        // 보스 스프라이트 알파값을 극소치로 낮추어 반투명(투명) 상태 연출
        Color c = spriter.color;
        c.a = 0.01f;
        spriter.color = c;

        float timer = 0f; // 경과 시간 체크용 타이머

        // 설정된 총 유지 시간에 도달할 때까지 루프 반복
        while (timer < invisibleTime)
        {
            TeleportAroundTarget(); // 플레이어 주변으로 즉시 위치 이동

            yield return new WaitForSeconds(teleportDelay); // 순간이동 주기만큼 대기

            timer += teleportDelay; // 타이머 누적 연산
        }

        // 패턴 종료 후 보스 알파값 완전 복구 (불투명 상태화)
        c.a = 1f;
        spriter.color = c;

        isPatternPlaying = false; // 패턴 해제 (이동 및 다음 패턴 가능화)
    }

    // 플레이어 주변 무작위 구역으로 순간이동 연산
    void TeleportAroundTarget()
    {
        if (target == null)
            return;

        // 원형 테두리 상의 무작위 방향 벡터 추출
        Vector2 dir = Random.insideUnitCircle.normalized;

        // 지정된 텔레포트 거리에 무작위 오차 가중치를 더해 최종 상대 좌표 결정
        Vector2 offset =
            dir * teleportDistance +
            Random.insideUnitCircle * teleportRandomOffset;

        // 플레이어 현재 위치에 오프셋 좌표 누적 가산
        Vector3 nextPos = target.position + (Vector3)offset;
        nextPos.z = 0f; // 2D z축 고정

        transform.position = nextPos; // 보스 위치 강제 갱신
    }

    // ==========================================
    // 사망 처리 오버라이드
    // ==========================================
    protected override void Dead()
    {
        ClearBullets(); // 필드에 남아 날아다니는 탄막 강제 비활성화
        base.Dead();     // 부모 클래스의 사망 연출 및 시퀀스 작동
    }

    // 현재 추적 중인 리스트 내 활성화된 모든 탄막 풀링 반환 처리
    void ClearBullets()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i] != null &&
                bullets[i].activeSelf)
            {
                bullets[i].SetActive(false); // 풀링 비활성화 복귀
            }
        }

        bullets.Clear(); // 리스트 내부 메모리 초기화
    }
}