using UnityEngine;

/// <summary>
/// 라바 티라노 골렘용 유도탄 (물리 충돌 시 확률적 화상 디버프 부여, HomingBossBullet 상속)
/// </summary>
public class LavaHomingBullet : HomingBossBullet
{
    [Header("화상 상태이상 설정")]
    [SerializeField] float burnDuration = 3f;      // 화상 디버프 총 지속 시간
    [SerializeField] float burnTickDamage = 2f;     // 1틱당 플레이어가 입을 화상 피해량
    [SerializeField] float burnTickInterval = 0.5f; // 화상 피해가 들어오는 주기 (틱 간격)
    [SerializeField, Range(0f, 100f)] float burnChance = 30f; // 🔥 화상이 적용될 최종 확률 (0% ~ 100%)

    [Header("디버프 연출")]
    [SerializeField] float blinkSpeed = 10f;                            // 화상 상태일 때 플레이어 본체 깜빡임 속도

    // ==========================================
    // 2D 물리 충돌 피격 이벤트 (일반 Collider 상호작용)
    // ==========================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 충돌한 상대 오브젝트가 플레이어가 아니라면 처리하지 않고 즉시 탈출
        if (!collision.gameObject.CompareTag("Player"))
            return;

        // 2. 상대방 오브젝트로부터 플레이어 컴포넌트 참조 안전하게 추출
        Player player =
            collision.gameObject.GetComponent<Player>();

        // 3. 컴포넌트가 누락되어 있다면 예외 처리 종료
        if (player == null)
            return;

        // 4. 🔥 [확률 판정] 0.0 ~ 100.0 사이의 난수를 생성하여 설정된 화상 확률 이내인지 검사
        if (Random.Range(0f, 100f) <= burnChance)
        {
            // 주입 성공 시: 플레이어 본체에 화상 디버프 매개변수 일괄 적용
            player.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, blinkSpeed);
        }

        // 5. 확률 판정 성패 여부와 관계없이 충돌이 일어났으므로 탄막 오브젝트 풀 비활성화 반환
        gameObject.SetActive(false);
    }
}