using UnityEngine;
 
[CreateAssetMenu(fileName = "Rune", menuName = "Scriptable Object/RuneData")]
public class RuneData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public string runeName;	
	public RuneCategory category;
	public RuneType runeType;
    public Sprite runeIcon;
	[TextArea] public string runeDescription;
	[Tooltip("룬 기능 완료 후 즉시 탄환 소멸 요청")] public bool isDestroyed;
	[Tooltip("데미지 배율만 (DamageCalculator). 이동/회전에는 쓰지 않음")] public float power;
}


public enum RuneCategory { Active, Trigger, Final, State, Logic }
public enum RuneType 
{ 
    Orbit, Wave, Spiral, Homing, 
    Split, Ricochet, Vampire, Freeze, Chain, Explode, 
    Recursion, 
    Gravity, Growth, 
    Blink, Boing 
}