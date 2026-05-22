using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 추가


public class Weapon : MonoBehaviour
{
    // 무기 타입 ID (0: 회전 근접, 1: 원거리, 2: 소드)
    public int id;
    // 풀 매니저에서 사용할 프리팹 인덱스
    public int prefabId;
    // 현재 공격력
    public float damage;
    // 무기 개수/관통/스케일 등에 사용되는 수치
    public int count;
    // 발사 간격 또는 회전 속도 등 타입별 제어 값 
    public float speed;

    // 주기성 공격용 타이머
    float timer;
    // 소유자 플레이어 참조
    Player player;

    void Awake()
    {
        // 무기 생성 시 게임 매니저의 플레이어 참조 캐싱
        player = GameManager.instance.player;
    }

    void Update()
    {
        // 게임 정지 상태에서는 공격 처리 중단
        if (!GameManager.instance.isLive)
            return;
            
        // 무기 타입별 동작 분기
        switch (id)
        {
            case 0:
                // 플레이어 주변 회전형 무기
                transform.Rotate(Vector3.back * speed * Time.deltaTime);
                break;
            case 1:
                // 원거리 무기: 주기적으로 발사
                timer += Time.deltaTime;
                if (timer > speed)
                {
                    timer = 0f;
                    Fire();
                }
                break;
            case 2:
                // 소드 무기: 주기적으로 근접 히트박스 생성
            timer += Time.deltaTime;
            if (timer > speed) {
                timer = 0f;
                Swing();
            }
            break;
        }

        // 테스트용 즉시 강화 단축키
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LevelUp(20, 5);
        }
    }

    public void LevelUp(float damage, int per)
    {
        // 외부(아이템 선택 등)에서 전달된 현재 레벨 스탯 반영
        this.damage = damage;
        this.count = per;

        // 회전형 무기는 개수 변화가 즉시 배치에 반영되어야 함
        if (id == 0)
            Batch();
    }

    public void Init(ItemData data)
    {
        // 기본 세팅: 이름/부모/초기 위치
        name = "Weapon " + data.itemId;
        transform.parent = player.transform;
        transform.localPosition = Vector3.zero;

        // 아이템 데이터 기반 기본 능력치 적용
        id = data.itemId;
        damage = data.baseDamage;
        count = data.baseCount;

        // 아이템의 발사체 프리팹이 풀 매니저의 몇 번 인덱스인지 검색
        for (int index = 0; index < GameManager.instance.pool.projectilePrefabs.Length; index++)
        {
            if (data.projectile == GameManager.instance.pool.projectilePrefabs[index])
            {
                prefabId = index;
                break;
            }
        }

        // 타입별 초기 동작 파라미터 설정
        switch (id)
        {
            case 0:
                speed = -150;
                Batch();
                break;
            case 1:
            float baseCooldown = 0.3f;
            float penalty = RuneManager.instance != null
            ? RuneManager.instance.GetTotalCooldownPenalty()
            : 0f;
            speed = baseCooldown + penalty;
            break;
            case 2:
                // 값이 클수록 공격 간격이 길어짐
                speed = 0.8f;
            break;
        }
    }

    void Batch()
    {
        // ✅ 현재 장착된 룬 가져오기
        List<RuneData> runes = RuneManager.instance != null
            ? RuneManager.instance.GetActiveRunes()
            : new List<RuneData>();

        for (int index = 0; index < count; index++)
        {
            Transform bullet;

            if (index < transform.childCount)
                bullet = transform.GetChild(index);
            else
            {
                bullet = GameManager.instance.pool.GetProjectile(prefabId).transform;
                bullet.parent = transform;
            }

            bullet.localPosition = Vector3.zero;
            bullet.localRotation = Quaternion.identity;

            Vector3 rotVec = Vector3.forward * 360 * index / count;
            bullet.Rotate(rotVec);
            bullet.Translate(bullet.up * 1f, Space.World);

            BulletRune br = bullet.GetComponent<BulletRune>();
            if (br != null)
            {
                br.prefabId = prefabId;
                // ✅ 룬 리스트 전달
                br.Init(damage, -1, Vector2.zero, count, runes, false);
            }
            else
                bullet.GetComponent<Bullet>()?.Init(damage, -1, Vector2.zero, count, false);
        }
    }
    void Fire()
    {
        Vector3 dir = player.GetFacingDirection();
        Vector2 dir2 = new Vector2(dir.x, dir.y);
        if (dir2.sqrMagnitude < 1e-6f) dir2 = Vector2.right;

        Transform bulletTf = GameManager.instance.pool.GetProjectile(prefabId).transform;
        bulletTf.SetParent(this.transform);
        bulletTf.localPosition = Vector3.zero;

        List<RuneData> runes = RuneManager.instance != null
            ? RuneManager.instance.GetActiveRunes()
            : new List<RuneData>();

        // ── 디버그 추가 ──
        BulletRune br = bulletTf.GetComponent<BulletRune>();

        if (br != null)
        {
            br.prefabId = prefabId;
            br.Init(damage, count, dir2.normalized, count, runes);
        }
        else
            bulletTf.GetComponent<Bullet>()?.Init(damage, count, dir2.normalized, count, false);
    }

    void Swing()
    {
        // 소드는 단일 히트박스를 생성하고 count를 크기 배율로 활용
        for (int i = 0; i < 1; i++)
        {
            Transform bullet = GameManager.instance.pool.GetProjectile(prefabId).transform;
            bullet.SetParent(this.transform);

            // 크기에 비례해 생성 위치를 전방으로 조금 더 보정
            float dist = 0.4f * (1 + count * 0.2f);
            bullet.localPosition = player.spriter.flipX ? Vector3.left * dist : Vector3.right * dist;

            // count에 비례한 스케일 확대
            float scaleMultiplier = 1.0f + (count * 0.2f); 
            bullet.localScale = Vector3.one * scaleMultiplier;

            // 플레이어 바라보는 방향에 맞게 각도 설정
            float baseAngle = player.spriter.flipX ? 180f : 0f;
            bullet.rotation = Quaternion.Euler(0, 0, baseAngle);

            // 근접 히트박스는 물리 속도를 사용하지 않음
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            // per=-1로 전달해 근접 무기 로직 사용, 스케일 보정 활성화
            bullet.GetComponent<Bullet>().Init(damage, -1, Vector2.zero, count, true);
        }
    }

}
