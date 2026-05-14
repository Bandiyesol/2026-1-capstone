using UnityEngine;

public class EffectRicochet : RuneEffect
{
	public override bool ManualCollision => true;
    private int bounce;



    public override void InitEffect(WeaponInstance instance, Motion motion, RuneData data)
    {
        base.InitEffect(instance, motion, data);
		bounce = data.count;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.CompareTag("Wall"))
        {
            if (bounce > 0)
            {
                bounce--;
                Bounce(collision);
            }

            else parentMotion.ExecuteRune();
        }
    }

    private void Bounce(Collider2D collision)
    {
		float offset = Random.Range(-30f, 30f);
        transform.right = -transform.right; 
		transform.Rotate(0f, 0f, offset);
    }
}