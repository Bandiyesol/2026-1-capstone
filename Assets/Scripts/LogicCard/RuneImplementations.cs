using System.Collections.Generic;
using UnityEngine;

// ══════════════════════════════════════════════════════════════
//  궤적 룬 (Trajectory) — 탄환의 이동 방식 결정
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Wave (파동): Sine 곡선을 그리며 관통력이 붙는다.
/// valueA = 진폭, valueB = 주파수 배율
/// </summary>
public class WaveRune : IBulletRune
{
    float amplitude;
    float frequency;
    float elapsed;
    Vector2 baseDir;
    Vector2 perpDir;
    
    public void Init(RuneData data)
    {
        amplitude = data.valueA;   // 기본 1f
        frequency = data.valueB;   // 기본 1f
    }

    public void OnUpdate(BulletRune bullet, float delta)
    {
        // 첫 프레임에 기준 방향 계산
        if (elapsed == 0f && bullet.rigid != null)
        {
            baseDir = bullet.rigid.linearVelocity.normalized;
            perpDir = new Vector2(-baseDir.y, baseDir.x); // 수직 방향
        }
        elapsed += delta;

        if (bullet.rigid == null) return;
        float speed   = bullet.rigid.linearVelocity.magnitude;
        float waveOff = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f) * amplitude;
        bullet.rigid.linearVelocity = (baseDir + perpDir * waveOff).normalized * speed;
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy)
    {
        // Wave는 관통력 부여 — per를 소모하지 않고 그냥 통과
        // OnTriggerEnter2D는 직접 호출 불가 → 데미지는 Enemy쪽 충돌에서 처리됨
        return true; // 탄환 유지
    }

    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Orbit (공전): 플레이어 주변을 공전하며 공격한다.
/// 기존 Weapon id=0(회전 근접)을 룬으로 확장한 버전.
/// valueA = 공전 반경, valueB = 공전 속도(deg/s)
/// </summary>
/// <summary>
/// Orbit (공전): 플레이어 주변을 공전하며 공격한다.
/// 기존 Weapon id=0(회전 근접)을 룬으로 확장한 버전.
/// valueA = 공전 반경, valueB = 공전 속도(deg/s)
/// </summary>
public class OrbitRune : IBulletRune
{
    float radius;
    float angularSpeed;
    float angle;
    Transform playerTransform;

    public void Init(RuneData data)
    {
        radius       = data.valueA;
        angularSpeed = data.valueB;
    }

    public void OnUpdate(BulletRune bullet, float delta)
    {
        Debug.Log($"OrbitRune OnUpdate 실행 — angle: {angle}");
        
        if (playerTransform == null)
            playerTransform = GameManager.instance.player.transform;

        if (bullet.transform.parent != null)
            bullet.transform.SetParent(null, true);

        angle += angularSpeed * delta;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
        bullet.transform.position = (Vector2)playerTransform.position + offset;

        if (bullet.rigid != null) bullet.rigid.linearVelocity = Vector2.zero;
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}

/// <summary>
/// Ricochet (도탄): 벽(또는 화면 경계)에서 튕길 때마다 데미지가 증가한다.
/// valueA = 최대 튕김 횟수, valueB = 튕김당 데미지 배율
/// </summary>
public class RicochetRune : IBulletRune
{
    int maxBounce;
    float damageMultiplier;
    int bounceCount;

    public void Init(RuneData data)
    {
        maxBounce        = Mathf.RoundToInt(data.valueA); // 기본 3
        damageMultiplier = data.valueB;                    // 기본 1.5f
    }

