using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 장판형 또는 특정 영역에 머무르며 주기적인 피해를 입히는 마법 구체(오브) 클래스
/// </summary>
public class MotionOrb : Motion
{
    // 주기적인 틱(Tick) 데미지를 주기 위해 시간을 재는 타이머
    private float ticktimer;

    // 현재 오브의 공격 범위(콜라이더) 안에 들어와 있는 적들의 리스트
    private List<IDamageable> targetsInRange = new List<IDamageable>();

    protected override void OnStartMotion()
    {
        ResetOrbState();
    }

    // 오브의 유지 시간은 스탯의 스폰타임으로 설정
    protected override float GetDefaultTime() => instance.spawntime;

    // 범위형이므로 적과 부딪혔다고 파괴되지 않고 유지됨
    protected override bool ShouldDestroyOnHit() => false;

    /// <summary>
    /// 매 프레임 타이머를 체크하여 일정 공격 속도(주기)마다 데미지를 입힙니다.
    /// </summary>
    protected override void Update()
    {
        base.Update();

        if (instance == null)
            return;

        // 프레임 경과 시간을 틱 타이머에 누적
        ticktimer += Time.deltaTime;

        // 누적 시간이 무기 공격 속도(틱 간격)를 넘어서면
        float tickInterval = Mathf.Max(0.05f, instance.attackspeed);
        if (ticktimer >= tickInterval)
        {
            // 범위 내 적들에게 데미지 적용
            ApplyTickDamage();
            // 타이머 초기화 후 다시 계산
            ticktimer = 0f;
        }
    }

    /// <summary>
    /// 범위 안에 있는 모든 적에게 일괄적으로 틱 데미지를 가합니다.
    /// </summary>
    private void ApplyTickDamage()
    {
        // 룬 적용 없는 오브의 기본 틱 데미지 계산
        float calculatedTickDamage = DamageCalculator.CalculateBaseDamage(instance, null);

        // 리스트를 역순으로 순회 (순회 중 적이 죽어서 null이 되거나 리스트에서 삭제될 때 인덱스 오류 방지)
        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            var target = targetsInRange[i];

            // 타겟이 살아있다면 데미지를 주고
            if (target != null) target.TakeDamage(calculatedTickDamage);
            // 이미 죽어서 컴포넌트가 파괴된 타겟이라면 리스트에서 제거
            else targetsInRange.RemoveAt(i);
        }
    }

    /// <summary>
    /// 적이 오브 영역(Collider 트리거) 안으로 들어왔을 때 리스트에 추가합니다.
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 대상이 적인지 태그로 확인
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            // 리스트에 없는 새로운 적이라면 데미지 대상 리스트에 등록
            if (target != null && !targetsInRange.Contains(target)) targetsInRange.Add(target);
        }

        // 부모의 기본 충돌 처리 (트리거 룬 발동 등) 연계
        base.OnTriggerEnter2D(collision);
    }

    /// <summary>
    /// 적이 오브 영역 밖으로 빠져나갔을 때 데미지 리스트에서 제외합니다.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            // 대상 리스트에서 안전하게 제거하여 더 이상 틱 데미지를 받지 않도록 함
            if (target != null) targetsInRange.Remove(target);
        }
    }

    public override void ResetForPool()
    {
        base.ResetForPool();
        ResetOrbState();
    }

    void ResetOrbState()
    {
        ticktimer = 0f;
        targetsInRange.Clear();
    }
}