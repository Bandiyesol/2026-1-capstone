using UnityEngine;

// 독 포자를 밟거나 닿으면 터지는 기믹
public class PoisonSpore : BiomeGimmick
{
    [Header("독 장판 Pool 인덱스")]
    [SerializeField] int poisonFieldIndex = 2;

    // 이미 터졌는지 체크
    bool exploded;

    // 애니메이터 캐싱
    Animator anim;

    protected override void Awake()
    {
        base.Awake();

        // Animator 가져오기
        anim = GetComponent<Animator>();
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // 활성화될 때 상태 초기화
        exploded = false;

        // 성장 중엔 충돌 비활성화
        DisableCollider();
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 플레이어가 닿으면 폭발
        Explode();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (exploded)
            return;

        if (collision.GetComponent<Motion>() != null)
        {
            Explode();
        }
    }

    void Explode()
    {
        // 중복 폭발 방지
        if (exploded)
            return;

        exploded = true;

        // 충돌 비활성화
        if (coll != null)
            coll.enabled = false;

        // 폭발 애니메이션 실행
        if (anim != null)
            anim.SetTrigger("Explode");
    }

    // 애니메이션 이벤트에서 호출
    public void SpawnPoisonField()
    {
        // 독 장판 가져오기
        GameObject field = GameManager.instance.pool.GetGimmick(poisonFieldIndex);

        // 같은 부모 아래 배치
        field.transform.SetParent(transform.parent);

        // 현재 위치에 생성
        field.transform.position = transform.position;

        // 포자는 비활성화
        gameObject.SetActive(false);
    }
}