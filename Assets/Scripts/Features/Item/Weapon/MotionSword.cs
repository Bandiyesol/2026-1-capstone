using UnityEngine;

/// <summary>
/// 검과 같은 근접 휘두르기 무기의 모션을 처리하는 클래스.
/// 애니메이션 진행에 맞추어 생명 주기가 결정됩니다.
/// </summary>
public class MotionSword : Motion
{
    // 검 휘두르는 애니메이션을 재생할 컴포넌트
    private Animator animationCtrl;

    // 공격 애니메이션이 끝났는지 체크하는 플래그
    private bool isFinished = false;

    /// <summary>
    /// 생성 시 자신의 Animator 컴포넌트를 가져와 캐싱합니다.
    /// </summary>
	protected override void OnStartMotion() => animationCtrl = GetComponent<Animator>();

    // 애니메이션 기반이지만 기본 생존 시간 대비용 스폰 타임 반환
    protected override float GetDefaultTime() => instance.spawntime;

    // 검은 다수의 적을 베어야 하므로 1명 쳤다고 파괴되면 안 됨
    protected override bool ShouldDestroyOnHit() => false;

    /// <summary>
    /// 유니티 애니메이션 이벤트(Animation Event)에서 호출되어 공격 애니메이션 종료를 알립니다.
    /// </summary>
	public void OnAnimationFinished()
    {
        // 현재 실행 중인 액티브 룬이 있고, 그 룬의 연출/동작이 아직 덜 끝났다면
        if (currentActiveRune != null && currentActiveRune is IActiveDriver driver && !driver.isFinished)
        {
            // 검을 파괴하지 않고 다시 처음부터 Attack 애니메이션을 재시작하여 모션 유지
            if (animationCtrl != null) animationCtrl.Play("Attack", 0, 0f);
            return; // 파괴 로직 진입 안 함
        }

        // 액티브 룬이 없거나 끝났다면 검 휘두르기 완전 종료 처리
        isFinished = true;
        // 무기 역할 끝(애니메이션 끝)으로 자연 소멸 요청
        RequestDestroy(DestroyReason.WeaponLogic);
    }

    /// <summary>
    /// 모션이 파괴될 때, 정말 파괴해도 되는지(애니메이션이 끝났는지) 이중 검증합니다.
    /// </summary>
	protected override bool ActuallyDestroy()
    {
        // 부모의 룬 보호막 로직 검사 + 애니메이션 종료 여부까지 true여야 최종 파괴 허용
        return base.ActuallyDestroy() && isFinished;
    }
}