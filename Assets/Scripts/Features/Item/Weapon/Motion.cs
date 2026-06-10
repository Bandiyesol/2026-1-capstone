using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 무기(투사체/타격체) 파괴가 요청된 이유를 구분하기 위한 열거형
/// </summary>
public enum DestroyReason
{
	WeaponLogic,   // 무기 자체의 수명 다함, 혹은 애니메이션 종료 등 고유 로직에 의한 파괴
	TriggerRune    // 적 타격 시 발동되는 트리거 룬이 발동을 마치고 스스로 파괴를 요청할 때
}

/// <summary>
/// 모든 무기 모션(검, 활, 오브 등)의 최상위 공통 부모 클래스입니다.
/// 무기의 생존 시간, 룬 적용, 충돌(데미지) 처리 및 파괴 로직을 담당합니다.
/// </summary>
public abstract class Motion : MonoBehaviour
{
	[Header("[ 설정 ]")]

	// 현재 생성된 무기의 고유 스탯 및 정보 데이터
	public WeaponInstance instance;

	// 무기에 장착된 전체 룬 데이터 리스트
	protected List<RuneData> allRunes;

	// 상태 변화나 로직을 지속적으로 적용하는 룬(패시브 등) 효과 리스트
	protected List<RuneEffect> persistentEffects = new List<RuneEffect>();

	// 현재 발동되어 실행 중인 단발성/액티브 룬 효과
	protected RuneEffect currentActiveRune;

	// 현재 실행 중인 액티브 룬의 순서/인덱스 (다수의 액티브 룬이 있을 경우 순차 실행용)
	protected int activeIndex = -1;

	// 무기(투사체)가 맵에 존재할 수 있는 남은 시간
	protected float life = 0f;

	// 무기 초기화(Initialize)가 정상적으로 완료되었는지 체크하는 플래그
	protected bool isInitialLifeSet = false;

	// 파괴가 여러 번 호출되어 에러가 발생하는 것을 막기 위한 중복 방지 플래그
	private bool isDestroyRequested = false;

	/// <summary>
	/// 무기가 생성될 때 최초로 호출되어 스탯과 룬을 세팅합니다.
	/// </summary>
	public virtual void Initialize(WeaponInstance instance, List<RuneData> runes, float inheritedLifeTime = -1f)
	{
		this.instance = instance;

		// 외부에서 받은 룬 리스트를 깊은 복사(새 리스트 할당)하여 보관
		allRunes = new List<RuneData>(runes);

		// WeaponInstance에 정의된 무기 크기 스탯(size)을 실제 Transform Scale에 적용
		transform.localScale = new Vector3(instance.size, instance.size, 1f);

		// 상속받은(외부에서 지정한) 수명이 있다면 적용하고, 없으면 각 무기별 기본 수명을 가져옴
		if (inheritedLifeTime > 0f) life = inheritedLifeTime;
		else life = GetDefaultTime();

		// 초기화 완료 플래그 켜기 (이후 Update문 실행 가능)
		isInitialLifeSet = true;

		// 자식 클래스(검, 활 등)에서 정의한 시작 시점 특수 처리 실행
		OnStartMotion();

		// 지속형(State, Logic) 룬과 충돌 반응형(Trigger) 룬 세팅
		SetupPersistentRunes();
		SetTriggerRunes();

		// 장착된 첫 번째 액티브 룬을 바로 실행
		ExecuteActiveRune();
	}

	/// <summary>
	/// 매 프레임 무기의 수명을 깎고, 이동 및 룬 효과를 업데이트합니다.
	/// </summary>
	protected virtual void Update()
	{
		// 초기화가 안 되었다면 Update 로직을 돌리지 않음
		if (!isInitialLifeSet) return;

		// 프레임 경과 시간만큼 생존 시간 차감
		life -= Time.deltaTime;

		// 생존 시간이 0 이하가 되면 무기 고유 로직에 의한 파괴 요청
		if (life <= 0f)
			RequestDestroy(DestroyReason.WeaponLogic);

		// 투사체 이동 처리 (활 등에서 오버라이드 됨)
		UpdateMovement();

		// 지속 룬(상태/로직) 매 프레임 업데이트 적용
		foreach (var effect in persistentEffects)
		{
			// 플레이어/무기 상태에 영향을 주는 룬 업데이트
			if (effect is IStateEffect state)
				state.UpdateState();

			// 특수 로직(쿨타임 감소, 스택 쌓기 등) 업데이트
			if (effect is ILogicEffect logic)
				logic.UpdateLogic();
		}
	}

	// [자식 클래스 구현 필수] 생성 시 무기별 특별한 처리가 필요할 때 구현 (예: 애니메이터 연결, 초기 위치 저장)
	protected abstract void OnStartMotion();

	// [자식 클래스 구현 필수] 해당 무기 타입의 기본 생존 시간 반환 (예: instance.spawntime)
	protected abstract float GetDefaultTime();

