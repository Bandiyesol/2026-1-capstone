using UnityEngine;
using System.Collections;

// 모든 바이옴 기믹의 공통 부모
public abstract class BiomeGimmick : MonoBehaviour
{
    [Header("기믹 유지 시간")]
    [SerializeField]
    protected float lifeTime = 2f;

    // 현재 남은 시간
    protected float currentLifeTime;

    // 공통 콜라이더
    protected Collider2D coll;

    protected virtual void Awake()
    {
        // 콜라이더 캐싱
        coll = GetComponent<Collider2D>();
    }

    protected virtual void OnEnable()
    {
        // 남은 시간 초기화
        currentLifeTime = lifeTime;

        // 켜질 때 기믹 시작
        OnSpawn();

        // 일정 시간 뒤 비활성화
        if (lifeTime > 0f)
            StartCoroutine(DisableRoutine());
    }

    protected virtual void Update()
    {
        // 남은 시간 감소
        if (currentLifeTime > 0f)
        {
            currentLifeTime -= Time.deltaTime;
        }
    }

    // 기믹 생성 직후 동작
    protected virtual void OnSpawn() { }

    // 플레이어와 닿았을 때 동작
    protected abstract void OnPlayerTrigger(Player player);

    // 일정 시간 뒤 비활성화
    IEnumerator DisableRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        Player player = collision.GetComponent<Player>();

        if (player != null)
            OnPlayerTrigger(player);
    }

    // 콜라이더 활성
    public void EnableCollider()
    {
        if (coll != null)
            coll.enabled = true;
    }

    // 콜라이더 비활성
    public void DisableCollider()
    {
        if (coll != null)
            coll.enabled = false;
    }
}