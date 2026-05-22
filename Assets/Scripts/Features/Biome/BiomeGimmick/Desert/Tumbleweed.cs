using UnityEngine;
using System.Collections;

// 회전초
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

        // 랜덤 방향
        moveDir =
            Random.value < 0.5f ? -1 : 1;

        // 랜덤 이동속도
        currentMoveSpeed =
            Random.Range(
                minMoveSpeed,
                maxMoveSpeed
            );

        // 오른쪽 이동이면 반전
        if (spriter != null)
            spriter.flipX = moveDir == 1;
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();

        if (!active)
            return;

        // 앞으로 이동
        transform.position +=
            Vector3.right *
            moveDir *
            currentMoveSpeed *
            Time.deltaTime;
    }

    // 플레이어 충돌
    protected override void OnPlayerTrigger(Player player)
    {
        if (!active)
            return;

        active = false;

        // 플레이어 감속 시작
        player.StartCoroutine(
            SlowPlayerRoutine(player)
        );

        // 회전초 제거
        gameObject.SetActive(false);
    }

    // 플레이어 감속
    IEnumerator SlowPlayerRoutine(Player player)
    {
        if (player == null)
            yield break;

        // 원래 속도 저장
        float originSpeed = player.speed;

        // 이동속도 감소
        player.speed *= slowMultiplier;

        // 지속 시간 대기
        yield return new WaitForSeconds(slowDuration);

        // 속도 복구
        if (player != null)
            player.speed = originSpeed;
    }

    void OnDisable()
    {
        active = false;

        StopAllCoroutines();
    }
}