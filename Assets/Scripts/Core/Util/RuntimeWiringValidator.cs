using UnityEngine;

public static class RuntimeWiringValidator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ValidateOnSceneLoad()
    {
        GameManager game = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        Player player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        WeaponManager weaponManager = Object.FindFirstObjectByType<WeaponManager>(FindObjectsInactive.Include);
        RuneManager runeManager = Object.FindFirstObjectByType<RuneManager>(FindObjectsInactive.Include);

        if (game == null)
        {
            Debug.LogWarning("[RuntimeWiringValidator] GameManager가 없습니다.");
            return;
        }

        if (player == null)
            Debug.LogWarning("[RuntimeWiringValidator] Player가 없습니다.");
        else
        {
            if (player.GetComponent<PlayerStats>() == null) Debug.LogWarning("[RuntimeWiringValidator] PlayerStats 누락");
            if (player.GetComponent<WeaponInventory>() == null) Debug.LogWarning("[RuntimeWiringValidator] WeaponInventory 누락");
            if (player.GetComponent<WeaponController>() == null) Debug.LogWarning("[RuntimeWiringValidator] WeaponController 누락");
            if (player.GetComponent<Scaner>() == null) Debug.LogWarning("[RuntimeWiringValidator] Scaner 누락");
        }

        if (weaponManager == null)
            Debug.LogWarning("[RuntimeWiringValidator] WeaponManager가 없습니다.");
        if (runeManager == null)
            Debug.LogWarning("[RuntimeWiringValidator] RuneManager가 없습니다.");
        if (game.uiWeaponSelect == null)
            Debug.LogWarning("[RuntimeWiringValidator] GameManager.uiWeaponSelect 참조가 비어 있습니다.");
        if (game.uiRuneSelect == null)
            Debug.LogWarning("[RuntimeWiringValidator] GameManager.uiRuneSelect 참조가 비어 있습니다.");

        // 핵심 모션 프리팹 조회 가능 여부
        if (weaponManager != null)
        {
            if (weaponManager.GetMotionPrefab("effect_sword") == null)
                Debug.LogWarning("[RuntimeWiringValidator] effect_sword 프리팹을 찾지 못했습니다.");
            if (weaponManager.GetMotionPrefab("effect_bow") == null)
                Debug.LogWarning("[RuntimeWiringValidator] effect_bow 프리팹을 찾지 못했습니다.");
            if (weaponManager.GetMotionPrefab("effect_orb") == null)
                Debug.LogWarning("[RuntimeWiringValidator] effect_orb 프리팹을 찾지 못했습니다.");
        }
    }
}
