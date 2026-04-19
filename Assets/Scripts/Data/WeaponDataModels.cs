using System;
using System.Collections.Generic;

[Serializable]
public class WeaponInfo
{
	public string id;
	public string name;
	public string spriteId;
	public string motionId;
	public string type;
	public string grade;
	public string balanceKey;
}

[Serializable]
public class WeaponBalance
{
	public string key;
	public float[] damageRange;
	public float[] weightRange;
	public float[] cooltimeRange;
	public float[] reachRange;
	public float[] speedRange;
}

[Serializable]
public class WeaponDataLoader
{
	public List<WeaponInfo> weapons;
	public List<WeaponBalance> balances;
}