using System.Collections;
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
        DisableCollider();
        StartCoroutine(PlayAnimNextFrame());
    }

    IEnumerator PlayAnimNextFrame()
    {
        yield return null;

        if (anim != null)
        {
            anim.enabled = false;        // Animator 완전히 끄기
            yield return null;           // 한 프레임 대기
            anim.enabled = true;         // 다시 켜면 Entry부터 재시작
        }
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
        if (exploded)
            return;

        exploded = true;
        StopAllCoroutines(); // 추가: lifeTime 타이머 중단

        if (coll != null)
            coll.enabled = false;

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