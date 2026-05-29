using System.Collections;
using UnityEngine;

// 모든 적의 공통 동작 처리
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("적 스탯 데이터")]
    [SerializeField] EnemyData data; // ScriptableObject 데이터

    [Header("외부 참조")]
    public Rigidbody2D target;       // 추적할 플레이어
    public WaveManager waveManager;  // 웨이브 관리용

    [Header("현재 스탯 (런타임 적용값)")]
    public float health;       // 현재 체력
    public float maxHealth;    // 최대 체력
    public float attackDamage; // 공격력
    public float speed;        // 이동 속도

    // 생존 여부
    bool isLive;

    // 빙결 여부
    bool isFrozen;

    // 안개 은신 여부
    bool hiddenInFog;

    // 빙결 남은 시간
    float freezeTimer;

    // 피격 이펙트 중복 방지
    bool isHitEffectRunning;

    // 캐싱 컴포넌트
    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;

    // 기본 색상 저장
    Color originColor;

    void Awake()
    {
        // 필요한 컴포넌트 캐싱
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();

        // 원래 색 저장
        originColor = spriter.color;
    }

    void OnEnable()
    {
        // 플레이어 참조 가져오기
        if (GameManager.instance != null &&
            GameManager.instance.player != null)
        {
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();
        }

        // 기본 상태 초기화
        isLive = true;
        isFrozen = false;
        hiddenInFog = false;

        // 물리 활성화
        coll.enabled = true;
        rigid.simulated = true;

        // 렌더 순서 복구
        spriter.sortingOrder = 2;

        // 기본 색 복구
        spriter.color = originColor;

        // 데이터 적용
        ApplyData();
    }

    // EnemyData 값 적용
    void ApplyData()
    {
        // 데이터 없으면 종료
        if (data == null)
            return;

        // 최대 체력 설정
        maxHealth = data.maxHealth;

        // 현재 체력 풀로 회복
        health = maxHealth;

        // 이동 속도 적용
        speed = data.moveSpeed;

        // 공격력 적용
        attackDamage = data.attackDamage;
    }

    void FixedUpdate()
    {
        // 게임 정지 상태면 종료
        if (!GameManager.instance.isLive)
            return;

        // 죽었으면 종료
        if (!isLive)
            return;

        // 빙결 상태 처리
        if (isFrozen)
        {
            // 빙결 시간 감소
            freezeTimer -= Time.fixedDeltaTime;

            // 시간이 끝나면 해제
            if (freezeTimer <= 0f)
                isFrozen = false;

            // 이동 정지
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 타겟 없으면 종료
        if (target == null)
            return;

        // 플레이어 방향 계산
        Vector2 dirVec = target.position - rigid.position;

        // 이동량 계산
        Vector2 nextVec =
            dirVec.normalized * speed * Time.fixedDeltaTime;

        // 이동
        rigid.MovePosition(rigid.position + nextVec);

        // 물리 흔들림 제거
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 동작 불가 상태면 종료
        if (!GameManager.instance.isLive ||
            !isLive ||
            target == null)
            return;

        // 플레이어 방향 바라보기
        spriter.flipX = target.position.x < rigid.position.x;

        // 안개 은신 중이면 투명도 처리
        if (hiddenInFog)
        {
            // 플레이어와 거리 계산
            float dist =
                Vector2.Distance(rigid.position, target.position);

            // 가까우면 보이기
            // 멀면 반투명
            float alpha = dist <= 2.2f ? 1f : 0.12f;

            Color c = spriter.color;
            c.a = alpha;
            spriter.color = c;
        }
    }

    // 피해 처리
    public void TakeDamage(float damage)
    {
        // 이미 죽었으면 무시
        if (!isLive || health <= 0f)
            return;

        // 체력 감소
        health -= damage;

        // 피격 효과 실행
        if (!isHitEffectRunning)
            StartCoroutine(HitFlashEffect());

        // 체력 0 이하이면 사망
        if (health <= 0f)
            Die();
    }

    // 피격 시 빨간색 점멸
    IEnumerator HitFlashEffect()
    {
        isHitEffectRunning = true;

        // 빨간색
        spriter.color = Color.red;

        yield return new WaitForSeconds(0.1f);

        // 원래 색 복구
        spriter.color = originColor;

        isHitEffectRunning = false;
    }

    // 빙결 적용
    public void ApplyFreeze(float duration)
    {
        isFrozen = true;

        // 더 긴 시간 유지
        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    // 사망 처리
    void Die()
    {
        isLive = false;

        // 충돌 비활성화
        coll.enabled = false;

        // 물리 비활성화
        rigid.simulated = false;

        // 뒤로 렌더링
        spriter.sortingOrder = 1;

        // 웨이브 매니저에 알림
        waveManager?.OnEnemyDead();

        // 오브젝트 풀 반환
        gameObject.SetActive(false);
    }

    // 즉사
    public void KillInstantly()
    {
        if (!isLive)
            return;

        health = 0f;
        Die();
    }

    // 안개 진입
    public void EnterFog()
    {
        hiddenInFog = true;
    }

    // 안개 탈출
    public void ExitFog()
    {
        hiddenInFog = false;

        // 완전 불투명 복구
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;
    }
}