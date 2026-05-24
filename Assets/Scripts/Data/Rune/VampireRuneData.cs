using UnityEngine;

[CreateAssetMenu(fileName = "VampireRune", menuName = "RuneData/Trigger/Vampire")]
public class VampireRuneData : RuneData
{
	[Tooltip("흡혈 쿨타임(초)")]
	public float interval = 0.5f;
}
