using UnityEngine;

public class MotionBoomerang : Motion
{
	const float BaseCatchDistance = 0.45f;
	const float CatchDistancePerSize = 0.4f;
	const float OutboundReachSlack = 1.05f;
	const float ReturnTimeSlack = 1.75f;
	const float OutboundTimeSlack = 1.35f;

	Vector3 startPos;
	Transform owner;
	bool isReturning;
	float outboundSpeed;
	float returnSpeed;
	float outboundElapsed;
	float returningElapsed;

	protected override void OnStartMotion()
	{
		startPos = transform.position;
		isReturning = false;
		outboundElapsed = 0f;
		returningElapsed = 0f;
		outboundSpeed = instance.movespeed * 1.15f;
		returnSpeed = Mathf.Max(instance.movespeed * 0.75f, outboundSpeed * 0.55f);
		ResolveOwner();
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;

	protected override void Update()
	{
		base.Update();
		if (IsDestroyed || instance == null)
			return;

		ResolveOwner();

		if (isReturning)
		{
			returningElapsed += Time.deltaTime;

			if (ShouldCatch())
				RequestCatchDestroy();
			else if (returningElapsed >= ResolveMaxReturnTime())
				RequestCatchDestroy();

			return;
		}

		outboundElapsed += Time.deltaTime;

		if (Vector2.Distance(startPos, transform.position) >= instance.reach * OutboundReachSlack)
			BeginReturn();
		else if (outboundElapsed >= ResolveMaxOutboundTime())
			BeginReturn();

		// 수명이 끝났는데 액티브 룬 때문에 base 파괴가 막힌 경우 대비
		if (life <= 0f)
			RequestCatchDestroy();
	}

	protected override void UpdateMovement()
	{
		// 복귀 중에는 액티브 룬 이동보다 플레이어 쪽 복귀를 우선합니다.
		if (isReturning)
		{
			MoveTowardOwner();
			return;
		}

		if (currentActiveRune is IActiveDriver driver && !driver.isFinished)
		{
			base.UpdateMovement();
			return;
		}

		MoveOutbound();
	}

	void BeginReturn()
	{
		if (isReturning)
			return;

		isReturning = true;
		returningElapsed = 0f;
	}

	void MoveOutbound()
	{
		float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
		Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		if (direction.sqrMagnitude < 0.0001f)
			direction = Vector2.right;

		transform.Translate((Vector3)(direction * outboundSpeed * Time.deltaTime), Space.World);
	}

	void MoveTowardOwner()
	{
		if (owner == null)
		{
			RequestCatchDestroy();
			return;
		}

		Vector2 toOwner = (Vector2)owner.position - (Vector2)transform.position;
		float distance = toOwner.magnitude;
		if (distance <= 0.001f)
		{
			RequestCatchDestroy();
			return;
		}

		Vector2 direction = toOwner / distance;
		float speed = returnSpeed;
		// 가까워질수록 빨리 붙어서 고정 catch 거리 문제를 줄입니다.
		if (distance < ResolveCatchDistance() * 2f)
			speed = Mathf.Max(speed, distance / Mathf.Max(Time.deltaTime, 0.001f) * 0.5f);

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
		transform.position += (Vector3)(direction * speed * Time.deltaTime);
	}

	bool ShouldCatch()
	{
		if (owner == null)
			return true;

		return Vector2.Distance(owner.position, transform.position) <= ResolveCatchDistance();
	}

	float ResolveCatchDistance()
	{
		float size = instance != null ? instance.size : 1f;
		return BaseCatchDistance + size * CatchDistancePerSize;
	}

	float ResolveMaxOutboundTime()
	{
		float speed = Mathf.Max(0.01f, outboundSpeed);
		float reach = instance != null ? instance.reach : 5f;
		return reach / speed * OutboundTimeSlack + 0.25f;
	}

	float ResolveMaxReturnTime()
	{
		float speed = Mathf.Max(0.01f, returnSpeed);
		float reach = instance != null ? instance.reach : 5f;
		return reach / speed * ReturnTimeSlack + 0.5f;
	}

	void ResolveOwner()
	{
		if (owner != null)
			return;

		if (PlayerStats.Instance != null)
			owner = PlayerStats.Instance.transform;
	}

	void RequestCatchDestroy()
	{
		if (IsDestroyed)
			return;

		// Homing 등 액티브 룬이 남아 있으면 WeaponLogic 파괴가 막히므로 먼저 해제합니다.
		currentActiveRune = null;
		RequestDestroy(DestroyReason.WeaponLogic);
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		isReturning = false;
		startPos = Vector3.zero;
		owner = null;
		outboundSpeed = 0f;
		returnSpeed = 0f;
		outboundElapsed = 0f;
		returningElapsed = 0f;
	}
}
