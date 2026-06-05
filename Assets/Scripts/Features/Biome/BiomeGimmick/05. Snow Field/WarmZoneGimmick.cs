using System.Collections;
using UnityEngine;

// 범위 안에 있으면 동결이 회복되는 구역 (모닥불, 열원 기믹 등)
public class WarmZoneGimmick : BiomeGimmick
{
    [Header("범위 비주얼 오브젝트")]
    // 실제 원 스프라이트
    Transform rangeVisual;

    // 원래 스케일
    Vector3 originScale;

    // 설원 바이옴 효과 (추위 회복 제어용 참조)
    FreezeBiomeEffect freezeEffect;

    protected override void Awake()
    {
        // 부모 Awake (기존 로직 유지)
        base.Awake();

        // 자식 Sprite 탐색
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sprite in sprites)
        {
            // 자기 자신 제외
            if (sprite.transform != transform)
            {
                rangeVisual = sprite.transform;

                // 원래 크기 저장
                originScale = rangeVisual.localScale;
                break;
            }
        }
    }

    private void Start()
    {
        // [안전장치 추가] 런타임에 freezeEffect가 비어있다면 컴포넌트나 씬에서 자동으로 찾아 안전하게 캐싱
        if (freezeEffect == null)
        {
            freezeEffect = FindFirstObjectByType<FreezeBiomeEffect>();
        }
    }

    protected override void Update()
    {
        // 부모 Update 실행 (수명 감소 로직 포함)
        base.Update();

        // 수명 없으면 중단
        if (lifeTime <= 0f)
            return;

        // 남은 수명 비율 계산 (0.0 ~ 1.0)
        float ratio = Mathf.Clamp01(currentLifeTime / lifeTime);

        // [연출 유지] 수명 비율에 따라 모닥불의 따뜻한 범위 원형 스프라이트가 부드럽게 축소됨
        if (rangeVisual != null)
        {
            rangeVisual.localScale = originScale * ratio;
        }
    }

    protected override void OnSpawn()
    {
        // 오브젝트가 생성/활성화될 때 범위 내에 이미 플레이어가 서 있는지 검사 (Overlap)
        Collider2D coll = GetComponent<Collider2D>();
        if (coll == null) return;

        Collider2D[] results = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;

        int count = coll.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = results[i];
            if (hit == null) continue;

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

    // 플레이어 진입 시 설원 바이옴 디버프에 따뜻한 구역 플래그 전달
    public void EnterPlayer()
    {
        freezeEffect?.EnterWarmZone();
    }

    // 플레이어 이탈 시 설원 바이옴 디버프에 따뜻한 구역 해제 플래그 전달
    public void ExitPlayer()
    {
        freezeEffect?.ExitWarmZone();
    }
}