using UnityEngine;

// [RuneEffect.cs] 인터페이스 선언부
public interface IActiveDriver { bool isFinished { get; } void UpdateMovement(); } // 매 프레임 위치 조작
public interface IStateEffect { bool isFinished { get; } void UpdateState(); }    // 무기 상태 변경
public interface ILogicEffect { void UpdateLogic(); }                             // 백그라운드 로직
public interface ITriggerEffect { bool ProtectParent { get; } bool DestroyOnExecute { get; } void OnReflect(Collider2D collision); } // 충돌/트리거 발생 시
public interface IFinalEffect { void OnFinalExecute(); }                          // 소멸 직전 실행

// 모든 룬 이펙트 스크립트의 최상위 부모 클래스
public abstract class RuneEffect : MonoBehaviour
{
	protected WeaponInstance weapon;      // 장착된 무기 데이터
	protected Motion parentMotion;        // 무기의 물리적 모션 관리자
	public RuneData data { get; protected set; } // 원본 룬 데이터
	public float currentCooltime { get; protected set; } = 0f;

	public bool isReady => currentCooltime <= 0f; // 쿨타임 체크
	public virtual bool isFinished => true;
	public virtual bool ManualCollision => false;

	// 룬이 부착될 때 필요한 종속성 주입 및 초기화
	public virtual void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		weapon = instance;
		parentMotion = motion;
		data = runeData;
		currentCooltime = 0f;
	}

	// 쿨타임을 해당 룬의 인터벌 값으로 리셋
	public void ResetCooltime() => currentCooltime = RuneDataAccess.GetInterval(data);

	// 매 프레임 쿨타임 감소
	protected void UpdateCooltime()
	{
		if (currentCooltime > 0f) currentCooltime -= Time.deltaTime;
	}
}