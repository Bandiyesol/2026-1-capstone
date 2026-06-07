#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// WeaponSelectUI를 복제해 씬에 RewardSelectUI를 영구 배치합니다.
/// Tools → Setup Reward Select UI In Scene
/// </summary>
public static class RewardSelectUISceneSetup
{
	const string TargetScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/Setup Reward Select UI In Scene")]
	public static void SetupFromMenu()
	{
		SetupInScene(saveScene: true, showDialog: true);
	}

	public static bool SetupInScene(bool saveScene, bool showDialog)
	{
		if (Application.isPlaying)
		{
			Debug.LogWarning("[RewardSelectUISceneSetup] 플레이 모드에서는 실행할 수 없습니다.");
			return false;
		}

		Scene scene = SceneManager.GetActiveScene();
		if (scene.path != TargetScenePath)
		{
			scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
		}

		if (Object.FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include) != null)
		{
			if (showDialog)
				EditorUtility.DisplayDialog("RewardSelectUI", "씬에 RewardSelectUI가 이미 있습니다.", "확인");
			return true;
		}

		WeaponSelectUI weaponSelect = Object.FindAnyObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
		if (weaponSelect == null)
		{
			Debug.LogError("[RewardSelectUISceneSetup] WeaponSelectUI를 찾지 못했습니다.");
			return false;
		}

		GameObject clone = Object.Instantiate(weaponSelect.gameObject, weaponSelect.transform.parent);
		clone.name = "RewardSelectUI";
		clone.SetActive(false);

		Undo.RegisterCreatedObjectUndo(clone, "Create RewardSelectUI");

		Object.DestroyImmediate(clone.GetComponent<WeaponSelectUI>());

		RewardSelectUI reward = clone.GetComponent<RewardSelectUI>();
		if (reward == null)
			reward = clone.AddComponent<RewardSelectUI>();

		reward.EnsureReady();
		EditorUtility.SetDirty(reward);

		if (saveScene)
		{
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
		}

		Debug.Log("[RewardSelectUISceneSetup] RewardSelectUI를 씬에 생성했습니다.");
		if (showDialog)
			EditorUtility.DisplayDialog("RewardSelectUI", "RewardSelectUI를 씬에 생성했습니다.", "확인");

		return true;
	}
}
#endif