	/// <summary>
	/// 유니티 기본 2D 물리 충돌 감지 (Trigger 설정 필요)
	/// </summary>
	protected virtual void OnTriggerEnter2D(Collider2D collision)
		=> HandleCollision(collision);

	// 타격 후 투사체가 관통할지 파괴될지 결정하는 가상 메서드 (기본은 관통/파괴 안됨)
	protected virtual bool ShouldDestroyOnHit() => false;

	/// <summary>
	/// 실제로 파괴(Destroy)가 가능한지 체크합니다. (룬 효과에 의해 보호받을 수 있음)
	/// </summary>
	protected virtual bool ActuallyDestroy()
	{
		// 현재 이 무기 게임오브젝트에 붙어있는 트리거 룬 효과들 찾기
		var triggerEffects = GetComponents<RuneEffect>().OfType<ITriggerEffect>();

		foreach (var trigger in triggerEffects)
		{
			// 트리거 룬 중 하나라도 부모(무기)의 파괴를 막고 있다면 파괴 불가 판정
			if (trigger.ProtectParent)
				return false;
		}

		return true; // 아무도 보호하지 않는다면 파괴 가능
	}

	// 외부에서 현재 무기의 남은 생존 시간을 확인할 때 사용
	public float GetRemainingLife() => life;

	// 외부에서 이 무기에 달린 룬 데이터들을 복사해서 가져갈 때 사용
	public List<RuneData> GetRunes()
	{
		return new List<RuneData>(allRunes);
	}

	/// <summary>
	/// 무기의 이동 처리를 담당합니다. 액티브 룬이 이동을 제어 중이면 룬에 맡깁니다.
	/// </summary>
	protected virtual void UpdateMovement()
	{
		// 현재 액티브 룬이 있고, 그 룬이 이동(Driver)을 제어하는 인터페이스를 가졌다면
		if (currentActiveRune != null &&
			currentActiveRune is IActiveDriver driver)
		{
			// 룬이 이동 로직을 주도함
			driver.UpdateMovement();

			// 해당 룬의 액션(이동 등)이 모두 끝났다면 다음 액티브 룬으로 넘어감
			if (driver.isFinished)
				ExecuteActiveRune();
		}
	}

	/// <summary>
	/// 적과 충돌했을 때 데미지 계산 및 룬 발동을 처리합니다.
	/// </summary>
	protected virtual void HandleCollision(Collider2D collision)
	{
		// 무기에 장착된 트리거 효과(충돌 시 발동) 룬 가져오기
		var triggerEffects = GetComponents<RuneEffect>()
			.OfType<ITriggerEffect>()
			.ToList();

		bool triggerAnyActivated = false;

		// 1. 트리거 룬에 의한 공격 처리
		foreach (var effect in triggerEffects)
		{
			RuneEffect rune = effect as RuneEffect;

			// 룬이 존재하고 쿨타임이 다 차서 발동 준비가 되었다면
			if (rune != null && rune.isReady)
			{
				// 룬 효과가 포함된 최종 데미지 계산
				float calculatedDamage =
					DamageCalculator.CalculateBaseDamage(instance, rune.data);

				// 대상에게 데미지 적용
				ApplyCalculatedDamage(collision, calculatedDamage);

				// 룬의 특수 반사/추가 타격 효과 등 실행
				effect.OnReflect(collision);

				// 발동했으므로 룬 쿨타임 초기화
				rune.ResetCooltime();

				triggerAnyActivated = true; // 트리거 발동됨 체크

				// 이 룬이 1회성 타격 후 무기 파괴를 요구한다면 즉시 파괴 프로세스 진입
				if (effect.DestroyOnExecute)
				{
					RequestDestroy(DestroyReason.TriggerRune);
					return;
				}
			}
		}

		// 2. 어떤 트리거 룬도 발동되지 않았다면 (일반 평타 공격 처리)
		if (!triggerAnyActivated)
		{
			// 룬 추가 계수 없는 기본 무기 데미지 계산
			float defaultDamage =
				DamageCalculator.CalculateBaseDamage(instance, null);

			ApplyCalculatedDamage(collision, defaultDamage);

			// 맞은 뒤 파괴되어야 하는 무기(예: 활)라면 무기 로직에 의한 파괴 요청
			if (ShouldDestroyOnHit())
				RequestDestroy(DestroyReason.WeaponLogic);
		}
	}

	/// <summary>
	/// 충돌체(적)의 IDamageable 인터페이스를 찾아 실제로 최종 데미지를 가합니다.
	/// </summary>
	protected virtual void ApplyCalculatedDamage(Collider2D collision, float finalDamage)
	{
		var damageable = collision.GetComponent<IDamageable>();

		// 데미지를 받을 수 있는 대상이라면 피격 처리
		if (damageable != null)
			damageable.TakeDamage(finalDamage);
	}

