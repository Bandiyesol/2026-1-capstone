using System.Security.Cryptography;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	private float damage;
	private float speed;
	private float range;
	private Vector3 direction;
	private Vector3 startPos;


	public void Setup(float dmg, float ran, float spd, Vector3 dir)
	{
		damage = dmg;
		range = ran;
		speed = spd;
		direction = dir;
		startPos = transform.position;

		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
	}

	void Update()
	{
		transform.position += direction * speed * Time.deltaTime;

		if (Vector3.Distance(startPos, transform.position) > range) Destroy(gameObject);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Monster")) != 0)
		{
			MonsterAI monster = collision.GetComponent<MonsterAI>();
			
			if (monster != null)
			{
				monster.Damage(damage, transform.position);
				Destroy(gameObject);
			}
		}

		if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacle")) Destroy(gameObject);
	}
}