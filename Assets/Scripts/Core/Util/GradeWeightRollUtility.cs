using UnityEngine;

/// <summary>Common / Rare / Unique / Legendary 가중치 룰렛.</summary>
public static class GradeWeightRollUtility
{
	public static T Roll<T>(float common, float rare, float unique, float legendary,
		T commonValue, T rareValue, T uniqueValue, T legendaryValue)
	{
		float total = common + rare + unique + legendary;
		if (total <= 0f)
			return commonValue;

		float roll = Random.Range(0f, total);
		if (roll < common)
			return commonValue;

		roll -= common;
		if (roll < rare)
			return rareValue;

		roll -= rare;
		if (roll < unique)
			return uniqueValue;

		return legendaryValue;
	}

	public static float GetPercent(float part, float total) =>
		total > 0f ? part / total * 100f : 0f;
}
