#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChestDropSettingsCreator
{
    const string AssetPath = "Assets/Resources/Data/ChestDropSettings.asset";

    [MenuItem("Tools/Game/Create Default Chest Drop Settings")]
    public static void CreateDefault()
    {
        if (!Directory.Exists("Assets/Resources/Data"))
            Directory.CreateDirectory("Assets/Resources/Data");

        var existing = AssetDatabase.LoadAssetAtPath<ChestDropSettings>(AssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("[Chest] ChestDropSettings가 이미 있습니다.");
            return;
        }

        var settings = ScriptableObject.CreateInstance<ChestDropSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        Debug.Log("[Chest] ChestDropSettings 생성 완료: " + AssetPath);
    }
}
#endif
