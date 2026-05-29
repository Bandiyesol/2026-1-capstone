using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnitySetupValidator
{
    const string MainScenePath = "Assets/Scenes/ProtoType_LTG.unity";
    const string WeaponInfoPath = "Assets/Resources/Data/WeaponInfo.json";
    const string WeaponBalancePath = "Assets/Resources/Data/WeaponBalance.json";
    const string MotionFolderPath = "Assets/Resources/Prefabs/Motions";
    const string RuneCatalogPath = "Assets/Arts/Data/RuneCatalog.asset";

    [MenuItem("Tools/Setup/Apply ProtoType_LTG Build Scene")]
    public static void ApplyMainBuildScene()
    {
        var scene = new EditorBuildSettingsScene(MainScenePath, true);
        EditorBuildSettings.scenes = new[] { scene };
        Debug.Log("[SetupValidator] Build Scene을 ProtoType_LTG 1개로 설정했습니다.");
    }

    [MenuItem("Tools/Setup/Validate Unity Wiring")]
    public static void ValidateUnityWiring()
    {
        List<string> warnings = new();

        ValidateBuildSettings(warnings);
        ValidatePlayerComponents(warnings);
        ValidateManagersAndReferences(warnings);
        ValidateWeaponResources(warnings);
        ValidateMotionPrefabs(warnings);
        ValidateRuneSetup(warnings);

        if (warnings.Count == 0)
        {
            Debug.Log("[SetupValidator] 모든 검증을 통과했습니다.");
            return;
        }

        Debug.LogWarning("[SetupValidator] 검증 경고:\n- " + string.Join("\n- ", warnings));
    }

    static void ValidateBuildSettings(List<string> warnings)
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0 || scenes[0].path != MainScenePath)
            warnings.Add("Build Settings의 첫 씬이 ProtoType_LTG가 아닙니다.");
    }

    static void ValidatePlayerComponents(List<string> warnings)
    {
        Player player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null)
        {
            warnings.Add("씬에서 Player 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        Component[] required =
        {
            player.GetComponent<PlayerStats>(),
            player.GetComponent<WeaponInventory>(),
            player.GetComponent<WeaponController>(),
            player.GetComponent<Scaner>(),
            player.GetComponent<Rigidbody2D>(),
            player.GetComponent<Collider2D>(),
            player.GetComponent<SpriteRenderer>(),
            player.GetComponent<Animator>(),
            player.GetComponent<UnityEngine.InputSystem.PlayerInput>()
        };

        if (required.Any(c => c == null))
            warnings.Add("Player 필수 컴포넌트 중 누락이 있습니다 (PlayerStats/WeaponInventory/WeaponController/Scaner/물리/렌더/입력).");
    }

    static void ValidateManagersAndReferences(List<string> warnings)
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (game == null)
        {
            warnings.Add("씬에 GameManager가 없습니다.");
        }
        else
        {
            if (game.player == null) warnings.Add("GameManager.player가 비어 있습니다.");
            if (game.pool == null) warnings.Add("GameManager.pool이 비어 있습니다.");
            if (game.uiResult == null) warnings.Add("GameManager.uiResult가 비어 있습니다.");
            if (game.uiWeaponSelect == null) warnings.Add("GameManager.uiWeaponSelect가 비어 있습니다.");
            if (game.uiRuneSelect == null) warnings.Add("GameManager.uiRuneSelect가 비어 있습니다.");
        }

        if (Object.FindObjectsByType<WeaponManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            warnings.Add("WeaponManager는 씬 전체에서 1개여야 합니다.");
        if (Object.FindObjectsByType<RuneManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            warnings.Add("RuneManager는 씬 전체에서 1개여야 합니다.");
    }

    static void ValidateWeaponResources(List<string> warnings)
    {
        TextAsset infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(WeaponInfoPath);
        TextAsset balanceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(WeaponBalancePath);
        if (infoAsset == null || balanceAsset == null)
        {
            warnings.Add("WeaponInfo.json 또는 WeaponBalance.json을 찾을 수 없습니다.");
            return;
        }

        WeaponDataLoader infoLoader = JsonUtility.FromJson<WeaponDataLoader>(infoAsset.text);
        WeaponDataLoader balanceLoader = JsonUtility.FromJson<WeaponDataLoader>(balanceAsset.text);
        HashSet<string> balanceKeys = new(balanceLoader.balance.Select(b => b.key));

        foreach (WeaponInfo info in infoLoader.info)
        {
            if (!balanceKeys.Contains(info.balanceKey))
                warnings.Add($"WeaponBalance 누락: {info.id} -> {info.balanceKey}");

            string prefabPath = $"{MotionFolderPath}/{info.motionId}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                warnings.Add($"Motion 프리팹 누락: {prefabPath}");
        }
    }

    static void ValidateMotionPrefabs(List<string> warnings)
    {
        string[] required = { "effect_sword", "effect_bow", "effect_orb" };
        foreach (string prefabName in required)
        {
            string path = $"{MotionFolderPath}/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                warnings.Add($"필수 Motion 프리팹 누락: {path}");
                continue;
            }

            if (prefab.GetComponent<Motion>() == null)
                warnings.Add($"{prefabName}에 Motion 파생 컴포넌트가 없습니다.");

            Collider2D collider = prefab.GetComponent<Collider2D>();
            if (collider == null)
                warnings.Add($"{prefabName}에 Collider2D가 없습니다.");
            else if (!collider.isTrigger)
                warnings.Add($"{prefabName}의 Collider2D는 Trigger 권장입니다.");

            if (prefabName == "effect_sword" && prefab.GetComponent<Animator>() == null)
                warnings.Add("effect_sword는 Animator를 사용하는 구성이 권장됩니다.");
        }
    }

    static void ValidateRuneSetup(List<string> warnings)
    {
        RuneCatalog catalog = AssetDatabase.LoadAssetAtPath<RuneCatalog>(RuneCatalogPath);
        if (catalog == null)
        {
            warnings.Add("RuneCatalog.asset을 찾을 수 없습니다.");
            return;
        }

        if (catalog.runes == null || catalog.runes.Length == 0)
        {
            warnings.Add("RuneCatalog.runes가 비어 있습니다.");
            return;
        }

        foreach (RuneData rune in catalog.runes)
        {
            if (rune == null)
            {
                warnings.Add("RuneCatalog에 null 항목이 있습니다.");
                continue;
            }

            if (!RuneEffectRegistry.TryGetEffectType(rune.runeType, out _)
                && rune.runeType != RuneType.None
                && rune.category != RuneCategory.Final)
            {
                warnings.Add($"RuneEffect 미매핑: {rune.name} ({rune.runeType})");
            }
        }

        RuneSelectUI runeSelect = Object.FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
        if (runeSelect == null)
        {
            warnings.Add("씬에 RuneSelectUI가 없습니다.");
            return;
        }

        if (runeSelect.runeCatalog == null && (runeSelect.allRunes == null || runeSelect.allRunes.Length == 0))
            warnings.Add("RuneSelectUI에 runeCatalog/allRunes가 연결되지 않았습니다.");
    }
}
