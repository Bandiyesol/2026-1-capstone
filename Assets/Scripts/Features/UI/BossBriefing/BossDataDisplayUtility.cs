using UnityEngine;

/// <summary>보스 알리미·HUD에 표시할 BossData 스탯 문구.</summary>
public static class BossDataDisplayUtility
{
	public static BossData ResolveForStage(int stageIndex, GameObject[] portraitPrefabFallback = null)
	{
		GameObject prefab = BossBriefPortraitResolver.ResolvePrefab(stageIndex, portraitPrefabFallback);
		if (prefab == null)
			return null;

		BossBase boss = prefab.GetComponent<BossBase>();
		if (boss == null)
			boss = prefab.GetComponentInChildren<BossBase>(true);

		return boss != null ? boss.data : null;
	}

	public static string FormatStatsLine(BossData data, bool includeMelee = true)
	{
		if (data == null)
			return string.Empty;

		string line = $"체력 {FormatValue(data.maxHealth)} · 이속 {FormatValue(data.moveSpeed)}";
		if (includeMelee)
			line += $" · 근접 {FormatValue(data.attackDamage)}";

		line += $" · 방어 {FormatDefense(data.damageReduction)}";
		return line;
	}

	public static string CombineStatsAndDescription(string statsLine, string description)
	{
		if (string.IsNullOrWhiteSpace(statsLine))
			return description ?? string.Empty;

		if (string.IsNullOrWhiteSpace(description))
			return statsLine;

		return statsLine + "\n" + description;
	}

	static string FormatValue(float value)
	{
		if (Mathf.Approximately(value, Mathf.Round(value)))
			return ((int)Mathf.Round(value)).ToString();

		return value.ToString("0.#");
	}

	static string FormatDefense(float reduction)
	{
		return $"{FormatValue(reduction * 100f)}%";
	}
}
