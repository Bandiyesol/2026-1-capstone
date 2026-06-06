using UnityEngine;

/// <summary>
/// 활/화살과 같은 직선 투사체 계열 무기의 움직임과 파괴 로직을 담당하는 클래스
/// </summary>
public class MotionBow : Motion
{
	// 투사체가 발사된 최초 시작 위치 (사거리 계산용)
	private Vector3 startPos;

	/// <summary>
	/// 무기(화살) 생성 시점에 최초 위치를 저장해둡니다.
	/// </summary>
	protected override void OnStartMotion()
		=> startPos = transform.position;

	/// <summary>
	/// 활 무기 데이터에 설정된 스폰 시간(유지 시간)을 기본 수명으로 설정합니다.
	/// </summary>
	protected override float GetDefaultTime()
		=> instance.spawntime;

	/// <summary>
	/// 화살은 적을 관통하지 않고 맞히면 즉시 파괴되도록 true를 반환합니다.
	/// </summary>
	protected override bool ShouldDestroyOnHit()
		=> true;

	/// <summary>
	/// 매 프레임 수명 및 사거리 초과 여부를 체크합니다.
	/// </summary>
	protected override void Update()
	{
		// 부모(Motion)의 수명 감소 및 룬 업데이트 로직 먼저 실행
		base.Update();

		// 시작 지점부터 현재 위치까지의 거리가 무기 스탯의 사거리(reach)를 넘었다면
		if (Vector2.Distance(startPos, transform.position) > instance.reach)
			// 사거리 초과로 인한 무기 자연 소멸 요청
			RequestDestroy(DestroyReason.WeaponLogic);
	}

	/// <summary>
	/// 투사체가 매 프레임 앞으로 날아가는 이동 로직입니다.
	/// </summary>
	protected override void UpdateMovement()
	{
		// 액티브 룬에 의한 특수 이동 로직이 있다면 먼저 처리
		base.UpdateMovement();

		// 액티브 룬이 이동을 제어하지 않는 순수 화살 상태라면
		if (currentActiveRune == null)
			// 현재 방향(로컬 right)을 기준으로 무기 이동속도만큼 직선 이동
			transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
	}
}