	/// <summary>
	/// 외부 혹은 내부 로직에서 무기 파괴를 요청할 때 호출합니다.
	/// </summary>
	public void RequestDestroy(DestroyReason reason)
	{
		// 이미 파괴 진행 중이면 무시
		if (isDestroyRequested) return;

		// 무기 수명이 다했더라도, 액티브 룬 효과(예: 화려한 이펙트 공격 중)가 아직 안 끝났다면 파괴 보류
		if (currentActiveRune != null &&
			currentActiveRune is IActiveDriver driver &&
			!driver.isFinished)
		{
			// 트리거 파괴가 아닌 자연 수명 파괴라면 무시하고 룬 연출 끝날 때까지 대기
			if (reason == DestroyReason.WeaponLogic)
				return;
		}

		// 트리거 룬이 보호 중이라 파괴할 수 없는 상태라면 무시
		if (!ActuallyDestroy()) return;

		// 모든 방어 조건을 통과했으므로 파괴 플래그 켜기
		isDestroyRequested = true;

		// 최종 파괴 전 마무리 로직(파괴 시 발동되는 룬 등) 실행
		FinalizeMotion();
	}

	/// <summary>
	/// 무기 오브젝트가 씬에서 지워지기 직전 마지막으로 호출됩니다. (Final 룬 처리)
	/// </summary>
	private void FinalizeMotion()
	{
		// 파괴될 때 발동하는 Final 카테고리 룬 찾기 (예: 폭발, 분열 등)
		RuneData finalRune =
			allRunes.FirstOrDefault(r => r.category == RuneCategory.Final);

		if (finalRune != null)
		{
			// Final 룬 컴포넌트를 붙여서 효과 발생
			RuneEffect effect =
				RuneEffectRegistry.AddEffect(gameObject,
				finalRune.runeType,
				instance,
				this,
				finalRune);

			// FinalEffect 인터페이스를 상속받은 룬이라면 마지막 실행(폭발 이펙트 등) 트리거
			if (effect is IFinalEffect final)
				final.OnFinalExecute();
		}

		// 무기 게임오브젝트를 풀로 반환 (없으면 파괴)
		if (PoolManager.Instance != null)
			PoolManager.Instance.ReleaseMotion(this);
		else
			Destroy(gameObject);
	}

	/// <summary>풀 재사용 전 런타임 상태·룬 컴포넌트를 초기화합니다.</summary>
	public virtual void ResetForPool()
	{
		isDestroyRequested = false;
		isInitialLifeSet = false;
		instance = null;
		allRunes = null;
		persistentEffects.Clear();
		currentActiveRune = null;
		activeIndex = -1;
		life = 0f;

		RuneEffect[] effects = GetComponents<RuneEffect>();
		for (int i = effects.Length - 1; i >= 0; i--)
		{
			if (effects[i] != null)
				Destroy(effects[i]);
		}
	}

	/// <summary>
	/// 다음 순서의 액티브 룬(직접 행동하는 룬)을 세팅하고 실행합니다.
	/// </summary>
	private void ExecuteActiveRune()
	{
		// 이전에 실행되던 액티브 룬이 있다면 깔끔하게 컴포넌트 삭제
		if (currentActiveRune != null)
		{
			Destroy(currentActiveRune);
			currentActiveRune = null;
		}

		// 인덱스 증가 (첫 호출 시 -1에서 0이 됨)
		activeIndex++;

		// 액티브 룬만 추려내기
		var activeRunes =
			allRunes.Where(r => r.category == RuneCategory.Active).ToList();

		// 아직 실행할 액티브 룬이 남아있다면 해당 룬 컴포넌트 부착 및 실행 대기
		if (activeIndex < activeRunes.Count)
			currentActiveRune = AddRuneComponent(activeRunes[activeIndex]);
		else
			currentActiveRune = null; // 모두 실행했다면 널 처리
	}

	/// <summary>
	/// 생성 즉시 지속적으로 무기/플레이어에 영향을 주는 룬들을 세팅합니다.
	/// </summary>
	private void SetupPersistentRunes()
	{
		var persistents =
			allRunes.Where(r =>
				r.category == RuneCategory.State ||
				r.category == RuneCategory.Logic);

		foreach (var runeData in persistents)
		{
			// 컴포넌트 부착
			RuneEffect runeEffect = AddRuneComponent(runeData);

			// 정상 부착되었다면 리스트에 담아서 Update()에서 매 프레임 관리
			if (runeEffect != null)
				persistentEffects.Add(runeEffect);
		}
	}

	/// <summary>
	/// 조건 충족 시(적중 시 등) 발동하는 트리거 룬들을 미리 무기에 붙여둡니다.
	/// </summary>
	private void SetTriggerRunes()
	{
		var triggers = allRunes.Where(r => r.category == RuneCategory.Trigger);

		foreach (var trigger in triggers)
			AddRuneComponent(trigger);
	}

	/// <summary>
	/// 룬 타입에 맞는 룬 컴포넌트를 팩토리(Registry)에서 생성하여 이 무기에 부착합니다.
	/// </summary>
	private RuneEffect AddRuneComponent(RuneData data)
	{
		return RuneEffectRegistry.AddEffect(
			gameObject,
			data.runeType,
			instance,
			this,
			data
		);
	}
}