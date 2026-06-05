using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [Header("탄막 설정")]
    [SerializeField] public float damage = 10f; // 탄막 피해량
    [SerializeField] float speed = 5f; // 이동 속도

    // [보존] 인스펙터 창에서 설정한 값이 절대 0으로 초기화되지 않고 유지됩니다.
    [SerializeField] protected float lifeTime = 5f;

    protected Vector2 moveDir = Vector2.zero; // 이동 방향
    protected Rigidbody2D rigid; // 리지드바디
    protected Collider2D col; // 콜라이더
    protected float timer; // 흘러간 시간을 계산하는 타이머 변수

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    protected virtual void OnEnable()
    {
        timer = 0f;

        if (col != null)
            col.enabled = false;

        if (rigid != null)
        {
            rigid.simulated = true;
            rigid.linearVelocity = Vector2.zero;
        }
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)moveDir * speed * Time.deltaTime;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            gameObject.SetActive(false);
    }

    public virtual void Init(Vector2 dir)
    {
        timer = 0f; // ⭐[버그 수정] 제 실수로 lifeTime을 0으로 밀어버리던 코드를 'timer'로 정확히 수정했습니다.
        moveDir = dir.normalized; // 방향 저장

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg; // 각도 계산
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (col != null)
            col.enabled = true; // 방향 세팅이 끝나면 충돌 활성화
    }

    void OnDisable()
    {
        moveDir = Vector2.zero;
        if (rigid != null) rigid.linearVelocity = Vector2.zero;
    }
}