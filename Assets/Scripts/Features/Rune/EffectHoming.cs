using UnityEngine;

public class EffectHoming : RuneEffect
{
    private Transform target;



    public override void InitEffect(WeaponInstance instance, Motion motion, RuneData data)
    {
        base.InitEffect(instance, motion, data);
        FindTarget();
    }


    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            FindTarget();
            return;
        }

        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float rotationSpeed = parentMotion.instance.speed * data.power;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }


    private void FindTarget()
    {
		float radius = data.duration > 0f ? data.duration : 10f;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);
        float minDistance = Mathf.Infinity;

        foreach (var enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = enemy.transform;
                }
            }
        }
    }
}