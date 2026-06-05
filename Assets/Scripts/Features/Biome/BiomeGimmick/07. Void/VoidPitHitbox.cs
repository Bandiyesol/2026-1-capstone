using UnityEngine;

// 함정 충돌 처리
public class VoidPitHitbox : MonoBehaviour
{
    [Header("최대 반지름")]
    [SerializeField]
    float maxRadius = 0.73f; // px / PPU

    [Header("구멍이 커지는 속도와 작아지는 속도")]
    [SerializeField]
    float growSpeed = 0.75f;
    [SerializeField]
    float shrinkSpeed = 0.75f;

    // 부모 기믹
    VoidPitGimmick parentGimmick;

    // 콜라이더
    CircleCollider2D coll;

    // 커지는 상태
    bool growing;

    // 작아지는 상태
    bool shrinking;

    void Awake()
    {
        // 부모 찾기
        parentGimmick =
            GetComponentInParent<VoidPitGimmick>();

        // 콜라이더 저장
        coll =
            GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        // 커지는 중
        if (growing)
        {
            coll.radius +=
                growSpeed * Time.deltaTime;

            // 최대 크기 도달
            if (coll.radius >= maxRadius)
            {
                coll.radius = maxRadius;

                growing = false;
            }
        }

        // 작아지는 중
        if (shrinking)
        {
            coll.radius -=
                shrinkSpeed * Time.deltaTime;

            // 최소 크기 제한
            if (coll.radius <= 0f)
            {
                coll.radius = 0f;

                shrinking = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어만
        if (!collision.CompareTag("Player"))
            return;

        // 부모 없으면 중단
        if (parentGimmick == null)
            return;

        // 플레이어 가져오기
        Player player =
            collision.GetComponent<Player>();

        // 플레이어 없으면 중단
        if (player == null)
            return;

        // 부모에 전달
        parentGimmick.HitPlayer(player);
    }

    // 커지기 시작
    public void StartGrow()
    {
        growing = true;
        shrinking = false;
    }

    // 최대 크기 유지
    public void HoldSize()
    {
        growing = false;
        shrinking = false;
    }

    // 작아지기 시작
    public void StartShrink()
    {
        shrinking = true;
        growing = false;
    }

    // 충돌 켜기
    public void EnableHitbox()
    {
        parentGimmick?.EnableHitbox();
    }

    // 충돌 끄기
    public void DisableHitbox()
    {
        parentGimmick?.DisableHitbox();
    }
}