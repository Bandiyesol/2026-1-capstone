#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AudioMixSettingsBuilder
{
	const string AssetPath = "Assets/Resources/Data/AudioMixSettings.asset";

	[MenuItem("Tools/Game/Create Audio Mix Settings")]
	public static void CreateFromMenu()
	{
		if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
		{
			if (!AssetDatabase.IsValidFolder("Assets/Resources"))
				AssetDatabase.CreateFolder("Assets", "Resources");
			AssetDatabase.CreateFolder("Assets/Resources", "Data");
		}

		AudioMixSettings existing = AssetDatabase.LoadAssetAtPath<AudioMixSettings>(AssetPath);
		if (existing != null)
		{
			Selection.activeObject = existing;
			EditorGUIUtility.PingObject(existing);
			Debug.Log("[AudioMixSettingsBuilder] 이미 존재합니다 — Inspector에서 조절하세요.");
			return;
		}

		var asset = ScriptableObject.CreateInstance<AudioMixSettings>();
		AssetDatabase.CreateAsset(asset, AssetPath);
		AssetDatabase.SaveAssets();
		AudioMixSettings.SetCached(asset);
		Selection.activeObject = asset;
		EditorGUIUtility.PingObject(asset);
		Debug.Log("[AudioMixSettingsBuilder] AudioMixSettings 생성 완료 — Inspector에서 BGM/SFX 배율을 조절하세요.");
	}
}
#endif
