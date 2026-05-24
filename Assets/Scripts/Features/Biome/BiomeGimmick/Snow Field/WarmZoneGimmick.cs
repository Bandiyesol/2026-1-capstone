using System.Collections;
using UnityEngine;

// 범위 안에 있으면 동결이 회복되는 구역
public class WarmZoneGimmick : BiomeGimmick
{
    [Header("범위 비주얼 오브젝트")]
    // 실제 원 스프라이트
    Transform rangeVisual;

    // 원래 스케일
    Vector3 originScale;

    // 설원 바이옴 효과
    FreezeBiomeEffect freezeEffect;

    protected override void Awake()
    {
        // 부모 Awake
        base.Awake();

        // 자식 Sprite 탐색
        SpriteRenderer[] sprites =
            GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sprite in sprites)
        {
            // 자기 자신 제외
            if (sprite.transform != transform)
            {
                rangeVisual = sprite.transform;

                // 원래 크기 저장
                originScale =
                    rangeVisual.localScale;

                break;
            }
        }
    }

    protected override void Update()
    {
        // 부모 Update
        base.Update();

        // 수명 없으면 중단
        if (lifeTime <= 0f)
            return;

        // 남은 비율
        float ratio =
            Mathf.Clamp01(
                currentLifeTime / lifeTime
            );

        // 원 축소
        if (rangeVisual != null)
        {
            rangeVisual.localScale =
                originScale * ratio;
        }
    }

    protected override void OnSpawn()
    {
        // 수명 초기화
        currentLifeTime = lifeTime;

        // 크기 초기화
        if (rangeVisual != null)
        {
            rangeVisual.localScale =
                originScale;
        }

        // FreezeBiomeEffect 탐색
        StartCoroutine(FindFreezeEffect());
    }

    IEnumerator FindFreezeEffect()
    {
        // 연결 대기
        yield return null;

        // 부모에서 탐색
        freezeEffect =
            transform.parent
            .GetComponentInParent<FreezeBiomeEffect>();

        // 플레이어 재검사
        CheckPlayerInside();
    }

    // 플레이어 재검사
    void CheckPlayerInside()
    {
        if (coll == null)
            return;

        Collider2D[] results =
            new Collider2D[10];

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.useTriggers = true;

        int count =
            coll.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = results[i];

            if (hit == null)
                continue;

            if (hit.CompareTag("Player"))
            {
                EnterPlayer();
                return;
            }
        }
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 플레이어 진입 처리
        EnterPlayer();
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // 플레이어 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        // 계속 회복 상태 유지
        EnterPlayer();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        // 회복 상태 해제
        ExitPlayer();
    }

    // 플레이어 진입
    public void EnterPlayer()
    {
        freezeEffect?.EnterWarmZone();
    }

    // 플레이어 이탈
    public void ExitPlayer()
    {
        freezeEffect?.ExitWarmZone();
    }

    void OnDisable()
    {
        // 상태 해제
        freezeEffect?.ExitWarmZone();

        // 크기 원복
        if (rangeVisual != null)
        {
            rangeVisual.localScale =
                originScale;
        }
    }
}