using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기존 Bullet.cs를 대체하는 룬 탄환 클래스.
/// 최대 3개의 룬(IBulletRune)을 순서대로 실행하며,
/// 룬 조합이 유효하지 않으면 탄환이 즉시 사라지는 '런타임 에러' 연출을 수행한다.
/// </summary>
public class BulletRune : MonoBehaviour
{
    public int prefabId; // 풀 매니저에서 사용할 프리팹 인덱스
    // ──────────────── 전투 스탯 ────────────────
    public float damage;
    // 관통 횟수 (-1이면 근접 무기)
    public int per;
    // 근접 무기 판정 여부
    public bool isMelee;

    // ──────────────── 룬 ────────────────
    // 장착된 룬 인스턴스 목록 (순서 중요)
    readonly List<IBulletRune> runes = new();
    // 이 탄환에 장착된 RuneData 원본 (수치 참조용)
    public List<RuneData> runeDataList = new();
    // 런타임 에러 상태 (호환 불가 조합일 때 true)
    public bool isRuneError = false;

    // ──────────────── 이동 ────────────────
    public Rigidbody2D rigid;
    // 프리팹 기본 스케일 (재사용 시 복원용)
    Vector3 defaultScale;
    // 원거리 탄환 생존 시간
    float lifetime = 4f;
    float timer;

    // ──────────────── 런타임 에러 파티클 ────────────────
    // 인스펙터에서 연결 (없어도 동작은 함)
    [SerializeField] ParticleSystem errorParticle;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        defaultScale = transform.localScale;
    }

    // ─────────────────────────────────────────────────────
    // 초기화 (Weapon.cs의 Fire()에서 호출)
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// 탄환 초기화. 룬 리스트를 받아 컴포넌트로 추가한다.
    /// </summary>
    public void Init(float damage, int per, Vector2 dir, int weaponCount,
                     List<RuneData> runeDataList, bool isCustomScale = false)
    {
        this.damage = damage;
        this.per    = per;
        isMelee     = per <= -1;

        // 스케일 초기화
        transform.localScale = defaultScale;
        if (isCustomScale)
            transform.localScale = defaultScale * (1f + weaponCount * 0.2f);

        // ── 룬 초기화 ──
        ClearRunes();
        this.runeDataList = runeDataList ?? new List<RuneData>();
        isRuneError = false;
        ApplyRunes();

        // ── 이동 초기화 ──
        if (isMelee || rigid == null) return;
        Vector2 n = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.right;
        rigid.linearVelocity = n * 15f;
        float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ─────────────────────────────────────────────────────
    // 룬 적용 내부 로직
    // ─────────────────────────────────────────────────────
    void ApplyRunes()
    {
        foreach (var data in runeDataList)
        {
            if (data == null) continue;
            IBulletRune rune = CreateRune(data.runeType);
            if (rune == null) continue;
            rune.Init(data);
            runes.Add(rune);
        }

        // 호환성 검사 — 런타임 에러 판정
        if (!RuneValidator.IsValidCombination(runeDataList))
        {
            isRuneError = true;
            TriggerRuneError();
        }
    }

    // 룬 타입 → IBulletRune 인스턴스 생성
    IBulletRune CreateRune(RuneType type)
    {
        return type switch
        {
            // ── 궤적 ──
            RuneType.Wave      => new WaveRune(),
            RuneType.Orbit     => new OrbitRune(),
            RuneType.Ricochet  => new RicochetRune(),
            RuneType.Blink     => new BlinkRune(),
            RuneType.Return    => new ReturnRune(),
            // ── 로직 ──
            RuneType.Homing    => new HomingRune(),
            RuneType.Split     => new SplitRune(),
            RuneType.Chain     => new ChainRune(),
            RuneType.Recursion => new RecursionRune(),
            RuneType.Delay     => new DelayRune(),
            // ── 속성 ──
            RuneType.Explode   => new ExplodeRune(),
            RuneType.Gravity   => new GravityRune(),
            RuneType.Freeze    => new FreezeRune(),
            RuneType.Growth    => new GrowthRune(),
            RuneType.Vampire   => new VampireRune(),
            _                  => null
        };
    }

    void ClearRunes()
    {
        runes.Clear();
        runeDataList = new List<RuneData>();
    }

    // ─────────────────────────────────────────────────────
    // 런타임 에러 연출
    // ─────────────────────────────────────────────────────
    void TriggerRuneError()
    {
        // 탄환을 즉시 멈추고
        if (rigid != null) rigid.linearVelocity = Vector2.zero;
        // 파티클 재생 후 소멸 (파티클이 없으면 그냥 소멸)
        if (errorParticle != null)
        {
            errorParticle.Play();
            Invoke(nameof(Deactivate), 0.3f);
        }
        else
        {
            Deactivate();
        }
    }

    // ─────────────────────────────────────────────────────
    // Unity 이벤트
    // ─────────────────────────────────────────────────────
void Update()
{
    if (isRuneError) return;

    float delta = Time.deltaTime;
    var runeSnapshot = new List<IBulletRune>(runes);

    // ✅ Orbit 룬은 근접 무기여도 Update 실행
    bool hasOrbit = runeSnapshot.Exists(r => r is OrbitRune);

    if (!isMelee || hasOrbit)
    {
        foreach (var rune in runeSnapshot)
            rune.OnUpdate(this, delta);
    }

    if (isMelee) return; // 생존 시간 체크는 근접 무기 제외

    timer += delta;
    if (timer >= lifetime)
        ExpireAndDeactivate();
}

    void OnEnable()
    {
        timer = 0f;
        isRuneError = false;
    }

    void OnDisable()
    {
        transform.localScale = Vector3.one;
        ClearRunes();
    }

void OnTriggerEnter2D(Collider2D collision)
{
    if (!collision.CompareTag("Enemy") || per == -1 || isRuneError) return;

    // ✅ 순회 전 복사본 생성 — SpawnCopy가 runes를 건드려도 안전
    var runeSnapshot = new List<IBulletRune>(runes);

    bool keepAlive = false;
    foreach (var rune in runeSnapshot)
    {
        if (rune.OnHit(this, collision))
            keepAlive = true;
    }

    if (!keepAlive)
    {
        per--;
        if (per == -1)
        {
            if (rigid != null) rigid.linearVelocity = Vector2.zero;
            Deactivate();
        }
    }
}

    // ─────────────────────────────────────────────────────
    // 소멸 처리
    // ─────────────────────────────────────────────────────
    void ExpireAndDeactivate()
    {
        foreach (var rune in runes)
            rune.OnExpire(this);
        Deactivate();
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────
    // 외부 접근용 유틸리티
    // ─────────────────────────────────────────────────────
    // 풀에서 새 탄환을 가져와 현재 룬 구성으로 복사 발사 (Split/Recursion용)
   public BulletRune SpawnCopy(Vector2 dir, float damageOverride = -1f,
    params RuneType[] excludeRunes)
{
    GameObject obj = GameManager.instance.pool.Get(prefabId);
    BulletRune copy = obj.GetComponent<BulletRune>();
    if (copy == null) return null;

    // 제외할 룬만 빼고 나머지 전부 유지
    var newList = new List<RuneData>(runeDataList);
    newList.RemoveAll(r => r != null &&
        System.Array.IndexOf(excludeRunes, r.runeType) >= 0);

    copy.transform.position = transform.position;
    float dmg = damageOverride >= 0 ? damageOverride : damage;
    copy.Init(dmg, per, dir, 0, newList);
    return copy;
}
}
