using System.Collections;
using UnityEngine;

// IDamageable 인터페이스를 구현하여 데미지를 받을 수 있는 적 컴포넌트
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("데이터")]
    [SerializeField] EnemyData data; // 적의 기본 스탯을 담은 스크립터블 오브젝트

    [Header("기타")]
    public Rigidbody2D target; // 추적할 대상 (플레이어)
    public WaveManager waveManager; // 웨이브 관리를 위한 매니저 참조

    [Header("적 스텟(건드리지 말 것)")]
    public float health;       // 현재 체력
    public float maxHealth;    // 최대 체력
    public float attackDamage; // 공격력
    public float speed;        // 이동 속도

    // 상태 제어 변수들
    bool isLive;               // 현재 살아있는지 여부
    bool isFrozen;             // 빙결(치명적 멈춤) 상태 여부
    bool hiddenInFog;          // 안개 속에 숨겨졌는지 여부
    float freezeTimer;         // 빙결 남은 시간 타이머
    bool isHitEffectRunning;   // 피격 깜빡임 코루틴 실행 중 여부

    // 컴포넌트 캐싱 변수들
    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;
    Color originColor;         // 피격 이펙트 후 되돌릴 원래 스프라이트 색상

    void Awake()
    {
        // 컴포넌트 최초 캐싱 및 기본 색상 저장
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        originColor = spriter.color;
    }

    void OnEnable()
    {
        // 오브젝트 풀에서 활성화될 때 실행: 플레이어 타겟 자동 설정
        if (GameManager.instance != null && GameManager.instance.player != null)
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();

        // 적 상태 및 컴포넌트 초기화(부활 세팅)
        isLive = true;
        isFrozen = false;
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2; // 살아있을 때 레이어 순서 높임
        spriter.color = originColor;
        ApplyData(); // 데이터 로드 및 스텟 적용
    }

    void ApplyData()
    {
        if (data == null) return;

        // ScriptableObject(EnemyData)의 데이터를 실시간 스텟 변수에 대입
        speed = data.moveSpeed;
        maxHealth = data.maxHealth;
        health = maxHealth;
        attackDamage = data.attackDamage;
    }

    void FixedUpdate()
    {
        // 게임이 멈췄거나 적이 죽었다면 물리 연산 스킵
        if (!GameManager.instance.isLive || !isLive)
            return;

        // 빙결 상태 처리: 타이머 감소 및 물리 속도 제로화 후 리턴
        if (isFrozen)
        {
            freezeTimer -= Time.fixedDeltaTime;
            if (freezeTimer <= 0f)
                isFrozen = false;

            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null) return;

        // 플레이어 방향으로 등속 이동 처리 및 관성(떨림) 방지를 위한 속도 제로화
        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 게임 중지, 사망, 타겟 부재 시 렌더링 연산 스킵
        if (!GameManager.instance.isLive || !isLive || target == null)
            return;

        // 플레이어의 X축 위치에 따라 좌우 스프라이트 반전(Flip)
        spriter.flipX = target.position.x < rigid.position.x;

        // 안개 시스템 내부 로직: 플레이어와 멀어지면 투명하게(시야 제한) 변경
        if (hiddenInFog)
        {
            float dist = Vector2.Distance(rigid.position, target.position);
            float alpha = dist <= 2.2f ? 1f : 0.12f; // 일정 거리(2.2) 밖이면 반투명화
            Color c = spriter.color;
            c.a = alpha;
            spriter.color = c;
        }
    }

    // IDamageable 인터페이스 구현부: 외부(무기 등)에서 호출 시 데미지 적용
    public void TakeDamage(float damage)
    {
        if (!isLive || health <= 0f) return;

        health -= damage;

        // 중복 코루틴 방지하면서 피격 빨간색 깜빡임 효과 실행
        if (!isHitEffectRunning)
            StartCoroutine(HitFlashEffect());

        if (health <= 0f)
            Die(); // 사망 처리
    }

    // 피격 시 0.1초 동안 빨갛게 변했다가 원래대로 돌아오는 코루틴
    IEnumerator HitFlashEffect()
    {
        isHitEffectRunning = true;
        spriter.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriter.color = originColor;
        isHitEffectRunning = false;
    }

    // 외부에서 빙결 상태(디버프 등)를 부여할 때 호출하는 메서드
    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration); // 더 긴 빙결 시간으로 갱신
    }

    // 적 사망 처리 메서드
    void Die()
    {
        isLive = false;
        coll.enabled = false;       // 충돌체 비활성화 (시체 통과 가능)
        rigid.simulated = false;    // 물리 연산 중지
        spriter.sortingOrder = 1;   // 바닥 시체 연출을 위해 레이어 순서 낮춤

        if (GameManager.instance != null)
            GameManager.instance.Kill++; // 플레이어 총 킬 수 누적

        // 보상 드랍 시스템 연동: 코인 생성
        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromEnemy(transform.position);

        // 보상 드랍 시스템 연동: 상자 생성 (유니크 몬스터/보스는 더 좋은 상자)
        if (ChestDropManager.Instance != null)
        {
            if (data != null && data.isUnique)
                ChestDropManager.Instance.TryDropFromBoss(transform.position);
            else
                ChestDropManager.Instance.TryDropFromEnemy(transform.position);
        }

        waveManager?.OnEnemyDead(); // 현재 웨이브 생존 적 숫자 차감 알림
        gameObject.SetActive(false); // 오브젝트 풀로 반환(비활성화)
    }

    // 즉사기 혹은 특수 기믹으로 적을 바로 처형할 때 사용
    public void KillInstantly()
    {
        if (!isLive) return;

        health = 0f;
        Die();
    }

    // 안개 트리거 진입 시 호출 (외부용)
    public void EnterFog()
    {
        hiddenInFog = true;
    }

    // 안개 영역을 벗어났을 때 호출하여 알파(투명도) 원상복구 (외부용)
    public void ExitFog()
    {
        hiddenInFog = false;
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;
    }
}