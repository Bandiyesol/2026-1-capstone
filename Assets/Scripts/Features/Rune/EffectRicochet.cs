using UnityEngine;

// 벽이나 적에게 닿았을 때 튕기는 트리거 룬 효과
public class EffectRicochet : RuneEffect, ITriggerEffect
{
	private int maxCount;          // 최대 튕김 허용 횟수
	private int currentCount;      // 현재 남은 튕김 횟수

	// 설정에 따라 실행 후 투사체를 파괴할지 여부
	public bool DestroyOnExecute => data.isDestroyed;
	// 도탄 횟수가 남아있으면 부모 무기의 소멸(파괴)을 방지함
	public bool ProtectParent => currentCount > 0;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		maxCount = RuneDataAccess.GetBounceCount(data);
		currentCount = 0; // 초기에는 0, 첫 충돌 시 활성화
	}

	private void Update()
	{
		// 남은 횟수가 없으면 쿨타임을 돌려 다음 도탄을 준비
		if (currentCount <= 0) UpdateCooltime();
	}

	// 물리적 충돌/반사 발생 시 호출됨
	public void OnReflect(Collider2D collision)
	{
		// 연쇄 도탄이 진행 중인 경우
		if (currentCount > 0)
		{
			PerformPhysicalReflect(collision);
			currentCount--;

			if (currentCount <= 0) ResetCooltime();
			return;
		}

		// 쿨타임이 끝났고 새롭게 도탄을 시작할 수 있는 경우
		if (isReady)
		{
			currentCount = maxCount;
			PerformPhysicalReflect(collision);
			currentCount--;

			if (currentCount <= 0) ResetCooltime();
		}
	}

	// 실제 물리 반사각을 계산하여 무기의 방향을 꺾는 함수
	private void PerformPhysicalReflect(Collider2D collision)
	{
		Vector2 incoming = transform.right; // 현재 진행 방향
		Vector2 hitPoint = collision.ClosestPoint(transform.position); // 충돌 지점
		Vector2 normal = ((Vector2)transform.position - hitPoint).normalized; // 충돌면의 법선(수직) 벡터

		// 입사 벡터와 법선 벡터를 사용해 반사 벡터 계산 (물리 엔진 함수)
		Vector2 reflect = Vector2.Reflect(incoming, normal);

		// 반사된 방향으로 무기 회전
		float angle = Mathf.Atan2(reflect.y, reflect.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, angle);
	}
}