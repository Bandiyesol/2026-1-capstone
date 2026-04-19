using System.Collections;
using UnityEngine;
using UnityEngine.Video;
public class MonsterAI : MonoBehaviour
{
	[Header("[ Default Data ]")]
	public float currentHp;
	public float maxDistance;

	[Header("[ Monster Indevidual Data ]")]
	[SerializeField] private string monsterName;
	[SerializeField] private float moveSpeed;
	[SerializeField] private float maxHp;
	[SerializeField] private float attackRange;
	[SerializeField] private float cooltime;
	[SerializeField] private float damage;
	[SerializeField] private float knockbackForce;
	[SerializeField] private float knockbackDuration;

	[Header("[ Referances ]")]
	private Transform player;
	private Rigidbody2D rigidBody;
	private SpriteRenderer spriteRenderer;
	private Animator animator;
	private float lastAttackTime;
	private bool isKnockback;


	void Awake()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
		rigidBody = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
	}

	void OnEnable()
	{
		Status();
	}

	void Update()
	{
		if (player == null || rigidBody == null) return;

		if (currentHp <= 0)
		{
			Die();
			return;
		}

		if (isKnockback) return;

		CheckDistance();
	}

	void Status()
	{
		currentHp = maxHp;
		player = GameObject.FindGameObjectWithTag("Player").transform;
		rigidBody.linearVelocity = Vector2.zero;
		spriteRenderer.color = Color.white;
		animator.Rebind();
	}

	void CheckDistance()
	{
		float distance = Vector2.Distance(transform.position, player.position);

		if (distance <= maxDistance)
		{
			MoveToPlayer();
			if (distance <= attackRange) Attack();
		}

		else TeleportToPlayer();
		
	}
	void MoveToPlayer()
	{
		Vector3 direction = (player.position - transform.position).normalized;
		rigidBody.linearVelocity = direction * moveSpeed;

		if (direction.x != 0) spriteRenderer.flipX = direction.x < 0;
	}

	void TeleportToPlayer()
	{
		Vector2 newPos = Random.insideUnitCircle.normalized * Random.Range(5f, 8f);
		transform.position = new Vector3(newPos.x, newPos.y, 0) + player.position;
	}

	void Attack()
	{
		if (Time.time >= lastAttackTime + cooltime)
		{
			PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

			if (playerHealth != null) 
			{
				if (animator != null) animator.SetTrigger("doAttack");
				playerHealth.TakeDamage(damage);
			}

			lastAttackTime = Time.time;
		}
	}

	public void Damage(float damage, Vector2 playerPosition)
	{
		if (currentHp <= 0) return;

		currentHp -= damage;
		StartCoroutine(KnockbackRoutine(playerPosition));
		
		if (currentHp <= 0) Die();
	}

	IEnumerator KnockbackRoutine(Vector2 playerPosition)
	{
		isKnockback = true;

		Vector2 knockbackDirection = (transform.position - (Vector3)playerPosition).normalized;

		rigidBody.linearVelocity = Vector2.zero;
		rigidBody.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

		yield return new WaitForSeconds(knockbackDuration);

		rigidBody.linearVelocity = Vector2.zero;
		isKnockback = false;
	}

	void Die()
	{
		if (currentHp > 0) return;

		rigidBody.linearVelocity = Vector2.zero;
		animator.Rebind();
		StageManager.Instance.OnMonsterDead();
		Invoke("ReturnToPool", 0.5f);
	}

	void ReturnToPool()
	{
		gameObject.SetActive(false);
	}
}