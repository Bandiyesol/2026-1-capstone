#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChestDropSettings))]
public class ChestDropSettingsEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var settings = (ChestDropSettings)target;
		EditorGUILayout.Space(4f);

		EditorGUILayout.HelpBox(
			$"상자 드랍 — 일반 몬스터: {settings.enemyDropChance * 100f:F1}%, " +
			$"보스/유니크: {settings.bossDropChance * 100f:F1}%",
			MessageType.Info);

		DrawChestGradePreview(settings);
		DrawRewardTypePreview(settings, ChestGrade.Normal, settings.normalChestReward);
		DrawRewardTypePreview(settings, ChestGrade.Rare, settings.rareChestReward);
		DrawRewardTypePreview(settings, ChestGrade.Unique, settings.uniqueChestReward);
		DrawRewardTypePreview(settings, ChestGrade.Legendary, settings.legendaryChestReward);

		DrawItemGradePreview(settings, ChestGrade.Normal, settings.normalItemGrade);
		DrawItemGradePreview(settings, ChestGrade.Rare, settings.rareItemGrade);
		DrawItemGradePreview(settings, ChestGrade.Unique, settings.uniqueItemGrade);
		DrawItemGradePreview(settings, ChestGrade.Legendary, settings.legendaryItemGrade);
	}

	static void DrawChestGradePreview(ChestDropSettings settings)
	{
		EditorGUILayout.LabelField("드롭 상자 등급", EditorStyles.boldLabel);
		float total = settings.GetTotalChestGradeWeight();
		if (total <= 0f)
		{
			EditorGUILayout.HelpBox("상자 등급 가중치 합이 0입니다.", MessageType.Warning);
			return;
		}

		DrawWeightLine("일반", settings.normalWeight, total);
		DrawWeightLine("희귀", settings.rareWeight, total);
		DrawWeightLine("유니크", settings.uniqueWeight, total);
		DrawWeightLine("전설", settings.legendaryWeight, total);
		EditorGUILayout.Space(2f);
	}

	static void DrawRewardTypePreview(ChestDropSettings settings, ChestGrade grade, ChestRewardWeight weight)
	{
		float total = weight.weapon + weight.accessory + weight.relic;
		if (total <= 0f)
			return;

		EditorGUILayout.LabelField(
			$"{ChestDropSettings.GetGradeLabel(grade)} 상자 — 보상 종류",
			EditorStyles.boldLabel);
		DrawWeightLine("무기", weight.weapon, total);
		DrawWeightLine("악세서리", weight.accessory, total);
		DrawWeightLine("성물", weight.relic, total);
		EditorGUILayout.Space(2f);
	}

	static void DrawItemGradePreview(ChestDropSettings settings, ChestGrade grade, ItemGradeWeight weight)
	{
		float total = weight.common + weight.rare + weight.unique + weight.legendary;
		if (total <= 0f)
			return;

		EditorGUILayout.LabelField(
			$"{ChestDropSettings.GetGradeLabel(grade)} 상자 — 아이템 등급",
			EditorStyles.boldLabel);
		DrawWeightLine("일반", weight.common, total);
		DrawWeightLine("희귀", weight.rare, total);
		DrawWeightLine("유니크", weight.unique, total);
		DrawWeightLine("전설", weight.legendary, total);
		EditorGUILayout.Space(2f);
	}

	static void DrawWeightLine(string label, float weight, float total)
	{
		if (weight <= 0f)
			return;

		float pct = weight / total * 100f;
		EditorGUILayout.LabelField(label, $"{pct:F1}%  (가중치 {weight:F0})");
	}
}
#endif
