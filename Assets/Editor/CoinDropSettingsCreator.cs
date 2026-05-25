#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CoinDropSettingsCreator
{
    const string AssetPath = "Assets/Resources/Data/CoinDropSettings.asset";

    [MenuItem("Tools/Game/Create Default Coin Drop Settings")]
    public static void CreateDefault()
    {
        if (!Directory.Exists("Assets/Resources/Data"))
            Directory.CreateDirectory("Assets/Resources/Data");

        var existing = AssetDatabase.LoadAssetAtPath<CoinDropSettings>(AssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("[Coin] CoinDropSettings가 이미 있습니다.");
            return;
        }

        var settings = ScriptableObject.CreateInstance<CoinDropSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        Debug.Log("[Coin] CoinDropSettings 생성 완료: " + AssetPath);
    }
}
#endif
