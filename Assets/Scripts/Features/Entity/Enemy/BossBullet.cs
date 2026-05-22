using UnityEngine;

public class BossBullet : MonoBehaviour
{
    // 탄막 데미지
    [SerializeField]
    public float damage = 10f;

    // 이동 속도
    [SerializeField]
    float speed = 5f;

    // 생존 시간
    [SerializeField]
    float lifeTime = 5f;

    // 이동 방향
    Vector2 moveDir = Vector2.zero;

    // 리지드바디
    Rigidbody2D rigid;
    // 콜라이더
    Collider2D col;

    // 타이머
    float timer;

    void Awake()
    {
        // 리지드 바디 저장
        rigid = GetComponent<Rigidbody2D>();

        // 콜라이더 저장
        col = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        // 타이머 초기화
        timer = 0f;

        // 충돌 잠시 끄기
        if (col != null)
            col.enabled = false;

        // 물리 활성화
        if (rigid != null)
        {
            rigid.simulated = true;
        }
    }

    void Update()
    {
        // 직접 이동
        transform.position +=
            (Vector3)moveDir *
            speed *
            Time.deltaTime;

        // 시간 증가
        timer += Time.deltaTime;

        // 자동 제거
        if (timer >= lifeTime)
        {
            gameObject.SetActive(false);
        }
    }

    // 탄막 초기화
    public void Init(Vector2 dir)
    {
        // 방향 저장
        moveDir = dir.normalized;

        // 회전
        float angle =
            Mathf.Atan2(moveDir.y, moveDir.x)
            * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0, 0, angle);

        // 위치 세팅 끝난 뒤 충돌 활성화
        if (col != null)
            col.enabled = true;
    }

    void OnDisable()
    {
        // 방향 초기화
        moveDir = Vector2.zero;

        // 속도 제거
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
        }
    }
}