    public void OnUpdate(BulletRune bullet, float delta)
    {
        if (bullet.rigid == null || bounceCount >= maxBounce) return;
        // 화면 경계 도달 시 반사 (카메라 뷰포트 기준)
        Vector2 pos = bullet.transform.position;
        Vector2 vel = bullet.rigid.linearVelocity;
        Camera cam  = Camera.main;
        if (cam == null) return;

        Vector3 vMin = cam.ViewportToWorldPoint(Vector3.zero);
        Vector3 vMax = cam.ViewportToWorldPoint(Vector3.one);

        bool reflected = false;
        if (pos.x < vMin.x || pos.x > vMax.x) { vel.x = -vel.x; reflected = true; }
        if (pos.y < vMin.y || pos.y > vMax.y) { vel.y = -vel.y; reflected = true; }

        if (reflected)
        {
            bounceCount++;
            bullet.damage *= damageMultiplier;
            bullet.rigid.linearVelocity = vel;
        }
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Blink (점멸): 일정 거리마다 순간이동하며 충돌 판정을 무시한다.
/// valueA = 점멸 거리 간격
/// </summary>
public class BlinkRune : IBulletRune
{
    float blinkDistance;
    float traveled;

    public void Init(RuneData data) => blinkDistance = data.valueA; // 기본 3f

    public void OnUpdate(BulletRune bullet, float delta)
    {
        if (bullet.rigid == null) return;
        float moved = bullet.rigid.linearVelocity.magnitude * delta;
        traveled += moved;
        if (traveled >= blinkDistance)
        {
            traveled = 0f;
            // 진행 방향으로 blinkDistance만큼 워프
            Vector2 dir = bullet.rigid.linearVelocity.normalized;
            bullet.transform.position += (Vector3)(dir * blinkDistance);
        }
    }

    // 충돌 판정 무시 — OnHit에서 true 반환해 per 소모 차단
    public bool OnHit(BulletRune bullet, Collider2D enemy) => true;
    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Return (회귀): 최대 사거리까지 갔다가 돌아오며, 돌아올 때 데미지 대폭 증가.
/// valueA = 왕복 거리, valueB = 귀환 시 데미지 배율
/// </summary>
public class ReturnRune : IBulletRune
{
    float maxDistance;
    float returnDamageMult;
    Vector2 startPos;
    Vector2 outDir;
    bool returning;
    float distTraveled;

    public void Init(RuneData data)
    {
        maxDistance      = data.valueA; // 기본 6f
        returnDamageMult = data.valueB; // 기본 3f
    }

    public void OnUpdate(BulletRune bullet, float delta)
    {
        if (bullet.rigid == null) return;

        if (!returning)
        {
            // 첫 프레임에 출발 지점·방향 기록
            if (distTraveled == 0f)
            {
                startPos = bullet.transform.position;
                outDir   = bullet.rigid.linearVelocity.normalized;
            }
            distTraveled += bullet.rigid.linearVelocity.magnitude * delta;
            if (distTraveled >= maxDistance)
            {
                returning = true;
                bullet.damage *= returnDamageMult;
                // 반대 방향으로 속도 전환
                float speed = bullet.rigid.linearVelocity.magnitude;
                bullet.rigid.linearVelocity = -outDir * speed;
            }
        }
        else
        {
            // 출발 지점 근처 도달 시 소멸
            float dist = Vector2.Distance(bullet.transform.position, startPos);
            if (dist < 0.3f)
                bullet.gameObject.SetActive(false);
        }
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}


// ══════════════════════════════════════════════════════════════
//  로직 룬 (Logic) — 실행 방식 결정
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Homing (유도): 가장 가까운 적을 향해 탄환 방향을 서서히 회전한다.
/// valueA = 유도 강도(회전 속도 deg/s)
/// </summary>
public class HomingRune : IBulletRune
{
    float turnSpeed;

    public void Init(RuneData data) => turnSpeed = data.valueA; // 기본 180f

    public void OnUpdate(BulletRune bullet, float delta)
    {
        if (bullet.rigid == null) return;
        Transform target = GameManager.instance.player.scaner.nearestTarget;
        if (target == null) return;

        Vector2 toTarget = ((Vector2)target.position - (Vector2)bullet.transform.position).normalized;
        Vector2 current  = bullet.rigid.linearVelocity.normalized;
        float speed      = bullet.rigid.linearVelocity.magnitude;

        // Vector2.RotateTowards 대신 각도 보간으로 처리
        float currentAngle = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
        float targetAngle  = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float newAngle     = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * delta);
        Vector2 newDir     = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));
        bullet.rigid.linearVelocity = newDir * speed;

        float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Split (분열): 적중 시 탄환이 3방향으로 복제된다.
/// valueA = 분열 개수, valueB = 분열 탄환 데미지 비율
/// </summary>
public class SplitRune : IBulletRune
{
    int splitCount;
    float damageFraction;

    public void Init(RuneData data)
    {
        splitCount     = Mathf.Max(2, Mathf.RoundToInt(data.valueA)); // 기본 3
        damageFraction = data.valueB;                                   // 기본 0.5f
    }

    public void OnUpdate(BulletRune bullet, float delta) { }

 public bool OnHit(BulletRune bullet, Collider2D enemy)
{
    if (bullet.rigid == null) return false;
    Vector2 baseDir = bullet.rigid.linearVelocity.normalized;
    float spreadAngle = 360f / splitCount;

    for (int i = 0; i < splitCount; i++)
    {
        float ang = spreadAngle * i;
        Vector2 dir = Quaternion.Euler(0, 0, ang) * baseDir;
        // Split만 제외하고 나머지 룬 전부 유지
        bullet.SpawnCopy(dir, bullet.damage * damageFraction, RuneType.Split);
    }
    return false;
}
    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Chain (연쇄): 적중 시 주변 적 최대 3명에게 데미지를 전이한다.
/// valueA = 연쇄 거리, valueB = 연쇄 대상 수
/// </summary>
public class ChainRune : IBulletRune
{
    float chainRange;
    int chainCount;

    public void Init(RuneData data)
    {
        chainRange = data.valueA; // 기본 4f
        chainCount = Mathf.RoundToInt(data.valueB); // 기본 3
    }

    public void OnUpdate(BulletRune bullet, float delta) { }

    public bool OnHit(BulletRune bullet, Collider2D enemy)
    {
        // 반경 내 적 탐색
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            enemy.transform.position, chainRange,
            LayerMask.GetMask("Enemy"));

        int hit = 0;
        foreach (var col in nearby)
        {
            if (col == enemy) continue;
            if (hit >= chainCount) break;
            Enemy e = col.GetComponent<Enemy>();
            if (e != null)
            {
                e.health -= bullet.damage;
                hit++;
            }
        }
        return false;
    }

    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Recursion (재귀): 탄환 소멸 시 그 자리에서 동일 탄환을 한 번 더 생성한다.
/// (한 번만 재귀 — 무한 루프 방지 위해 복사 탄환에는 재귀 룬을 제거한다)
/// </summary>
public class RecursionRune : IBulletRune
{
    public void Init(RuneData data) { }
    public void OnUpdate(BulletRune bullet, float delta) { }
    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;

    public void OnExpire(BulletRune bullet)
{
    if (bullet.runeDataList.Count == 0) return;
    Vector2 dir = bullet.rigid != null
        ? bullet.rigid.linearVelocity.normalized
        : Vector2.right;
    // Recursion만 제외하고 나머지 룬 전부 유지
    bullet.SpawnCopy(dir, -1f, RuneType.Recursion);
}
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Delay (지연): 발사 1초 후 실제로 활성화되며, 위력과 범위가 3배가 된다.
/// valueA = 지연 시간(초), valueB = 위력·범위 배율
/// </summary>
public class DelayRune : IBulletRune
{
    float delayTime;
    float multiplier;
    float elapsed;
    bool  activated;

    public void Init(RuneData data)
    {
        delayTime   = data.valueA; // 기본 1f
        multiplier  = data.valueB; // 기본 3f
        elapsed     = 0f;
        activated   = false;
    }

    public void OnUpdate(BulletRune bullet, float delta)
    {
        if (activated) return;
        elapsed += delta;
        if (elapsed < delayTime)
        {
            // 지연 중에는 투명하게 + 충돌 무시
            bullet.GetComponent<Collider2D>().enabled = false;
            SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1, 1, 1, 0.3f);
            return;
        }
        // 지연 종료 → 활성화
        activated = true;
        bullet.GetComponent<Collider2D>().enabled = true;
        SpriteRenderer sr2 = bullet.GetComponent<SpriteRenderer>();
        if (sr2 != null) sr2.color = Color.white;
        bullet.damage    *= multiplier;
        // 스케일 3배 확대
        bullet.transform.localScale *= multiplier;
    }

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}


// ══════════════════════════════════════════════════════════════
//  속성 룬 (Effect) — 최종 타격 효과 결정
// ══════════════════════════════════════════════════════════════

/// <summary>
/// Explode (폭발): 충돌 시 범위 데미지를 준다.
/// valueA = 폭발 반경, valueB = 범위 데미지 배율
/// </summary>
public class ExplodeRune : IBulletRune
{
    float radius;
    float aoeMultiplier;
    static GameObject explosionPrefab;

    public void Init(RuneData data)
    {
        radius        = data.valueA;
        aoeMultiplier = data.valueB;
        if (explosionPrefab == null)
            explosionPrefab = Resources.Load<GameObject>("Explosion");
    }

    public void OnUpdate(BulletRune bullet, float delta) { }

public bool OnHit(BulletRune bullet, Collider2D enemy)
{
    // 폭발 이펙트 생성 + 크기를 반경에 맞게 조절
    if (explosionPrefab != null)
    {
        GameObject explosion = GameObject.Instantiate(explosionPrefab,
            bullet.transform.position, Quaternion.identity);
        // radius * 2 = 지름 크기로 스케일 설정
        float scale = radius * 2f;
        explosion.transform.localScale = Vector3.one * scale;
    }

    Collider2D[] hits = Physics2D.OverlapCircleAll(
        bullet.transform.position, radius,
        LayerMask.GetMask("Enemy"));

    foreach (var col in hits)
    {
        Enemy e = col.GetComponent<Enemy>();
        if (e != null) e.health -= bullet.damage * aoeMultiplier;
    }

    return false;
}

    public void OnExpire(BulletRune bullet) { }  // ← 이거 추가
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Gravity (중력): 충돌 시 블랙홀을 생성해 주변 적을 한곳으로 모은다.
/// valueA = 블랙홀 반경, valueB = 당김 지속 시간(초)
/// </summary>
public class GravityRune : IBulletRune
{
    float pullRadius;
    float duration;

    public void Init(RuneData data)
    {
        pullRadius = data.valueA; // 기본 5f
        duration   = data.valueB; // 기본 2f
    }

    public void OnUpdate(BulletRune bullet, float delta) { }

    public bool OnHit(BulletRune bullet, Collider2D enemy)
    {
        // 블랙홀 코루틴을 GravityZone 오브젝트로 처리
        GameObject zone = new GameObject("GravityZone");
        zone.transform.position = bullet.transform.position;
        zone.AddComponent<GravityZone>().Init(pullRadius, duration, bullet.damage * 0.1f);
        return false;
    }

    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Freeze (빙결): 충돌 시 적의 이동을 일시 정지시킨다.
/// valueA = 빙결 지속 시간(초)
/// </summary>
public class FreezeRune : IBulletRune
{
    float freezeDuration;

    public void Init(RuneData data) => freezeDuration = data.valueA; // 기본 2f

    public void OnUpdate(BulletRune bullet, float delta) { }

    public bool OnHit(BulletRune bullet, Collider2D enemy)
    {
        Enemy e = enemy.GetComponent<Enemy>();
        if (e != null) e.ApplyFreeze(freezeDuration);
        return false;
    }

    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Growth (증폭): 비행 시간에 비례해 탄환이 커지며 데미지가 증가한다.
/// valueA = 최대 스케일 배율, valueB = 초당 성장 속도
/// </summary>
public class GrowthRune : IBulletRune
{
    float maxScale;
    float growthRate;
    float elapsed;
    Vector3 defaultScale;

    public void Init(RuneData data)
    {
        maxScale   = data.valueA; // 기본 5f (화면 절반 수준)
        growthRate = data.valueB; // 기본 1f (초당 증가 배율)
    }

public void OnUpdate(BulletRune bullet, float delta)
{
    // 첫 프레임에 기본 스케일 저장
    if (elapsed == 0f) defaultScale = bullet.transform.localScale;
    elapsed += delta;

    // 시간에 비례해서 스케일 증가
    float scale = Mathf.Min(1f + elapsed * growthRate, maxScale);
    bullet.transform.localScale = defaultScale * scale;

    // 데미지는 스케일에 비례해서 한 번만 계산
    bullet.damage = 3f * scale; // 기본 데미지 * 스케일
}

    public bool OnHit(BulletRune bullet, Collider2D enemy) => false;
    public void OnExpire(BulletRune bullet) { }
}

// ──────────────────────────────────────────────────────────────

/// <summary>
/// Vampire (흡수): 적에게 입힌 데미지만큼 플레이어 체력을 회복시킨다.
/// valueA = 흡수 비율 (0~1)
/// </summary>
public class VampireRune : IBulletRune
{
    float drainRatio;

    public void Init(RuneData data) => drainRatio = Mathf.Clamp01(data.valueA); // 기본 0.3f

    public void OnUpdate(BulletRune bullet, float delta) { }

public bool OnHit(BulletRune bullet, Collider2D enemy)
{
    Enemy e = enemy.GetComponent<Enemy>();
    if (e == null) return false;

    // ✅ 실제 데미지 적용
    float actualDamage = Mathf.Min(bullet.damage, e.health);
    e.health -= actualDamage;

    // 흡수
    GameManager.instance.Health = Mathf.Min(
        GameManager.instance.Health + actualDamage * drainRatio,
        GameManager.instance.maxHealth);

    return false;
}

    public void OnExpire(BulletRune bullet) { }
}


// ══════════════════════════════════════════════════════════════
//  보조 MonoBehaviour — Gravity 블랙홀 존
// ══════════════════════════════════════════════════════════════

/// <summary>
/// 블랙홀 존: 일정 시간 동안 주변 적을 중심으로 끌어당긴다.
/// </summary>
public class GravityZone : MonoBehaviour
{
    float radius;
    float duration;
    float tickDamage;
    float elapsed;

    public void Init(float r, float dur, float dmg)
    {
        radius     = r;
        duration   = dur;
        tickDamage = dmg;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration) { Destroy(gameObject); return; }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, radius, LayerMask.GetMask("Enemy"));

        foreach (var col in hits)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            Vector2 pullDir = ((Vector2)transform.position - rb.position).normalized;
            rb.AddForce(pullDir * 8f);
            // 틱 데미지
            Enemy e = col.GetComponent<Enemy>();
            if (e != null) e.health -= tickDamage * Time.deltaTime;
        }
    }
 }

