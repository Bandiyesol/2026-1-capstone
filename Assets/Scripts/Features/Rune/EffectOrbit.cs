using UnityEngine;

public class EffectOrbit : RuneEffect
{
	private float rotation;
	private float angle;
	private float radius;
	private Vector3 point;



	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		rotation = 0f;
		angle = 0f;
		radius = data.range * instance.size;
		point = transform.position;
	}


	private void Update()
	{
		float speed = parentMotion.instance.speed * data.power;
		float step = speed * Time.deltaTime;

		angle += step;
		rotation += step;

		float x = Mathf.Cos(angle) * radius;
		float y = Mathf.Sin(angle) * radius;

		transform.position = point + new Vector3(x, y, 0f);

		if (rotation >= data.duration) parentMotion.ExecuteRune();

	}
}