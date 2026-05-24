using UnityEngine;

// 바이옴 지속 효과 공통 부모
public abstract class BiomeEffect : MonoBehaviour
{
    // 플레이어 캐싱
    protected Player player;

    // 현재 효과 적용 여부
    protected bool applied;

    protected virtual void Awake()
    {
        if (GameManager.instance != null)
            player = GameManager.instance.player;
    }

    void Update()
    {
        // 게임 매니저 없으면 중단
        if (GameManager.instance == null)
            return;

        // 플레이어 재캐싱
        if (player == null)
            player = GameManager.instance.player;

        // 게임 진행 중인데 아직 적용 안됨
        if (GameManager.instance.isLive &&
            !applied)
        {
            ApplyEffect();
            applied = true;
        }

        // 게임 정지 중인데 적용 상태면 해제
        else if (!GameManager.instance.isLive &&
                 applied)
        {
            RemoveEffect();
            applied = false;
        }

        // 적용 중일 때만 지속 효과 실행
        if (applied)
            EffectUpdate();
    }

    void OnDisable()
    {
        if (applied)
        {
            RemoveEffect();
            applied = false;
        }
    }

    // 효과 적용
    protected abstract void ApplyEffect();

    // 효과 해제
    protected abstract void RemoveEffect();

    // 지속 실행 로직
    protected virtual void EffectUpdate() { }
}