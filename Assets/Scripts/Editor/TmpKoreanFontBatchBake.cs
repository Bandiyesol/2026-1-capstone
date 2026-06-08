#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// CI/배치 모드: neodgm SDF에 악세서리 글리프를 구워 넣고 종료합니다.
/// Unity -batchmode -quit -projectPath ... -executeMethod TmpKoreanFontBatchBake.BakeAndExit
/// </summary>
public static class TmpKoreanFontBatchBake
{
	public static void BakeAndExit()
	{
		TmpKoreanFontEditor.AddAccessoryGlyphsToNeoDgm();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorApplication.Exit(0);
	}
}
#endif
