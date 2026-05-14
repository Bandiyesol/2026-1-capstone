using System.Collections.Generic;
using UnityEngine;


public abstract class Motion : MonoBehaviour
{
	public WeaponInstance instance;
	protected List<RuneData> activeRunes;
	protected int index;
	protected RuneEffect effect;
	protected RuneData data;



	public virtual void Initialize(WeaponInstance instance, List<RuneData> runes) 
	{
		this.instance = instance;
		activeRunes = new List<RuneData>(runes);
		index = -1;
		transform.localScale = new Vector3(instance.size, instance.size, 1f);

		OnStartMotion();
		ExecuteRune();
	}


	protected abstract void OnStartMotion();


	public void ExecuteRune()
	{
		if (effect != null) Destroy(effect);

		index++;
		if (index >= activeRunes.Count)
		{
			effect = null;
			data = null;
			return;
		}

		data = activeRunes[index];
		System.Type type = System.Type.GetType("Effect" + data.runeType.ToString());
		if (type == null) return;

		effect = (RuneEffect)gameObject.AddComponent(type);
		if (effect != null) effect.InitEffect(instance, this, data);
	}


	protected virtual void Update() => transform.Translate(Vector3.right * instance.speed * Time.deltaTime);


	protected virtual void HandleCollision(Collider2D collision)
	{
		if (effect == null)
		{
			DefaultHitEffect(collision);
			return;
		}

		switch (data.category)
		{
			case RuneCategory.Movement:
				ExecuteRune();
				break;

			case RuneCategory.Trigger:
				if (effect.ManualCollision) return; 
            	ExecuteRune();
				break;

			case RuneCategory.Final:
				break;
		}
	}


	protected virtual void OnTriggerEnter2D(Collider2D collision) => HandleCollision(collision);


	protected virtual void DefaultHitEffect(Collider2D collision)
	{
		if (collision.CompareTag("Enemy"))
		{
			IDamageable enemy = collision.GetComponent<IDamageable>();
			if (enemy != null) enemy.TakeDamage(instance.damage);
		}

		CheckDestruction();
	}


	protected virtual void CheckDestruction()
	{
		int next = index + 1;
		if (next < activeRunes.Count && activeRunes[next].category == RuneCategory.Final)
		{
			ExecuteRune();
			return;
		}

		OnFinalDestroy();
	}


	private void OnFinalDestroy() => Destroy(gameObject);


	public List<RuneData> GetRemainingRunes() => new List<RuneData>(activeRunes);
}