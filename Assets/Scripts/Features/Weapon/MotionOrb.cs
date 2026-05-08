using UnityEngine;
using System.Collections.Generic;

public class MotionOrb : Motion
{
    private float timer;
    private List<IDamageable> targetsInRange = new List<IDamageable>();


    protected override void OnStart()
    {
        Destroy(gameObject, instance.reach); 
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= instance.speed)
        {
            ApplyTickDamage();
            timer = 0f;
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            IDamageable target = collision.GetComponent<IDamageable>();

            if (target != null && !targetsInRange.Contains(target)) targetsInRange.Add(target);
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