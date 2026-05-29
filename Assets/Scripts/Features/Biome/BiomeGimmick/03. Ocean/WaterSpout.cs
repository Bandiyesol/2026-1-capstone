using UnityEngine;
using System.Collections;

// 물기둥 기믹 (주변 플레이어를 흡입하고, 내부 진입 시 지속적인 틱 대미지 부여)
public class WaterSpout : BiomeGimmick
{
    [Header("경고 및 수명")]
    [SerializeField] float warningTime = 1f;

    [Header("흡입 설정")]
    [SerializeField] float suctionRange = 5f;
    [SerializeField] float suctionForce = 3f;

    [Header("지속 틱 데미지 설정")]
    [SerializeField] float tickDamage = 10f;
    [SerializeField] float damageInterval = 0.5f;

    [Header("오브젝트 연결")]
    [SerializeField] GameObject redCircle;
    [SerializeField] GameObject waterspoutObject;

    // 내부 상태 변수
    private bool isPlayerInside = false;
    private Coroutine damageCoroutine = null;

    protected override void Update()
    {
        // 부모 Update 실행 (수명 관리 등)
        base.Update();
    }

    protected override void OnSpawn()
    {
        // 상태 완전히 초기화 후 재생성 루틴 가동
        StopAllCoroutines();
        isPlayerInside = false;
        damageCoroutine = null;

        if (redCircle != null) redCircle.SetActive(true);
        if (waterspoutObject != null) waterspoutObject.SetActive(false);

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // 장판 경고 시간 대기
        yield return new WaitForSeconds(warningTime);

        if (redCircle != null) redCircle.SetActive(false);
        if (waterspoutObject != null) waterspoutObject.SetActive(true);

        // 물기둥이 유지되는 동안 주변 플레이어를 끌어당기는 물리 루프 시작
        while (gameObject.activeSelf)
        {
            ApplySuction();
            yield return new WaitForFixedUpdate(); // 물리 연산 동기화
        }
    }

    // 주변 플레이어를 중심점으로 끌어당기는 함수 (기존 물리 로직 유지)
    void ApplySuction()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, suctionRange);
        foreach (Collider2D hitCollider in hits)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Player player = hitCollider.GetComponent<Player>();
                if (player != null)
                {
                    // 중심 방향 벡터 계산 후 플레이어 외력(externalVelocity)에 인력 주입
                    Vector2 suctionDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                    player.externalVelocity = suctionDir * suctionForce;
                }
            }
        }
    }

    // --- WaterSpoutEvent 컴포넌트에서 호출하는 진입 이벤트 핸들 ---
    public void OnWaterspoutEnter(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(TickDamageRoutine());
            }
        }
    }

    public void OnWaterspoutExit(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    // 내부 지속 피해 루틴
    IEnumerator TickDamageRoutine()
    {
        while (isPlayerInside)
        {
            // [방어 시스템 연동] 체력을 무조건 깎던 방식에서 PlayerStats 방어 수치 및 회피가 적용되도록 개선
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TakeDamage(tickDamage);
            }
            else
            {
                GameManager.instance.Health -= tickDamage; // 폴백 구조 유지
            }

            yield return new WaitForSeconds(damageInterval);
        }
        damageCoroutine = null;
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 부모 가상 메서드 의무 구현 (진입처리는 하위 콜라이더 Event스크립트가 전담)
    }

    void OnDisable()
    {
        // 오브젝트 풀 반환 및 비활성화 시 코루틴 안전 정리
        isPlayerInside = false;
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        StopAllCoroutines();
    }
}