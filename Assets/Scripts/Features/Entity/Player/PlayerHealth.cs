using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
	public float maxHp;
	public float currentHp;


	void Start()
	{
		currentHp = maxHp;
	}

	public void TakeDamage(float damage)
	{
		currentHp -= damage;
		if (currentHp <= 0) Die();
	}

	void Die()
	{
		Debug.Log("플레이어 사망");
	}
}