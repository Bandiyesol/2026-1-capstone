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
	public string type;
	public string grade;
	public string balanceKey;
}


[Serializable]
public class WeaponBalance
{
	public string key;
	public bool isRecursion;
	public float[] damageRange;
	public float[] weightRange;
	public float[] sizeRange;
	public float[] spawntimeRange;
	public float[] cooltimeRange;
	public float[] reachRange;
	public float[] speedRange;
}