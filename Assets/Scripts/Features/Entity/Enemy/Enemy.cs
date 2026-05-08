using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    // 이동 속도
    public float speed;
    // 현재 체력
    public float health;
    // 최대 체력
    public float maxHealth;
    // 타입별 애니메이터 컨트롤러
    public RuntimeAnimatorController[] animCon;
    // 추적 대상(플레이어 리지드바디)
    public Rigidbody2D target;
    // 생존 상태 플래그
    bool isLive;
    bool isFrozen;
    float freezeTimer;
	private Color originalColor;
	private bool isHitEffectRunning = false;

    // 자주 사용하는 컴포넌트 캐싱
    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;


    void Awake()
    {
        // 런타임 중 자주 접근하는 컴포넌트는 미리 참조 저장
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
		originalColor = spriter.color;
    }

    public void Init(SpawnData data)
    {
        // 스폰 데이터에 맞춰 외형/능력치 초기화
        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed = data.speed;
        maxHealth = data.health;
        health = data.health;
    }

    void OnEnable()
    {
        // 풀에서 재활성화될 때 상태를 전투 가능 상태로 초기화
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();
            isLive = true;
            coll.enabled = true;
            rigid.simulated = true;
            spriter.sortingOrder = 2;
            health = maxHealth;
        }

    }

    void FixedUpdate()
    {
        // 게임이 멈췄거나 이미 죽은 적은 이동 처리하지 않음
        if (!GameManager.instance.isLive)
            return;

        if (!isLive)
            return;
        if (isFrozen)
		{
        	freezeTimer -= Time.fixedDeltaTime;
        	if (freezeTimer <= 0f) isFrozen = false;
        	rigid.linearVelocity = Vector2.zero;
        	return;
		}

        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 타겟 기준으로 좌우 반전해 바라보는 방향 표현
        if (!GameManager.instance.isLive)
            return;
            
        if (!isLive)
            return;

        spriter.flipX = target.position.x < rigid.position.x;
    }

	public void TakeDamage(float damage)
	{
		if (health > 0)
		{
			health -= damage;
			if (!isHitEffectRunning) StartCoroutine(HitFlashEffect());
			if (health <= 0) Dead();
		}
	}

	private IEnumerator HitFlashEffect()
	{
		isHitEffectRunning = true;
		spriter.color = Color.red;

		yield return new WaitForSeconds(0.1f);

		spriter.color = originalColor;
		isHitEffectRunning = false;
	}

    void Dead()
    {
        // 사망 시 레벨업 체크 후 오브젝트 풀로 반납
        GameManager.instance.GetLevelUp();
        gameObject.SetActive(false);
    }
}

