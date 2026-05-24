using UnityEngine;
using System.Collections.Generic;

public class MotionOrb : Motion
{
    private float ticktimer;
    private List<IDamageable> targetsInRange = new List<IDamageable>();


    protected override void OnStartMotion() {}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


    protected override void Update()
    {
        base.Update();

		ticktimer += Time.deltaTime;
		if (ticktimer >= instance.attackspeed)
		{
			ApplyTickDamage();
			ticktimer = 0f;
		}
    }


    private void ApplyTickDamage()
    {
		float calculatedTickDamage = DamageCalculator.CalculateBaseDamage(instance, null);
        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            var target = targetsInRange[i];

            if (target != null) target.TakeDamage(calculatedTickDamage);
            else targetsInRange.RemoveAt(i);
        }
    }


    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null && !targetsInRange.Contains(target)) targetsInRange.Add(target);
        }

		base.OnTriggerEnter2D(collision);
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null)  targetsInRange.Remove(target);
        }
    }
}