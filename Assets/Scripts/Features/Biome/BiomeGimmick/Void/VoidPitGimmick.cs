using System.Collections;
using UnityEngine;

// 공허 낙사 기믹
public class VoidPitGimmick : BiomeGimmick
{
    [Header("경고 원")]
    [SerializeField]
    GameObject warningZone;

    [Header("함정 스프라이트")]
    [SerializeField]
    GameObject pitVisual;

    [Header("히트박스")]
    [SerializeField]
    Collider2D hitCollider;

    [Header("경고 시간")]
    [SerializeField]
    float warningTime = 1.5f;

    protected override void OnSpawn()
    {
        // 경고 표시
        if (warningZone != null)
            warningZone.SetActive(true);

        // 함정 숨기기
        if (pitVisual != null)
            pitVisual.SetActive(false);

        // 충돌 끄기
        DisableHitbox();

        // 시작
        StartCoroutine(ActivateRoutine());
    }

    IEnumerator ActivateRoutine()
    {
        // 경고 대기
        yield return new WaitForSeconds(
            warningTime
        );

        // 경고 끄기
        if (warningZone != null)
            warningZone.SetActive(false);

        // 함정 표시
        if (pitVisual != null)
            pitVisual.SetActive(true);
    }

    // 플레이어 낙사
    public void HitPlayer(Player player)
    {
        // 스프라이트 끄기
        if (player.spriter != null)
        {
            player.spriter.enabled = false;
        }

        // 체력 제거
        GameManager.instance.Health = 0f;
    }

    // 충돌 켜기
    public void EnableHitbox()
    {
        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }
    }

    // 충돌 끄기
    public void DisableHitbox()
    {
        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }
    }

    protected override void OnPlayerTrigger(Player player) { }

    void OnDisable()
    {
        // 경고 복구
        if (warningZone != null)
            warningZone.SetActive(true);

        // 함정 숨기기
        if (pitVisual != null)
            pitVisual.SetActive(false);

        // 충돌 끄기
        DisableHitbox();
    }
}