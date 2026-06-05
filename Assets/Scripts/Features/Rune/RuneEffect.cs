using UnityEngine;


public interface IActiveDriver
{
	bool isFinished { get; }
	void UpdateMovement();
}


public interface IStateEffect
{
	bool isFinished { get; }
	void UpdateState();
}


public interface ILogicEffect
{
	void UpdateLogic();
}


public interface ITriggerEffect
{
	bool ProtectParent { get; }
	bool DestroyOnExecute { get; }
	void OnReflect(Collider2D collision);
}


public interface IFinalEffect
{
	void OnFinalExecute();
}


public abstract class RuneEffect : MonoBehaviour
{
	protected WeaponInstance weapon;
	protected Motion parentMotion;
	public RuneData data { get; protected set; }
	public float currentCooltime { get; protected set; } = 0f;


	public bool isReady => currentCooltime <= 0f;
	public virtual bool isFinished => true;
	public virtual bool ManualCollision => false;
	
	public virtual void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		weapon = instance;
		parentMotion = motion;
		data = runeData;
		currentCooltime = 0f;
	}

	public void ResetCooltime()
	{
		float interval = RuneDataAccess.GetInterval(data);
		// 특수 물약 버프 시 CooldownMultiplier가 0.5f → 쿨타임 절반
		float multiplier = RuneManager.instance != null ? RuneManager.instance.CooldownMultiplier : 1f;
		currentCooltime = interval * multiplier;
	}

	protected void UpdateCooltime() 
	{
		if (currentCooltime > 0f) currentCooltime -= Time.deltaTime;
	}

}