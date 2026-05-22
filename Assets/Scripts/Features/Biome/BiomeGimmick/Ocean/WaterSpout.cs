using UnityEngine;
using System.Collections;

public class WaterSpout : BiomeGimmick
{
    [Header("경고 및 수명")]
    [SerializeField] float warningTime = 1f;

    [Header("흡입 설정")]
    [SerializeField] float suctionRange = 5f;
    [SerializeField] float suctionForce = 3f;

    [Header("지속 틱 데미지 설정")]
    [SerializeField] float tickDamage = 10f;
    [SerializeField] float damageInterval = 0.5f;

    [Header("오브젝트 연결")]
    [SerializeField] GameObject redCircle;
    [SerializeField] GameObject waterspoutObject;

    // 내부 변수
    private bool isPlayerInside = false;
    private Coroutine damageCoroutine = null;

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnSpawn()
    {
        StopAllCoroutines();
        isPlayerInside = false;
        damageCoroutine = null;

        if (redCircle != null) redCircle.SetActive(true);
        if (waterspoutObject != null) waterspoutObject.SetActive(false);

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(warningTime);

        if (redCircle != null) redCircle.SetActive(false);
        if (waterspoutObject != null) waterspoutObject.SetActive(true);

        float remainingTime = lifeTime - warningTime;
        float elapsedTime = 0f;

        while (elapsedTime < remainingTime && gameObject.activeSelf)
        {
            elapsedTime += Time.deltaTime;
            ApplySuction();
            yield return null;
        }
    }

    void ApplySuction()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, suctionRange);
        foreach (var hitCollider in hits)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Player player = hitCollider.GetComponent<Player>();
                if (player != null)
                {
                    Vector2 suctionDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
                    player.externalVelocity = suctionDir * suctionForce;
                }
            }
        }
    }

    // --- 여기서부터 중요: WaterSpoutEvent에서 찾는 이름들 ---
    public void OnWaterspoutEnter(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(TickDamageRoutine());
            }
        }
    }

    public void OnWaterspoutExit(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    IEnumerator TickDamageRoutine()
    {
        while (isPlayerInside)
        {
            GameManager.instance.Health -= tickDamage;
            yield return new WaitForSeconds(damageInterval);
        }
        damageCoroutine = null;
    }

    protected override void OnPlayerTrigger(Player player) { }
}