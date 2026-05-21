using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum DestroyReason
{
	WeaponLogic,
	TriggerRune
}
	

public abstract class Motion : MonoBehaviour
{
	[Header("[ 설정 ]")]
	public WeaponInstance instance;
	protected List<RuneData> allRunes;

	protected List<RuneEffect> persistentEffects = new List<RuneEffect>();
	protected RuneEffect currentActiveRune;
	protected int activeIndex = -1;
	protected float life = 0f;
	protected bool isInitialLifeSet = false;
	private bool isDestroyRequested = false;


	


	public virtual void Initialize(WeaponInstance instance, List<RuneData> runes, float inheritedLifeTime = -1f) 
	{
		this.instance = instance;
		allRunes = new List<RuneData>(runes);
		transform.localScale = new Vector3(instance.size, instance.size, 1f);

		if (inheritedLifeTime > 0f) life = inheritedLifeTime;
		else life = GetDefaultTime();

		isInitialLifeSet = true;

		OnStartMotion();
		SetupPersistentRunes();
		SetTriggerRunes();
		ExecuteActiveRune();
	}


	protected virtual void Update()
	{
		if (!isInitialLifeSet) return;

		life -= Time.deltaTime;
		if (life <= 0f) RequestDestroy(DestroyReason.WeaponLogic);

		UpdateMovement();

		foreach (var effect in persistentEffects)
		{
			if (effect is IStateEffect state) state.UpdateState();
			if (effect is ILogicEffect logic) logic.UpdateLogic();
		}
	}


	protected abstract void OnStartMotion();

	protected abstract float GetDefaultTime();

	protected virtual void OnTriggerEnter2D(Collider2D collision) => HandleCollision(collision);

	protected virtual bool ShouldDestroyOnHit() => false;

	protected virtual bool ActuallyDestroy() 
	{
		var triggerEffects = GetComponents<RuneEffect>().OfType<ITriggerEffect>();
		foreach (var trigger in triggerEffects)
		{
			if (trigger.ProtectParent) return false;
		}
		
		return true;
	}

	public float GetRemainingLife() => life;
	public List<RuneData> GetRunes() { return new List<RuneData>(allRunes); }


	protected virtual void UpdateMovement()
	{
		if (currentActiveRune != null && currentActiveRune is IActiveDriver driver) 
		{
			driver.UpdateMovement();
			if (driver.isFinished) ExecuteActiveRune();
		}
	}


	protected virtual void HandleCollision(Collider2D collision)
	{
		var triggerEffects = GetComponents<RuneEffect>().OfType<ITriggerEffect>().ToList();
		bool triggerAnyActivated = false;

		foreach (var effect in triggerEffects)
		{
			RuneEffect rune = effect as RuneEffect;

			if (rune != null && rune.isReady)
			{
				float calculatedDamage = DamageCalculator.CalculateBaseDamage(instance, rune.data);
				ApplyCalculatedDamage(collision, calculatedDamage);

				effect.OnReflect(collision);
				rune.ResetCooltime();
				triggerAnyActivated = true;

				if (effect.DestroyOnExecute)
				{
					RequestDestroy(DestroyReason.TriggerRune);
					return;
				}
			}
		}

		if (!triggerAnyActivated)
		{
			float defaultDamage = DamageCalculator.CalculateBaseDamage(instance, null);
			ApplyCalculatedDamage(collision, defaultDamage);
			
			if (ShouldDestroyOnHit()) RequestDestroy(DestroyReason.WeaponLogic);
		}
	}


	protected virtual void ApplyCalculatedDamage(Collider2D collision, float finalDamage)
	{
		if (collision.CompareTag("Enemy")) 
		{
			var damageable = collision.GetComponent<IDamageable>();
			if (damageable != null) damageable.TakeDamage(finalDamage);
		}
	}


	public void RequestDestroy(DestroyReason reason)
	{
		if (isDestroyRequested) return;

		if (currentActiveRune != null && currentActiveRune is IActiveDriver driver && !driver.isFinished)
		{
			if (reason == DestroyReason.WeaponLogic) return;
		}

		if (!ActuallyDestroy()) return;

		isDestroyRequested = true;
		FinalizeMotion();
	}


	private void FinalizeMotion()
	{
		RuneData finalRune = allRunes.FirstOrDefault(r => r.category == RuneCategory.Final);

		if (finalRune != null)
		{
			RuneEffect effect = RuneEffectRegistry.AddEffect(gameObject, finalRune.runeType, instance, this, finalRune);
			if (effect is IFinalEffect final) final.OnFinalExecute();
		}

		Destroy(gameObject);
	}


	private void ExecuteActiveRune()
	{
		if (currentActiveRune != null)
		{
			Destroy(currentActiveRune);
			currentActiveRune = null;
		}

		activeIndex++;
		var activeRunes = allRunes.Where(r => r.category == RuneCategory.Active).ToList();

		if (activeIndex < activeRunes.Count) currentActiveRune = AddRuneComponent(activeRunes[activeIndex]);
		else currentActiveRune = null;

	}


	private void SetupPersistentRunes()
	{
		var persistents = allRunes.Where(r => r.category == RuneCategory.State || r.category == RuneCategory.Logic);
		foreach (var runeData in persistents)
		{
			RuneEffect runeEffect = AddRuneComponent(runeData);
			if (runeEffect != null) persistentEffects.Add(runeEffect);
		}
	}


	private void SetTriggerRunes()
	{
		var triggers = allRunes.Where(r => r.category == RuneCategory.Trigger);
		foreach (var trigger in triggers) AddRuneComponent(trigger);
	}


	private RuneEffect AddRuneComponent(RuneData data)
	{
		return RuneEffectRegistry.AddEffect(gameObject, data.runeType, instance, this, data);
	}
}