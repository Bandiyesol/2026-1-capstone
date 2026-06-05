using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 펌킨킹 보스 컨트롤러 (피격 소환 + 체력 페이즈별 증식 + 유도탄 부모 클래스 상속)
/// </summary>
public class PumpkinKing : BossBase
{
    [Header("소환 설정")]
    [SerializeField] int summonMonsterIndex; // 풀링에서 가져올 소환 몹 인덱스
    [SerializeField] int summonCount = 1;       // 피격 시 기본 소환 마리 수
    [SerializeField] float summonRadius = 2.5f;   // 보스 기준 소환 무작위 반경

    [Header("유도탄 설정")]
    [SerializeField] int homingBulletIndex;  // 풀링에서 사용할 유도탄 인덱스

    // 페이즈 전역 변수
    int summonMultiplier = 1; // 체력 단계에 따라 배로 증가할 소환 배율

    // 페이즈 중복 트리거 방지 플래그
    bool phase80Triggered;
    bool phase60Triggered;
    bool phase40Triggered;
    bool phase20Triggered;

    // ==========================================
    // 🔥 메모리 관리 리스트 분리 (몬스터 vs 탄막)
    // ==========================================

    List<GameObject> summonedMonsters = new List<GameObject>(); // 소환된 부하 몬스터 추적 리스트
    List<GameObject> spawnedObjects = new List<GameObject>();   // 발사된 유도 탄막 추적 리스트

    // 오브젝트 활성화 시 데이터 및 리스트 초기화
    protected override void OnEnable()
    {
        base.OnEnable();

        summonMultiplier = 1; // 소환 배율 초기화

        // 페이즈 플래그 초기화
        phase80Triggered = false;
        phase60Triggered = false;
        phase40Triggered = false;
        phase20Triggered = false;

        // 잔존 데이터 완전 클리어
        summonedMonsters.Clear();
        spawnedObjects.Clear();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 부모 클래스의 물리/이동 업데이트 유지
    }

    // 메인 패턴 타이머 충족 시 호출 (기본 시스템 오버라이드)
    protected override void StartRandomPattern()
    {
        FireHomingShot(); // 유도탄 발사 패턴 실행
    }

    // 피격 이벤트 오버라이드
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage); // 부모의 실 데미지 및 체력 연산

        // 사망 시 피격 소환 및 페이즈 검사 스킵
        if (health <= 0)
            return;

        SummonOnHit(); // 피격 시 부하 몬스터 소환
        CheckPhase();  // 체력 상태에 따른 페이즈 전환 검사
    }

    // ==========================================
    // 몬스터 소환 로직
    // ==========================================
    void SummonOnHit()
    {
        // 기본 소환 수 × 현재 페이즈 배율 계산
        int count = summonCount * summonMultiplier;

        for (int i = 0; i < count; i++)
        {
            // 오브젝트 풀에서 부하 몬스터 획득
            GameObject enemy =
                GameManager.instance.pool.GetEnemy(summonMonsterIndex);

            if (enemy == null)
                continue;

            // 보스 주변 원형 범위 내 무작위 스폰 좌표 연산
            Vector2 offset = Random.insideUnitCircle * summonRadius;

            enemy.transform.position =
                (Vector2)transform.position + offset;

            enemy.SetActive(true); // 몬스터 활성화 (AI 가동)

            summonedMonsters.Add(enemy); // 몬스터 전용 추적 리스트에 등록
        }
    }

    // ==========================================
    // 탄막 발사 로직
    // ==========================================
    void FireHomingShot()
    {
        if (target == null)
            return;

        // 명시적 형변환을 통한 타겟(플레이어) 방향 벡터 연산
        Vector2 dir =
            ((Vector2)target.position - (Vector2)transform.position).normalized;

        // 오브젝트 풀에서 보스 유도탄 획득
        GameObject bullet =
            GameManager.instance.pool.GetBossBullet(homingBulletIndex);

        if (bullet == null)
            return;

        bullet.transform.position = transform.position; // 보스 중심점 생성

        // 유도탄 컴포넌트 안전 추출 및 방향 전달 초기화
        bullet.GetComponent<HomingBossBullet>()?.Init(dir);

        anim.SetTrigger("Attack"); // 공격 애니메이션 트리거

        bullet.SetActive(true); // 탄막 활성화

        spawnedObjects.Add(bullet); // 탄막 전용 추적 리스트에 등록
    }

    // ==========================================
    // 체력 단계 검사 (페이즈 조건)
    // ==========================================
    void CheckPhase()
    {
        float hpPercent = health / maxHealth; // 실시간 체력 비율 계산

        // [주의] 높은 체력 구간부터 차례대로 조건 검사 및 배율 누적 증가 (*2배씩)
        if (!phase20Triggered && hpPercent <= 0.2f)
        {
            summonMultiplier *= 2;
            phase20Triggered = true;
        }
        else if (!phase40Triggered && hpPercent <= 0.4f)
        {
            summonMultiplier *= 2;
            phase40Triggered = true;
        }
        else if (!phase60Triggered && hpPercent <= 0.6f)
        {
            summonMultiplier *= 2;
            phase60Triggered = true;
        }
        else if (!phase80Triggered && hpPercent <= 0.8f)
        {
            summonMultiplier *= 2;
            phase80Triggered = true;
        }
    }

    // ==========================================
    // 사망 처리 오버라이드
    // ==========================================
    protected override void Dead()
    {
        ClearAllSpawnedObjects(); // 필드 내 잔존 몹 및 탄막 즉시 청소
        base.Dead();               // 부모 클래스의 사망 연출 및 보상 가동
    }

    // ==========================================
    // 몬스터 + 탄막 안전 풀링 반환
    // ==========================================
    void ClearAllSpawnedObjects()
    {
        // 1. 소환된 부하 몬스터 일괄 비활성화
        for (int i = 0; i < summonedMonsters.Count; i++)
        {
            if (summonedMonsters[i] != null &&
                summonedMonsters[i].activeSelf)
            {
                summonedMonsters[i].SetActive(false);
            }
        }

        // 2. 발사된 유도 탄막 일괄 비활성화
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null &&
                spawnedObjects[i].activeSelf)
            {
                spawnedObjects[i].SetActive(false);
            }
        }

        // 3. 리스트 메모리 해제 및 초기화
        summonedMonsters.Clear();
        spawnedObjects.Clear();
    }
}