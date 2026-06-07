#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShopCatalogSettings))]
public class ShopCatalogSettingsEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var settings = (ShopCatalogSettings)target;
		EditorGUILayout.Space(4f);
		EditorGUILayout.LabelField("등급 출현 확률 (무기·악세서리)", EditorStyles.boldLabel);

		float total = settings.GetTotalGradeWeight();
		if (total <= 0f)
		{
			EditorGUILayout.HelpBox("등급 가중치 합이 0입니다. Common만 나옵니다.", MessageType.Warning);
			return;
		}

		if (settings.gradeWeights == null || settings.gradeWeights.Length == 0)
			return;

		foreach (ShopGradeWeight entry in settings.gradeWeights)
		{
			if (entry == null || entry.weight <= 0f)
				continue;

			float pct = entry.weight / total * 100f;
			EditorGUILayout.LabelField(
				ShopGradeUtility.ToGradeName(entry.grade),
				$"{pct:F1}%  (가중치 {entry.weight:F0})");
		}
	}
}
#endif
