using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
	public WeaponInfo info;
	public LayerMask monsterLayer;

	[Header("[ Final Stats ]")]
	public float finalDamage;
	public float finalWeight;
	public float finalCooltime;
	public float finalReach;
	public float finalSpeed;

	protected float timer;


	public virtual void Init(WeaponInfo newInfo, WeaponBalance balance, LayerMask layer)
	{
		info = newInfo;
		monsterLayer = layer;
		timer = 0;

		finalDamage = Random.Range(balance.damageRange[0], balance.damageRange[1]);
		finalWeight = Random.Range(balance.weightRange[0], balance.weightRange[1]);
		finalCooltime = Random.Range(balance.cooltimeRange[0], balance.cooltimeRange[1]);
		finalReach = Random.Range(balance.reachRange[0], balance.reachRange[1]);
		finalSpeed = Random.Range(balance.speedRange[0], balance.speedRange[1]);
	}

	public void Tick(float dlt)
	{
		if (info == null) return;

		timer += dlt;

		if (timer >= finalCooltime)
		{
			Attack();
			timer = 0;
		}
	}

	protected abstract void Attack();
}