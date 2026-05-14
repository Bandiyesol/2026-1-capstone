using UnityEngine;


public class EffectGrowth : RuneEffect
{
	private float growthTimer = 0f;
	private float maxGrowth = 5f;

	private void Update()
	{
		if (growthTimer < 2f)
		{
			growthTimer += Time.deltaTime;
			float scale = 1f + (1f + (growthTimer * maxGrowth));
			transform.localScale = new Vector3(scale, scale, 1f);
		}
	}
}