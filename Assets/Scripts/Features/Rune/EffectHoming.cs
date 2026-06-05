using UnityEngine;

// 대상을 향해 유도되는 룬 효과 (매 프레임 위치를 업데이트하는 IActiveDriver 구현)
public class EffectHoming : RuneEffect, IActiveDriver
{
	private Transform target;       // 유도할 대상(적)
	private float elapsedtime;      // 효과 지속 시간 추적용
	private float searchtime;       // 타겟 탐색 쿨타임 추적용

	// 경과 시간이 룬의 지속 시간을 초과하면 효과 종료
	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		elapsedtime = 0f;
		searchtime = 0f;
		FindTarget(); // 초기화 시 바로 첫 타겟 탐색
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;
		searchtime += Time.deltaTime;

		// 타겟이 없거나, 타겟이 죽었을/비활성화되었을 경우
		if (target == null || !target.gameObject.activeInHierarchy)
		{
			// 0.2초마다 새로운 타겟을 탐색하여 부하를 줄임
			if (searchtime >= 0.2f)
			{
				FindTarget();
				searchtime = 0f;
			}
		}

		// 타겟을 못 찾았다면 그냥 정면(오른쪽)으로 직진
		if (target == null)
		{
			transform.Translate(Vector3.right * weapon.movespeed * Time.deltaTime);
			return;
		}

		// 타겟을 향한 방향 벡터 계산
		Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
		// 방향 벡터를 각도(도)로 변환
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

		// 무기의 기본 속도에 룬의 속도 배율 적용
		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		float rotationSpeed = weapon.movespeed * (speedMultiplier > 0f ? speedMultiplier : 1f);

		// 타겟 방향으로 부드럽게 회전 (Slerp)
		transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

		// 회전된 정면 방향으로 전진
		transform.Translate(Vector3.right * weapon.movespeed * Time.deltaTime);

		// 타겟에 충분히 가까워지면 타겟 해제 (새로운 타겟을 찾거나 명중 처리)
		if (Vector2.Distance(transform.position, target.position) < 0.2f) target = null;
	}

	private void FindTarget()
	{
		// 룬 데이터에서 탐색 범위 가져오기 (기본값 10f)
		float range = RuneDataAccess.GetAffectedRange(data);
		float radius = range > 0f ? range : 10f;

		// 반경 내의 'Enemy' 레이어를 가진 모든 콜라이더 검출
		Collider2D[] Enemies = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));

		float minDistance = Mathf.Infinity;
		Transform nearest = null;

		// 검출된 적들 중 가장 가까운 적 찾기
		foreach (var enemy in Enemies)
		{
			float distance = Vector2.Distance(transform.position, enemy.transform.position);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearest = enemy.transform;
			}

			// 가장 가까운 적을 타겟으로 설정
			target = nearest;
		}
	}
}