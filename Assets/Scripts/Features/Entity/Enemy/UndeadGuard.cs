using UnityEngine;

/// <summary>
/// 보스 수호대 전용 몬스터 (UndeadGuard)
/// 기존 구조 유지, 부모 Enemy 클래스의 시스템 드랍/카운트를 안전하게 바이패스합니다.
/// </summary>
public class UndeadGuard : Enemy
{
    [Header("==== [ Guard Properties ] ====")]
    [Tooltip("수호대가 배정받은 고유 원형 슬롯 인덱스 (0~7)")]
    [SerializeField] private int slotIndex;

    // --- 내부 제어 변수 ---
    private ImmortalUndeadBoss targetBoss; // 관리 및 통보 대상인 보스 본체 참조
    private float orbitAngle;              // 보스를 중심으로 배치될 고유 공전 각도 (라디안 단위)
    private float orbitRadius;             // 보스로부터 유지할 배치 반경 거리

    /// <summary>
    /// 수호대의 초기 상태 및 보스 관련 링크 데이터를 설정하고 활성화하는 초기화 메서드
    /// </summary>
    public void InitializeGuard(ImmortalUndeadBoss boss, int slot, float angle, float radius)
    {
        targetBoss = boss;
        slotIndex = slot;
        orbitAngle = angle;
        orbitRadius = radius;

        // 부모(Enemy) 클래스의 생존 플래그를 true로 설정하여 활동 시작
        isLive = true;
    }

    private void Update()
    {
        // 수호대가 살아있고, 추적할 보스가 존재할 때만 위치 유지 로직 수행
        if (isLive && targetBoss != null)
        {
            MaintainOrbitPosition();
        }
    }

    /// <summary>
    /// 보스의 현재 위치를 기준으로 지정된 각도와 반경에 맞춰 자신의 위치를 동기화하는 메서드
    /// </summary>
    private void MaintainOrbitPosition()
    {
        // 삼각함수(Cos, Sin)를 이용해 보스 중심의 2D 평면 원형 오프셋 좌표 계산
        Vector3 orbitOffset = new Vector3(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle), 0f) * orbitRadius;

        // 보스의 실시간 위치에 오프셋을 더해 최종 월드 좌표 갱신 (보스를 따라 움직임)
        transform.position = targetBoss.transform.position + orbitOffset;
    }

    /// <summary>
    /// 체력이 다했거나 사망 조건 충족 시 호출되는 피격 및 사망 처리 오버라이드 메서드
    /// </summary>
    protected override void Die()
    {
        // 이미 사망 처리 중이라면 중복 실행 방지를 위해 차단
        if (!isLive) return;

        isLive = false; // 사망 상태로 전환

        // 보스 본체에 자신의 고유 슬롯 번호를 넘겨주며 사망 사실을 통보 (부활 타이머 트리거 목적)
        if (targetBoss != null)
        {
            targetBoss.OnGuardDead(slotIndex);
        }

        // [주의] 일반 몬스터의 코인/상자 드랍 및 WaveManager 마리수 카운트 감산 처리를 
        // 전면 우회(Bypass)하기 위해 부모의 base.Die()는 고의적으로 호출하지 않음.
        gameObject.SetActive(false); // 오브젝트 풀 반환 및 비활성화
    }
}