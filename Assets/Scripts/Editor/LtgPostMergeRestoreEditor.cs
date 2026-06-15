#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ShinPro 병합 후 LTG UI/룬/보스 HUD를 한 번에 복구합니다.
/// Unity -batchmode -quit -projectPath ... -executeMethod LtgPostMergeRestoreEditor.ApplyFromCommandLine
/// </summary>
public static class LtgPostMergeRestoreEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/LTG/Restore Post-Merge UI and Features")]
	[MenuItem("Window/The Last Rune/LTG/Restore Post-Merge UI and Features")]
	public static void ApplyFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("LTG 복구", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		ApplyInternal();
		EditorUtility.DisplayDialog(
			"LTG 복구",
			"UI 스타일, 보스 체력바, 룬 순서 HUD, 룬 아이콘, 오버레이 레이아웃을 복구했습니다.",
			"확인");
	}

	public static void ApplyFromCommandLine()
	{
		ApplyInternal();
		AssetDatabase.SaveAssets();
		EditorSceneManager.SaveOpenScenes();
		Debug.Log("[LtgPostMergeRestore] 완료.");
		EditorApplication.Exit(0);
	}

	static void ApplyInternal()
	{
		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		OverlayMenuStyleApplyAllEditor.ApplyAllInternal();
		OverlayPanelUILayoutSetup.ApplyAll(saveScene: false, showDialog: false);
		GameplayHudSetupEditor.TrySetupInActiveScene();
		RuneLoadoutHudSetupEditor.Apply(saveScene: false);
		BossHealthHudSetupEditor.Apply(saveScene: false);
		RuneIconAssigner.AssignAll();
		// 스타일 적용 후 카드 레이아웃·스프라이트 순서를 맞춥니다.
		ChoiceSelectUILayoutSetup.ApplyAll(saveScene: false, showDialog: false);

		EditorSceneManager.MarkSceneDirty(scene);
	}
}
#endif
