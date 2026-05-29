using UnityEngine;
using System.Collections;

// 사막 바이옴 - 굴러다니다가 플레이어와 충돌 시 이동속도를 감속시키는 회전초
public class Tumbleweed : BiomeGimmick
{
    [Header("최소 이동 속도")]
    [SerializeField] float minMoveSpeed = 1.8f;

    [Header("최대 이동 속도")]
    [SerializeField] float maxMoveSpeed = 4.2f;

    // 현재 이동 속도
    float currentMoveSpeed;

    [Header("감속 배율")]
    [SerializeField] float slowMultiplier = 0.45f;

    [Header("감속 지속 시간")]
    [SerializeField] float slowDuration = 2f;

    // 이동 방향
    int moveDir;

    // 스프라이트
    SpriteRenderer spriter;

    // 현재 활성 여부
    bool active;

    protected override void Awake()
    {
        // 부모 초기화
        base.Awake();

        // 스프라이트 캐싱
        spriter = GetComponent<SpriteRenderer>();
    }

    protected override void OnSpawn()
    {
        active = true;

        // 랜덤 방향 설정 (-1: 왼쪽, 1: 오른쪽)
        moveDir = Random.value < 0.5f ? -1 : 1;

        // 랜덤 이동속도 설정
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        // 오른쪽 이동이면 스프라이트 반전 처리 (기존 연출 유지)
        if (spriter != null)
            spriter.flipX = moveDir == 1;
    }

    protected override void Update()
    {
        // 부모 Update 실행 (수명 체크 등)
        base.Update();

        if (!active)
            return;

        // 방향과 속도에 맞춰 매 프레임 등속 이동
        transform.position += Vector3.right * moveDir * currentMoveSpeed * Time.deltaTime;
    }

    // 플레이어 충돌 트리거
    protected override void OnPlayerTrigger(Player player)
    {
        if (!active)
            return;

        active = false;

        // [에러 해결] 컴포넌트가 사라지거나 비활성화되어도 감속 스케줄이 유지되도록 플레이어 주체로 코루틴 구동
        player.StartCoroutine(SlowPlayerRoutine(player));

        // 충돌한 회전초는 즉시 오브젝트 풀 반환 또는 비활성화
        gameObject.SetActive(false);
    }

    // 플레이어 감속 및 원복 루틴
    IEnumerator SlowPlayerRoutine(Player player)
    {
        if (player == null)
            yield break;

        // [에러 해결] 존재하지 않는 player.speed 변조 대신 변경된 moveSpeedMultiplier 배율 시스템 적용
        player.moveSpeedMultiplier *= slowMultiplier;

        // 기획된 감속 시간만큼 대기
        yield return new WaitForSeconds(slowDuration);

        if (player != null)
        {
            // [에러 해결] 지속 시간이 끝나면 디버프 배율을 역산하여 완벽하게 원래 속도로 원복
            player.moveSpeedMultiplier /= slowMultiplier;
        }
    }
}