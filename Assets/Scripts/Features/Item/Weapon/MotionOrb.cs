using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ������ �Ǵ� Ư�� ������ �ӹ����� �ֱ����� ���ظ� ������ ���� ��ü(����) Ŭ����
/// </summary>
public class MotionOrb : Motion
{
    // �ֱ����� ƽ(Tick) �������� �ֱ� ���� �ð��� ��� Ÿ�̸�
    private float ticktimer;

    // ���� ������ ���� ����(�ݶ��̴�) �ȿ� ���� �ִ� ������ ����Ʈ
    private List<IDamageable> targetsInRange = new List<IDamageable>();

    protected override void OnStartMotion()
    {
        ResetOrbState();
    }

    // ������ ���� �ð��� ������ ����Ÿ������ ����
    protected override float GetDefaultTime() => instance.spawntime;

    // �������̹Ƿ� ���� �ε����ٰ� �ı����� �ʰ� ������
    protected override bool ShouldDestroyOnHit() => false;

    /// <summary>
    /// �� ������ Ÿ�̸Ӹ� üũ�Ͽ� ���� ���� �ӵ�(�ֱ�)���� �������� �����ϴ�.
    /// </summary>
    protected override void Update()
    {
        base.Update();

        if (instance == null)
            return;

        // ������ ��� �ð��� ƽ Ÿ�̸ӿ� ����
        ticktimer += Time.deltaTime;

        // ���� �ð��� ƽ ������ �Ѿ�� (���� ���� �ݿ�)
        float effectiveTickInterval = instance.ResolveEffectiveTickInterval();
        if (ticktimer >= effectiveTickInterval)
        {
            // ���� �� ���鿡�� ������ ����
            ApplyTickDamage();
            // Ÿ�̸� �ʱ�ȭ �� �ٽ� ���
            ticktimer = 0f;
        }
    }

    /// <summary>
    /// ���� �ȿ� �ִ� ��� ������ �ϰ������� ƽ �������� ���մϴ�.
    /// </summary>
    private void ApplyTickDamage()
    {
        // �� ���� ���� ������ �⺻ ƽ ������ ���
        float calculatedTickDamage = DamageCalculator.CalculateBaseDamage(instance, null);

        // ����Ʈ�� �������� ��ȸ (��ȸ �� ���� �׾ null�� �ǰų� ����Ʈ���� ������ �� �ε��� ���� ����)
        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            var target = targetsInRange[i];

            // Ÿ���� ����ִٸ� �������� �ְ�
            if (target != null) target.TakeDamage(calculatedTickDamage);
            // �̹� �׾ ������Ʈ�� �ı��� Ÿ���̶�� ����Ʈ���� ����
            else targetsInRange.RemoveAt(i);
        }
    }

    /// <summary>
    /// ���� ���� ����(Collider Ʈ����) ������ ������ �� ����Ʈ�� �߰��մϴ�.
    /// </summary>
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // ����� ������ �±׷� Ȯ��
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            // ����Ʈ�� ���� ���ο� ���̶�� ������ ��� ����Ʈ�� ���
            if (target != null && !targetsInRange.Contains(target)) targetsInRange.Add(target);
        }

        // �θ��� �⺻ �浹 ó�� (Ʈ���� �� �ߵ� ��) ����
        base.OnTriggerEnter2D(collision);
    }

    /// <summary>
    /// ���� ���� ���� ������ ���������� �� ������ ����Ʈ���� �����մϴ�.
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            // ��� ����Ʈ���� �����ϰ� �����Ͽ� �� �̻� ƽ �������� ���� �ʵ��� ��
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