using UnityEngine;

[CreateAssetMenu(fileName = "GravityRune", menuName = "RuneData/State/Gravity")]
public class GravityRuneData : RuneData
{
	[Tooltip("중력 당김 반경(월드 단위). EffectGravity에서 weapon.size로 추가 확장")]
	public float pullRadius = 6f;
	public float duration;
	public float pullForce;
}
