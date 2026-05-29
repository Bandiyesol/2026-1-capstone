using System.Collections;
using UnityEngine;

// 공허 낙사 기믹
public class VoidPitGimmick : BiomeGimmick
{
    [Header("경고 원")]
    [SerializeField] GameObject warningZone;

    [Header("함정 스프라이트")]
    [SerializeField] GameObject pitVisual;

    [Header("히트박스")]
    [SerializeField] Collider2D hitCollider;

    [Header("경고 시간")]
    [SerializeField] float warningTime = 1.5f;

    protected override void OnSpawn()
    {
        // 코루틴 중복 실행 방지 및 상태 초기화
        StopAllCoroutines();

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
        // 경고 대기 (기존 1.5초 수치 유지)
        yield return new WaitForSeconds(warningTime);

        // 경고 끄기
        if (warningZone != null)
            warningZone.SetActive(false);

        // 함정 표시
        if (pitVisual != null)
            pitVisual.SetActive(true);
    }

    // [추가 - 에러 해결] 부모 클래스(BiomeGimmick)의 필수 추상 멤버 구현
    protected override void OnPlayerTrigger(Player player)
    {
        // 하위 오브젝트인 VoidPitHitbox의 트리거를 통해 주로 작동하지만,
        // 본체 콜라이더에 직접 닿았을 때도 예외 없이 낙사 처리되도록 안전하게 연동합니다.
        HitPlayer(player);
    }

    // 플레이어 낙사 처리
    public void HitPlayer(Player player)
    {
        if (player == null) return;

        // 낙하는 순간 물리 속도와 입력 벡터를 강제로 제로화하여 제어권 상실 연출 보강
        player.inputVec = Vector2.zero;
        player.externalVelocity = Vector2.zero;

        // 스프라이트 끄기
        if (player.spriter != null)
        {
            player.spriter.enabled = false;
        }

        // 체력 제거 및 즉시 사망 파이프라인 가동 (기존 기믹 유지)
        if (GameManager.instance != null)
        {
            GameManager.instance.Health = 0f;
        }
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
}