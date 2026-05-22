using UnityEngine;

// 파괴 시 독 장판 생성
public class PoisonSpore : BiomeGimmick
{
    [Header("독 장판 Pool 인덱스")]
    [SerializeField] int poisonFieldIndex = 2;

    // 이미 터졌는지
    bool exploded;

    Animator anim;

    protected override void Awake()
    {
        base.Awake();

        // 자주 쓰는 컴포넌트 캐싱
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

        // 재사용 초기화
        exploded = false;

        // 자라는 동안 충돌 비활성
        DisableCollider();
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 플레이어 접촉 시 폭발
        Explode();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 터졌으면 중단
        if (exploded)
            return;

        // 플레이어 공격에 맞으면 폭발
        if (collision.CompareTag("Bullet"))
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

        // 충돌 비활성
        if (coll != null)
            coll.enabled = false;

        // 터지는 애니메이션 재생
        if (anim != null)
            anim.SetTrigger("Explode");
    }

    // 애니메이션 마지막 프레임에서 호출
    public void SpawnPoisonField()
    {
        // 독 장판 생성
        GameObject field = GameManager.instance.pool.GetGimmick(poisonFieldIndex);

        // 현재 포자와 같은 스테이지 부모로 설정
        field.transform.SetParent(transform.parent);

        // 위치 지정
        field.transform.position = transform.position;

        // 포자 비활성
        gameObject.SetActive(false);
    }
}
