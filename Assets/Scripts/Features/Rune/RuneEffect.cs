using UnityEngine;


public abstract class RuneEffect : MonoBehaviour
{
	protected WeaponInstance weapon;
	protected Motion parentMotion;
	protected RuneData data;

	public virtual bool ManualCollision => false;
	
	public virtual void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		weapon = instance;
		parentMotion = motion;
		data = runeData;
	}
}