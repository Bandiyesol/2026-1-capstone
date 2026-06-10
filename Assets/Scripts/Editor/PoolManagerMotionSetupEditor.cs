#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PoolManager Inspector에 Motion 프리팹 목록을 채웁니다.
/// </summary>
public static class PoolManagerMotionSetupEditor
{
    const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
    const string MotionFolder = "Assets/Resources/Prefabs/Motions";

    [MenuItem("Tools/Game/Load Motion Prefabs Into PoolManager")]
    public static void LoadFromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Motion 풀 설정", "플레이 모드에서는 실행할 수 없습니다.", "확인");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PoolManager pool = Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);
        if (pool == null)
        {
            EditorUtility.DisplayDialog("Motion 풀 설정", "씬에서 PoolManager를 찾지 못했습니다.", "확인");
            return;
        }

        if (!TryLoadMotionPrefabs(pool))
        {
            EditorUtility.DisplayDialog("Motion 풀 설정", $"{MotionFolder} 에 Motion 프리팹이 없습니다.", "확인");
            return;
        }

        EditorUtility.SetDirty(pool);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog(
            "Motion 풀 설정",
            $"PoolManager → Motion Prefabs 에 {pool.motionPrefabs.Length}개를 연결했습니다.\n" +
            "Inspector에서 Chest Prefabs 아래 '무기 Motion 풀' 섹션을 확인하세요.",
            "확인");
    }

    public static bool TryLoadMotionPrefabs(PoolManager pool)
    {
        if (pool == null)
            return false;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { MotionFolder });
        if (guids == null || guids.Length == 0)
            return false;

        var prefabs = new GameObject[guids.Length];
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<Motion>() == null)
                continue;

            prefabs[count++] = prefab;
        }

        if (count == 0)
            return false;

        if (count < prefabs.Length)
            System.Array.Resize(ref prefabs, count);

        pool.motionPrefabs = prefabs;
        return true;
    }
}
#endif
