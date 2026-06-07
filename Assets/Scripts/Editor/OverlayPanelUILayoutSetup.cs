#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OverlayPanelUILayoutSetup
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	[MenuItem("Tools/Resize Overlay Panels (Status/Inventory/Setting/Shop)")]
	public static void ResizeFromMenu()
	{
		ApplyAll(saveScene: true, showDialog: true);
	}

	public static void ApplyAll(bool saveScene, bool showDialog)
	{
		Scene scene = SceneManager.GetActiveScene();
		if (!Application.isPlaying && scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		int count = 0;
		string[] panelNames = { "StatusPanel", "InventoryPanel", "SettingPanel", "ShopPanel" };
		foreach (string panelName in panelNames)
		{
			GameObject panel = GameObject.Find(panelName);
			if (panel == null)
				continue;

			OverlayPanelUILayout.Apply(panel.transform);
			EditorUtility.SetDirty(panel);
			count++;
		}

		if (saveScene && !Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
		}

		Debug.Log($"[OverlayPanelUILayoutSetup] {count}개 오버레이 패널 레이아웃 적용");
		if (showDialog)
			EditorUtility.DisplayDialog("Overlay Panels", $"Status/Inventory/Setting/Shop {count}개 패널 크기·폰트를 맞췄습니다.", "확인");
	}
}
#endif
