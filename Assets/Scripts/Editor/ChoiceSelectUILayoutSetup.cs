#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChoiceSelectUILayoutSetup
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/Resize Choice Select UI")]
	public static void ResizeFromMenu()
	{
		ApplyAll(saveScene: true, showDialog: true);
	}

	public static void ApplyAll(bool saveScene, bool showDialog)
	{
		if (Application.isPlaying)
		{
			Debug.LogWarning("[ChoiceSelectUILayoutSetup] 플레이 모드에서는 씬 저장 없이 현재 UI에만 적용합니다.");
		}

		Scene scene = SceneManager.GetActiveScene();
		if (!Application.isPlaying && scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		int count = 0;
		foreach (WeaponSelectUI ui in Object.FindObjectsByType<WeaponSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			ChoiceSelectUILayout.Apply(ui.transform);
			EditorUtility.SetDirty(ui);
			count++;
		}

		foreach (RewardSelectUI ui in Object.FindObjectsByType<RewardSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			ChoiceSelectUILayout.Apply(ui.transform);
			EditorUtility.SetDirty(ui);
			count++;
		}

		foreach (RuneSelectUI ui in Object.FindObjectsByType<RuneSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			ChoiceSelectUILayout.Apply(ui.transform);
			EditorUtility.SetDirty(ui);
			count++;
		}

		if (saveScene && !Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
		}

		Debug.Log($"[ChoiceSelectUILayoutSetup] {count}개 선택 UI 레이아웃 적용");
		if (showDialog)
			EditorUtility.DisplayDialog("Choice Select UI", $"선택 UI {count}개 레이아웃을 키웠습니다.", "확인");
	}
}
#endif
