using UnityEngine;
using System.Collections.Generic;

public class MotionOrb : Motion
{
    private float ticktimer;
	private float lifetimer;
    private List<IDamageable> targetsInRange = new List<IDamageable>();



    protected override void OnStartMotion() {}


    protected override void Update()
    {
        ticktimer += Time.deltaTime;
        if (ticktimer >= instance.speed)
        {
            ApplyTickDamage();
            ticktimer = 0f;
        }

		lifetimer += Time.deltaTime;
		if (lifetimer >= instance.spawntime) CheckDestruction();
    }


    private void ApplyTickDamage()
    {
        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            var target = targetsInRange[i];

            if (target != null) target.TakeDamage(instance.damage);
            else targetsInRange.RemoveAt(i);
        }
    }


	protected override void DefaultHitEffect(Collider2D collision)
	{
		if (collision.CompareTag("Enemy"))
		{
			IDamageable enemy = collision.GetComponent<IDamageable>();
			if (enemy != null && !targetsInRange.Contains(enemy)) targetsInRange.Add(enemy);
		}
	}


	protected override void HandleCollision(Collider2D collision) => base.HandleCollision(collision);
	

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null && !targetsInRange.Contains(target)) targetsInRange.Add(target);

			ExecuteRune();
        }
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