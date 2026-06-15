using System;
using System.Collections.Generic;


[Serializable]
public class WeaponDataLoader
{
	public List<WeaponInfo> info;
	public List<WeaponBalance> balance;
}


[Serializable]
public class WeaponInfo
{
	public string id;
	public string name;
	public string spriteId;
	public string motionId;
	/// <summary> Sword, Bow, Orb … — Motion·스폰 분기용 </summary>
	public string type;
	public string grade;
	public string balanceKey;
	/// <summary> Melee / Projectile / Zone — 스탯·UI 표기용 (비우면 type으로 추론) </summary>
	public string weaponCategory;
	/// <summary> 전설(Legendary) 전용 패시브 ID. 비우면 일반 무기. 플레이어 룬 3슬롯과 별개. </summary>
	public string legendaryPassiveId;
}


[Serializable]
public class WeaponBalance
{
	public string key;
	public bool isSplited;
	public bool isRevived;
	public float[] damageRange;
	public float[] weightRange;
	public float[] sizeRange;
	public float[] reachRange;
	public float[] spawntimeRange;
	public float[] cooltimeRange;
	/// <summary> 무기 공격 속도 배율 (높을수록 빠름). cooltime ÷ (playerAS × attackspeed) </summary>
	public float[] attackspeedRange;
	/// <summary> Orb 전용: 틱 데미지 기본 간격(초). tickInterval ÷ (playerAS × attackspeed) </summary>
	public float[] tickIntervalRange;
	public float[] movespeedRange;
}