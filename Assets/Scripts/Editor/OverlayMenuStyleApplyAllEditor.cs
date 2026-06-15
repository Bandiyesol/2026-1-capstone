#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로그인·설정·메인스토리·보스알리미·룬순서 UI를 메인 메뉴 스타일로 한 번에 적용.
/// Window/The Last Rune/UI/Apply Overlay Menu Style (All)
/// </summary>
public static class OverlayMenuStyleApplyAllEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/UI/Apply Overlay Menu Style (All)")]
	[MenuItem("Window/The Last Rune/UI/Apply Overlay Menu Style (All)")]
	public static void ApplyFromMenu()
	{
		if (SceneManager.GetActiveScene().path != ScenePath)
			EditorSceneManager.OpenScene(ScenePath);

		ApplyAllInternal();

		EditorUtility.DisplayDialog(
			"오버레이 메뉴 스타일",
			"로그인·회원가입·비밀번호 찾기·설정·메인스토리·보스알리미·룬순서 UI에 메인 메뉴 스타일을 적용했습니다.\n\n클리어 랭킹은 변경하지 않았습니다.",
			"확인");
	}

	/// <summary>배치/CI — Unity -executeMethod OverlayMenuStyleApplyAllEditor.ApplyFromCommandLine</summary>
	public static void ApplyFromCommandLine()
	{
		EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
		ApplyAllInternal();
		EditorSceneManager.SaveOpenScenes();
		Debug.Log("[OverlayMenuStyleApplyAll] Scene saved.");
		EditorApplication.Exit(0);
	}

	public static void ApplyAllInternal()
	{
		UiBareElementStylingEditor.ApplyFromBatch();
		OverlayMenuUiLayoutEditor.ApplyInternal(showDialog: false);
		UiRegressionFixEditor.ApplyFixesWithoutDialog();
	}
}
#endif
